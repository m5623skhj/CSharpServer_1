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
- Rejects zero or negative buffer sizes.

`EchoTcpServer(IPAddress ipAddress, int port, int inBufferSize, int maxConcurrentClients, TimeSpan clientIdleTimeout)`

- Configures the maximum number of actively handled clients and the per-read idle timeout.
- Rejects zero or negative buffer size, connection limit, and idle timeout values.

### `Port`

Returns the bound listener port. Useful when port `0` is used in tests.

### `Start()`

Starts the TCP listener.

### `AcceptAndHandleOnce()`

- Accepts one `TcpClient`.
- Gets its stream.
- Creates an echo `StreamConnection`.
- Reads until the client closes the stream.

### `AcceptAndHandle(int clientCount)`

- Rejects zero or negative client counts.
- Calls `AcceptAndHandleOnce()` repeatedly.
- Handles clients sequentially, not concurrently.

### `AcceptAndHandleConcurrently(int clientCount)`

- Rejects zero or negative client counts.
- Accepts the configured number of clients.
- Acquires a connection slot before accepting each client.
- Handles accepted clients with asynchronous stream reads up to the configured limit.
- Waits for all client handler tasks to complete.

### `AcceptAndHandleConcurrently(CancellationToken cancellationToken)`

- Accepts clients until cancellation is requested.
- Waits for a connection slot before accepting another client.
- Handles each accepted client with asynchronous stream reads using the supplied cancellation token.
- Closes clients that do not produce read data before the configured idle timeout.
- Stops waiting for new clients when cancellation is requested.
- Closes already accepted active clients when cancellation is requested.
- Waits for accepted client handler tasks to complete before returning.

## Internal Behavior

- Connection slots are returned from handler `finally` blocks, including failure and cancellation paths.
- Completed successful client handler tasks are pruned while the open-ended accept loop is running.
- A faulted handler cancels the accept wait immediately, closes peer handlers, and propagates its original exception.
- Concurrent handlers await `StreamConnection.ReadUntilEndAsync` without wrapping synchronous reads in `Task.Run`.
- Concurrent echo responses use cancellation-aware asynchronous writes.
- Expected cancellation from an active client read is handled as normal server shutdown.
- Client-level connection, stream, and `InvalidDataException` failures are isolated so one bad client does not fault the server loop.
- General `InvalidOperationException` failures are not swallowed as client network errors.

### `Dispose()`

Stops the listener.

## Notes

This server supports fixed client counts and a cancellable open-ended concurrent accept loop.
