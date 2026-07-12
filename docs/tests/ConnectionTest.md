# CSharpServer/UnitTest/Network/ConnectionTest.cs

## Purpose

Tests `Connection` behavior with a fake transport.

## Namespace

`UnitTest.Network`

## Types

### `ConnectionTest`

Verifies connection-to-session and connection-to-transport behavior.

### `FakeConnectionTransport`

Test-only `IConnectionTransport` implementation that records sent packets and close state.

## Test Coverage

- Raw transport bytes are passed into the session and decoded for the handler.
- Sending a payload writes an encoded packet to transport.
- Closing a connection closes the transport.
