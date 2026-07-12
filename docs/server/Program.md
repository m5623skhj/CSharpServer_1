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
- Creates an `EchoTcpServer` bound to `127.0.0.1`.
- Starts the listener.
- Handles one client by calling `AcceptAndHandleOnce()`.

## Notes

The server currently handles one client and then exits. Multi-client loops and graceful shutdown are not implemented yet.
