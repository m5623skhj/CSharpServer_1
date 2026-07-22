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
- Waits for the one-client server task to finish.
- Verifies that `AcceptAndHandle(2)` can return echo responses to two clients sequentially.
- Verifies that `AcceptAndHandleConcurrently(2)` can return echo responses to two asynchronously handled clients.
- Verifies that `AcceptAndHandleConcurrently(CancellationToken)` returns after cancellation while preserving accepted client echo responses.
- Verifies that cancellation stops an already accepted idle client's asynchronous read so the server loop can return.
- Uses a completed echo round trip instead of an arbitrary delay to prove the client was accepted before cancellation.
- Verifies that a second client is not handled while the configured single connection slot is occupied.
- Verifies that an idle client is closed after its configured timeout.
- Verifies that a malformed client packet does not prevent later clients from receiving echo responses.
- Verifies that zero buffer size, connection limit, and idle timeout are rejected by the server constructor.
