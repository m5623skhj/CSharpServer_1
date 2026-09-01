# CSharpServer/CSharpServer.Networking/Network/TcpServer.cs

## Purpose

Reusable TCP listener with injected `StreamConnection` creation, bounded concurrent handling,
idle connection cleanup, cancellation, and deterministic disposal.

## Construction

`TcpServer(IPAddress ipAddress, int port, int inBufferSize,
Func<Stream, int, StreamConnection> connectionFactory)` uses defaults of 100 concurrent clients
and a 30-second per-read idle timeout.

The extended constructor also accepts `maxConcurrentClients` and `clientIdleTimeout`.

Construction rejects a null address or connection factory, ports outside `0..65535`,
non-positive buffer and client-limit values, and invalid timer delays. A factory that returns
`null` causes `InvalidOperationException` in the client handler.

The connection factory may run concurrently for different accepted clients. It must be
thread-safe and return a new connection using the supplied stream and buffer size.

## Public Members

- `Port` returns the bound port after `Start` and rejects access before startup or after
  disposal.
- `Start` starts the listener and is serialized with disposal.
- `AcceptAndHandleOnce` handles one client synchronously.
- `AcceptAndHandle(int clientCount)` handles a fixed number of clients sequentially.
- `AcceptAndHandleConcurrently(int clientCount)` accepts and handles a fixed number of clients
  concurrently.
- `AcceptAndHandleConcurrently(CancellationToken cancellationToken)` accepts until canceled.
- `Dispose` cancels accepts, stops the listener, closes tracked clients, and completes resource
  cleanup before returning.

## Concurrency And Failure Behavior

One semaphore bounds synchronous and asynchronous active handlers. Slots are released on
success, failure, cancellation, and shutdown. Accepted clients remain tracked until handler
completion so cancellation and disposal can close them.

Unexpected handler faults or cancellations stop remaining accepts and are propagated. Expected
server-token cancellation is normalized as shutdown. Client I/O, socket, disposal, and malformed
packet failures are isolated so a bad client does not stop the server loop. Async waits do not
capture a caller synchronization context.
