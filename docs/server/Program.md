# CSharpServer/CSharpServer/Program.cs

## Purpose

Server executable entry point.

## Namespace

`CSharpServer`

## Types

### `Program`

Internal static entry class for the server process.

## Behavior

- Reads the TCP port from the first command line argument.
- Uses port `5000` when no port is supplied.
- Reads the number of clients to handle from the second command line argument.
- Uses client count `1` when no client count is supplied.
- Creates an `EchoTcpServer` bound to `127.0.0.1`.
- Starts the listener.
- Handles the configured number of clients by calling `AcceptAndHandle(clientCount)`.

## Notes

The server currently handles a fixed number of clients sequentially and then exits. Concurrent multi-client handling and graceful shutdown are not implemented yet.
