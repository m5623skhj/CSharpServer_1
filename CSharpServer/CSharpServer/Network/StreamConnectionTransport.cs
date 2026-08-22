namespace CSharpServer.Network
{
    public sealed class StreamConnectionTransport : IConnectionTransport
    {
        private readonly Stream stream;
        private readonly object closeSyncRoot = new();
        private readonly SemaphoreSlim sendSemaphore = new(1, 1);
        private bool isClosed;

        public StreamConnectionTransport(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            this.stream = stream;
        }

        internal int AvailableSendSlotCount => sendSemaphore.CurrentCount;

        public void Send(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            sendSemaphore.Wait();
            try
            {
                ThrowIfClosed();
                stream.Write(data);
                stream.Flush();
            }
            catch (IOException exception)
            {
                CloseAfterIOException(exception);
                throw;
            }
            finally
            {
                sendSemaphore.Release();
            }
        }

        public async ValueTask SendAsync(
            ReadOnlyMemory<byte> data,
            CancellationToken cancellationToken)
        {
            await sendSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfClosed();
                await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                try
                {
                    Close();
                }
                catch (Exception closeException)
                {
                    var propagatedCancellationToken = exception.CancellationToken;
                    if (!propagatedCancellationToken.CanBeCanceled
                        && cancellationToken.IsCancellationRequested)
                    {
                        propagatedCancellationToken = cancellationToken;
                    }

                    throw new OperationCanceledException(
                        exception.Message,
                        new AggregateException(exception, closeException),
                        propagatedCancellationToken);
                }

                throw;
            }
            catch (IOException exception)
            {
                CloseAfterIOException(exception);
                throw;
            }
            finally
            {
                sendSemaphore.Release();
            }
        }

        public void Close()
        {
            lock (closeSyncRoot)
            {
                if (isClosed)
                {
                    return;
                }

                isClosed = true;
                stream.Close();
            }
        }

        private void CloseAfterIOException(IOException exception)
        {
            try
            {
                Close();
            }
            catch (Exception closeException)
            {
                throw new IOException(
                    exception.Message,
                    new AggregateException(exception, closeException));
            }
        }

        private void ThrowIfClosed()
        {
            lock (closeSyncRoot)
            {
                ObjectDisposedException.ThrowIf(isClosed, this);
            }
        }
    }
}
