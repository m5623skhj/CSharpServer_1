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
- Uses concurrent handling when the third command line argument is `concurrent`.
- Creates an `EchoTcpServer` bound to `127.0.0.1`.
- Starts the listener.
- Handles the configured number of clients sequentially by default.
- Handles the configured number of clients concurrently when concurrent mode is requested.

## Notes

The server currently handles a fixed number of clients and then exits. Open-ended accept loops and graceful shutdown are not implemented yet.
