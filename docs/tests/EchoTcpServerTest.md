# CSharpServer/UnitTest/Network/EchoTcpServerTest.cs

## Purpose

Tests real loopback TCP echo behavior.

## Namespace

`UnitTest.Network`

## Types

### `EchoTcpServerTest`

Verifies `EchoTcpServer` and `EchoClient` integration over loopback TCP.

## Test Coverage

- Starts `EchoTcpServer` on loopback with OS-assigned port.
- Runs one server accept in a background task.
- Uses `EchoClient` to send `hello`.
- Verifies that the response is `hello`.
- Verifies an empty message round trip over the real loopback connection.
- Waits for the one-client server task to finish.
- Verifies that `AcceptAndHandle(2)` can return echo responses to two clients sequentially.
- Verifies that `AcceptAndHandleConcurrently(2)` can return echo responses to two asynchronously handled clients.
- Verifies that `AcceptAndHandleConcurrently(CancellationToken)` returns after cancellation while preserving accepted client echo responses.
- Verifies that cancellation stops an already accepted idle client's asynchronous read so the server loop can return.
- Uses a completed echo round trip instead of an arbitrary delay to prove the client was accepted before cancellation.
- Verifies that disposal closes active clients and completes the open-ended accept loop.
- Verifies that disposal cancels remaining accepts and completes the fixed-count accept loop.
- Verifies the configured client semaphore has no available slot while one client is active.
- Verifies that a second client is actually waiting for a slot before the first client is released.
- Verifies that an idle client is closed after its configured timeout.
- Verifies that an unexpected client handler fault stops accept processing and propagates immediately.
- Verifies that fixed-count mode propagates a handler fault without waiting for the remaining clients to connect.
- Verifies that a malformed client packet does not prevent later clients from receiving echo responses.
- Verifies that out-of-range ports, null IP address, and zero or negative buffer size, connection limit, and idle timeout are rejected by the server constructor.
- Verifies that sequential and fixed-count concurrent accept methods reject zero or negative client counts before accepting clients.
- Verifies that disposal makes connection slot state unavailable when no accept loop is running.
- Verifies that disposal also makes connection slot state unavailable after a synchronous handler completes.
- Verifies that disposal is idempotent.
- Verifies that start plus all sequential and concurrent accept entry points reject calls after disposal.
- Repeats concurrent start and disposal to verify that shutdown cannot leave the listener reopened.
