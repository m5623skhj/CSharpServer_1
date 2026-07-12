# CSharpServer/CSharpServer/Network/EchoTcpServer.cs

## Purpose

Minimal TCP echo server for one client.

## Namespace

`CSharpServer.Network`

## Types

### `EchoTcpServer`

Wraps `TcpListener` and accepts one echo client.

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

### `Dispose()`

Stops the listener.

## Notes

This server handles one client only. Multi-client accept loops and cancellation are future work.
