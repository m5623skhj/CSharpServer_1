# CSharpServer/CSharpServer/Network/EchoTcpServer.cs

## Purpose

Minimal TCP echo server for a fixed number of sequential clients.

## Namespace

`CSharpServer.Network`

## Types

### `EchoTcpServer`

Wraps `TcpListener` and accepts echo clients sequentially.

## Public Members

### Constructor

`EchoTcpServer(IPAddress ipAddress, int port, int inBufferSize)`

- Creates a listener for the supplied address and port.
- Stores the stream read buffer size.

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

### `Dispose()`

Stops the listener.

## Notes

This server handles a fixed number of clients sequentially. Concurrent accept/handle loops and cancellation are future work.
