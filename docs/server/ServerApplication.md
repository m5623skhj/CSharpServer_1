# CSharpServer/CSharpServer/ServerApplication.cs

## Purpose

Owns the executable server lifetime independently from console signal handling.

## Namespace

`CSharpServer`

## Types

### `ServerApplication`

Starts and runs the loopback TCP echo server until cancellation is requested.

## Public Methods

### `RunAsync(ServerOptions options, CancellationToken cancellationToken)`

- Rejects null options before reading server configuration.
- Uses a previously validated `ServerOptions` instance.
- Creates an `EchoTcpServer` bound to `127.0.0.1` with a 4096-byte read buffer.
- Passes the validated concurrent client limit and client idle timeout to the server.
- Starts the listener and prints its bound endpoint.
- Runs the open-ended concurrent accept loop.
- Returns after cancellation stops accepting clients and active handlers finish.
- Does not capture a caller synchronization context while awaiting server shutdown.

## Notes

Fixed-client-count APIs remain available on `EchoTcpServer`, but the executable uses the open-ended cancellable mode.
