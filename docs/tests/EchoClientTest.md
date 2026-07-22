# CSharpServer/UnitTest/Client/EchoClientTest.cs

## Purpose

Tests `EchoClient` stream behavior, total request timeout, and connection cancellation.

## Namespace

`UnitTest.Client`

## Types

### `EchoClientTest`

Verifies client request encoding and response decoding.

### `ScriptedStream`

Test-only stream that supplies scripted read bytes and records written bytes.

### `WaitingReadStream`

Test-only stream that records writes and keeps async reads pending until cancellation.

## Test Coverage

- `SendEchoRequest` writes an encoded request packet.
- `SendEchoRequest` decodes one encoded response packet.
- `SendEchoRequest` throws `InvalidOperationException` when the stream closes before a response is received.
- `SendEchoRequestAsync` throws `TimeoutException` when the complete request does not finish before the timeout.
- The host/port async overload throws `TimeoutException` when a connected server receives the request but does not respond.
- The host/port cancellation overload propagates cancellation during TCP connection.
