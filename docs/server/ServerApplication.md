# CSharpServer/CSharpServer/ServerApplication.cs

## Purpose

Owns the executable server lifetime independently from console signal handling.

## Namespace

`CSharpServer`

## Types

### `ServerApplication`

Starts and runs the loopback TCP echo server until cancellation is requested.

## Public Methods

### `RunAsync(string[] args, CancellationToken cancellationToken)`

- Reads the TCP port from the first command line argument.
- Uses port `5000` when no port is supplied.
- Creates an `EchoTcpServer` bound to `127.0.0.1` with a 4096-byte read buffer.
- Starts the listener and prints its bound endpoint.
- Runs the open-ended concurrent accept loop.
- Returns after cancellation stops accepting clients and active handlers finish.

## Notes

Fixed-client-count APIs remain available on `EchoTcpServer`, but the executable uses the open-ended cancellable mode.
