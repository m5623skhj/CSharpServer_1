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

Test-only stream that records writes, keeps async reads pending until cancellation, and records disposal.

## Test Coverage

- `SendEchoRequest` writes an encoded request packet.
- `SendEchoRequest` decodes one encoded response packet.
- `SendEchoRequest` throws `EndOfStreamException` when the stream closes before a response is received.
- The synchronous stream overload throws `TimeoutException` when the request does not complete.
- `SendEchoRequestAsync` throws `TimeoutException` when the complete request does not finish before the timeout.
- A timed-out stream request closes the stream so it cannot be reused with corrupted protocol state.
- The host/port async overload throws `TimeoutException` when a connected server receives the request but does not respond.
- The host/port cancellation overload propagates cancellation during TCP connection.
