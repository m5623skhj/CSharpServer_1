# CSharpServer/CSharpServer/Network/EchoTcpServer.cs

## Purpose

TCP echo server with bounded concurrent client handling and idle connection cleanup.

## Namespace

`CSharpServer.Network`

## Types

### `EchoTcpServer`

Wraps `TcpListener` and accepts echo clients either sequentially or concurrently.

## Public Members

### Constructor

`EchoTcpServer(IPAddress ipAddress, int port, int inBufferSize)`

- Creates a listener for the supplied address and port.
- Stores the stream read buffer size.
- Uses defaults of 100 concurrent clients and a 30-second client idle timeout.
- Rejects a null IP address.
- Accepts ports from `0` through `65535`, where `0` requests OS-assigned binding, and rejects values outside that range.
- Rejects zero or negative buffer sizes.

`EchoTcpServer(IPAddress ipAddress, int port, int inBufferSize, int maxConcurrentClients, TimeSpan clientIdleTimeout)`

- Configures the maximum number of actively handled clients and the per-read idle timeout.
- Rejects a null IP address.
- Accepts ports from `0` through `65535` and rejects values outside that range.
- Rejects zero or negative buffer size and connection limit values.
- Rejects zero, negative, or .NET timer-limit-exceeding idle timeout values.

### `Port`

Returns the bound listener port after `Start()` completes successfully. Useful when port `0` is used in tests.

- Throws `InvalidOperationException` before the listener has started instead of returning the unbound requested port.
- Throws `ObjectDisposedException` after server disposal begins.
- Shares the listener lifecycle lock with `Start()` and `Dispose()` so concurrent access observes a consistent state.

### `Start()`

Starts the TCP listener.

Throws `ObjectDisposedException` if the server has already been disposed.

Listener startup is serialized with disposal so a concurrent `Start` cannot reopen the listener after shutdown.

### `AcceptAndHandleOnce()`

- Acquires a shared client slot before accepting so concurrent synchronous calls honor the configured client limit.
- Accepts one `TcpClient`.
- Gets its stream.
- Creates an echo `StreamConnection`.
- Reads until the client closes the stream.
- Throws `ObjectDisposedException` when disposal interrupts a blocked accept instead of exposing a listener shutdown socket error.
- Throws `ObjectDisposedException` when disposal interrupts its wait for a client slot.

### `AcceptAndHandle(int clientCount)`

- Rejects zero or negative client counts.
- Calls `AcceptAndHandleOnce()` repeatedly.
- Handles clients sequentially, not concurrently.

### `AcceptAndHandleConcurrently(int clientCount)`

- Rejects zero or negative client counts.
- Accepts the configured number of clients.
- Acquires a connection slot before accepting each client.
- Handles accepted clients with asynchronous stream reads up to the configured limit.
- Cancels remaining accepts and closes peer clients when a handler faults.
- Cancels remaining accepts when a handler ends in an unexpected canceled state.
- Propagates a handler fault without waiting for the remaining client count to connect.
- Propagates unexpected handler cancellation without waiting for the remaining client count to connect.
- Waits for all client handler tasks to complete.
- Does not capture a caller synchronization context while waiting for slots, accepts, handlers, or task observation.

### `AcceptAndHandleConcurrently(CancellationToken cancellationToken)`

- Accepts clients until cancellation is requested.
- Waits for a connection slot before accepting another client.
- Handles each accepted client with asynchronous stream reads using the supplied cancellation token.
- Closes clients that do not produce read data before the configured idle timeout.
- Stops waiting for new clients when cancellation is requested.
- Closes already accepted active clients when cancellation is requested.
- Treats a client handler that observes the supplied canceled token as normal shutdown.
- Waits for accepted client handler tasks to complete before returning.
- Does not capture a caller synchronization context while accepting or completing cancellation cleanup.

## Internal Behavior

- Connection slots are returned after handler completion, including failure and cancellation paths.
- Synchronous accept calls hold the same global connection slots through handler completion and return them on every accept, tracking, handler, and disposal path.
- Listener start and stop operations share a lifecycle lock to preserve the disposed state during concurrent calls.
- Synchronous and asynchronous handler completion retry deferred connection-slot disposal when shutdown began during handling.
- Accepted clients are tracked at server scope until their handlers complete.
- Slot waiters are counted internally so concurrency tests can prove that a second client is actually queued.
- Completed successful client handler tasks are pruned while the open-ended accept loop is running.
- A faulted handler cancels the accept wait immediately, closes peer handlers, and propagates its original exception.
- A handler that ends canceled also stops the accept wait so the server cannot remain blocked waiting for another client.
- Fixed-count and open-ended modes share the same unsuccessful-handler cancellation behavior.
- Unexpected accept failures cancel and close active handlers before the original accept exception is propagated.
- Concurrent handlers await `StreamConnection.ReadUntilEndAsync` without wrapping synchronous reads in `Task.Run`.
- Default client handlers do not capture a caller synchronization context while awaiting connection completion, including when accept completes synchronously for a queued client.
- Concurrent echo responses use cancellation-aware asynchronous writes.
- Expected cancellation from any client handler using the server-supplied token is handled as normal server shutdown.
- Handler cancellation remains an error when the server-supplied token has not been canceled.
- Client-level connection, stream, and `InvalidDataException` failures are isolated so one bad client does not fault the server loop.
- General `InvalidOperationException` failures are not swallowed as client network errors.

### `Dispose()`

- Cancels fixed-count and open-ended asynchronous accept loops.
- Interrupts blocked synchronous accepts with `ObjectDisposedException`.
- Stops the listener and closes all accepted active clients.
- Disposes cancellation and connection-slot resources.
- Makes concurrent callers wait for the in-progress disposal to finish before returning.
- Allows active handler tasks to complete during shutdown without surfacing slot-release disposal races.
- Retries deferred connection-slot disposal after handlers leave their active section, including faulted asynchronous handlers.
- Is idempotent, including reentrant calls from synchronous cancellation callbacks.

## Notes

This server supports fixed client counts and a cancellable open-ended concurrent accept loop.

After disposal, `Start` and every sequential or concurrent accept entry point reject further use with `ObjectDisposedException`.
