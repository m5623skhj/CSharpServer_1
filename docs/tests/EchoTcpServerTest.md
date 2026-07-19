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
- Verifies that `AcceptAndHandleConcurrently(2)` can return echo responses to two clients handled by separate tasks.
- Verifies that `AcceptAndHandleConcurrently(CancellationToken)` returns after cancellation while preserving accepted client echo responses.
- Verifies that cancellation closes an already accepted idle client so the server loop can return.
- Verifies that a malformed client packet does not prevent later clients from receiving echo responses.
- Verifies that zero buffer size is rejected by the server constructor.
