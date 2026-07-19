# CSharpServer/CSharpServer/Network/EchoTcpServer.cs

## Purpose

Minimal TCP echo server for a fixed number of clients.

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
- Rejects zero or negative buffer sizes.

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
- Handles each accepted client on a separate task.
- Waits for all client handler tasks to complete.

### `AcceptAndHandleConcurrently(CancellationToken cancellationToken)`

- Accepts clients until cancellation is requested.
- Handles each accepted client on a separate task.
- Stops waiting for new clients when cancellation is requested.
- Closes already accepted active clients when cancellation is requested.
- Waits for accepted client handler tasks to complete before returning.

## Internal Behavior

- Completed client handler tasks are pruned while the open-ended accept loop is running.
- Client-level connection, stream, and malformed packet exceptions are isolated so one bad client does not fault the server loop.

### `Dispose()`

Stops the listener.

## Notes

This server supports fixed client counts and a cancellable open-ended concurrent accept loop. Executable-level graceful shutdown wiring is future work.
