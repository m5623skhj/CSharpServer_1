# CSharpServer/UnitTest/Network/ConnectionTest.cs

## Purpose

Tests `Connection` behavior with a fake transport.

## Namespace

`UnitTest.Network`

## Types

### `ConnectionTest`

Verifies connection-to-session and connection-to-transport behavior.

### `FakeConnectionTransport`

Test-only `IConnectionTransport` implementation that records sent packets and close state and can simulate a close failure.

## Test Coverage

- Null transport plus delegate and public packet handler constructor arguments are rejected.
- Raw transport bytes are passed into the session and decoded for the handler.
- Asynchronous receive passes the decoded payload and caller cancellation token to the async handler.
- Asynchronous receive propagates the original handler exception.
- Sending a payload writes an encoded packet to transport.
- Sending a payload asynchronously writes an encoded packet through the async transport contract.
- Closing a connection closes the transport.
- Synchronous and asynchronous receives after close throw `ObjectDisposedException` without invoking their packet handlers.
- Synchronous and asynchronous sends after close throw `ObjectDisposedException` without reaching a transport that does not enforce its own closed state.
- A transport close failure is propagated while the connection remains closed to later sends.
