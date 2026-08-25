namespace CSharpServer.Network
{
    public sealed class StreamConnectionReader
    {
        private static readonly TimeSpan MaxTimerDelay =
            TimeSpan.FromMilliseconds(uint.MaxValue - 1);
        private readonly Stream stream;
        private readonly byte[] buffer;
        private readonly Action<ReadOnlyMemory<byte>> dataHandler;
        private readonly Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> asyncDataHandler;
        private readonly SemaphoreSlim readSemaphore = new(1, 1);
        private int unusableState;

        public StreamConnectionReader(
            Stream stream,
            int inBufferSize,
            Action<byte[]> dataHandler)
            : this(
                ValidateStream(stream),
                inBufferSize,
                CreateDataHandler(dataHandler),
                CreateAsyncDataHandler(dataHandler))
        {
        }

        internal StreamConnectionReader(
            Stream stream,
            int inBufferSize,
            Action<ReadOnlyMemory<byte>> dataHandler,
            Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> asyncDataHandler)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(dataHandler);
            ArgumentNullException.ThrowIfNull(asyncDataHandler);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inBufferSize);

            this.stream = stream;
            buffer = new byte[inBufferSize];
            this.dataHandler = dataHandler;
            this.asyncDataHandler = asyncDataHandler;
        }

        internal int AvailableReadSlotCount => readSemaphore.CurrentCount;

        public bool ReadOnce()
        {
            readSemaphore.Wait();
            try
            {
                ThrowIfUnusable();
                var readCount = stream.Read(buffer);
                return HandleRead(buffer, readCount);
            }
            catch (OperationCanceledException exception)
            {
                CloseAfterCancellation(exception, CancellationToken.None);
                throw;
            }
            catch (InvalidDataException exception)
            {
                CloseAfterInvalidDataException(exception);
                throw;
            }
            catch (IOException exception)
            {
                CloseAfterIOException(exception);
                throw;
            }
            finally
            {
                readSemaphore.Release();
            }
        }

        public async Task<bool> ReadOnceAsync(CancellationToken cancellationToken)
        {
            await readSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfUnusable();
                var readCount = await stream.ReadAsync(buffer, cancellationToken)
                    .ConfigureAwait(false);
                if (readCount == 0)
                {
                    return false;
                }

                await asyncDataHandler(buffer.AsMemory(0, readCount), cancellationToken)
                    .ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException exception)
            {
                CloseAfterCancellation(exception, cancellationToken);
                throw;
            }
            catch (InvalidDataException exception)
            {
                CloseAfterInvalidDataException(exception);
                throw;
            }
            catch (IOException exception)
            {
                CloseAfterIOException(exception);
                throw;
            }
            finally
            {
                readSemaphore.Release();
            }
        }

        internal async Task<bool> ReadOnceAsync(
            CancellationToken cancellationToken,
            TimeSpan idleTimeout)
        {
            if (idleTimeout <= TimeSpan.Zero || idleTimeout > MaxTimerDelay)
            {
                throw new ArgumentOutOfRangeException(nameof(idleTimeout));
            }

            await readSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfUnusable();
                using var idleCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                idleCancellation.CancelAfter(idleTimeout);

                int readCount;
                try
                {
                    readCount = await stream.ReadAsync(buffer, idleCancellation.Token)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException exception)
                    when (!cancellationToken.IsCancellationRequested
                        && idleCancellation.IsCancellationRequested)
                {
                    CloseAfterIdleTimeout(exception);
                    return false;
                }

                if (readCount == 0)
                {
                    return false;
                }

                await asyncDataHandler(
                    buffer.AsMemory(0, readCount),
                    cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException exception)
            {
                CloseAfterCancellation(exception, cancellationToken);
                throw;
            }
            catch (InvalidDataException exception)
            {
                CloseAfterInvalidDataException(exception);
                throw;
            }
            catch (IOException exception)
                when (Volatile.Read(ref unusableState) == 0)
            {
                CloseAfterIOException(exception);
                throw;
            }
            finally
            {
                readSemaphore.Release();
            }
        }

        private void CloseAfterCancellation(
            OperationCanceledException exception,
            CancellationToken cancellationToken)
        {
            Interlocked.Exchange(ref unusableState, 1);
            try
            {
                stream.Close();
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
        }

        private void CloseAfterIdleTimeout(OperationCanceledException exception)
        {
            Interlocked.Exchange(ref unusableState, 1);
            try
            {
                stream.Close();
            }
            catch (Exception closeException)
            {
                throw new IOException(
                    "Stream cleanup failed after the idle timeout.",
                    new AggregateException(exception, closeException));
            }
        }

        private void CloseAfterIOException(IOException exception)
        {
            Interlocked.Exchange(ref unusableState, 1);
            try
            {
                stream.Close();
            }
            catch (Exception closeException)
            {
                throw new IOException(
                    exception.Message,
                    new AggregateException(exception, closeException));
            }
        }

        private void CloseAfterInvalidDataException(InvalidDataException exception)
        {
            Interlocked.Exchange(ref unusableState, 1);
            try
            {
                stream.Close();
            }
            catch (Exception closeException)
            {
                throw new InvalidDataException(
                    exception.Message,
                    new AggregateException(exception, closeException));
            }
        }

        private void ThrowIfUnusable()
        {
            ObjectDisposedException.ThrowIf(
                Volatile.Read(ref unusableState) != 0,
                this);
        }

        private bool HandleRead(byte[] readBuffer, int readCount)
        {
            if (readCount == 0)
            {
                return false;
            }

            dataHandler(readBuffer.AsMemory(0, readCount));
            return true;
        }

        private static Stream ValidateStream(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            return stream;
        }

        private static Action<ReadOnlyMemory<byte>> CreateDataHandler(
            Action<byte[]> dataHandler)
        {
            ArgumentNullException.ThrowIfNull(dataHandler);
            return data => dataHandler(data.ToArray());
        }

        private static Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask>
            CreateAsyncDataHandler(Action<byte[]> dataHandler)
        {
            ArgumentNullException.ThrowIfNull(dataHandler);
            return (data, _) =>
            {
                dataHandler(data.ToArray());
                return ValueTask.CompletedTask;
            };
        }
    }
}
