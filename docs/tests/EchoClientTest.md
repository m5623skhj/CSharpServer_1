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

Test-only stream that records writes, keeps async reads pending until cancellation, records disposal, and can simulate a close failure.

## Test Coverage

- `SendEchoRequest` writes an encoded request packet.
- `SendEchoRequest` decodes one encoded response packet.
- The stream overload supports an empty message and header-only response packet.
- The stream overload accepts a UTF-8 message exactly at `ProtocolLimits.MaxPayloadLength`.
- `SendEchoRequest` throws `EndOfStreamException` when the stream closes before a response is received.
- `SendEchoRequest` throws `InvalidDataException` when a response declares a negative payload length.
- `SendEchoRequest` throws `InvalidDataException` when a response declares a payload length above `ProtocolLimits.MaxPayloadLength`.
- `SendEchoRequest` throws `InvalidDataException` and closes the caller-owned stream when a complete response payload is not valid UTF-8.
- `SendEchoRequest` rejects a null caller-supplied stream.
- `SendEchoRequest` rejects a null message before writing a stream request.
- The host/port overload rejects a null host or message before opening a connection.
- The host/port overload rejects empty or whitespace-only hosts before opening a connection.
- The host/port overload rejects invalid ports before opening a connection.
- The host/port overload rejects oversized UTF-8 messages before opening a connection.
- The stream overload rejects oversized UTF-8 messages before writing request bytes.
- The stream overload rejects request strings that cannot be encoded as valid UTF-8 before writing request bytes.
- Incomplete responses that reach EOF close caller-owned streams, and close failures do not replace `EndOfStreamException`.
- Invalid response lengths close caller-owned streams, and close failures do not replace the protocol exception.
- Request and response I/O failures close caller-owned streams, and close failures do not replace the original `IOException`.
- The synchronous stream overload throws `TimeoutException` when the request does not complete.
- `SendEchoRequestAsync` throws `TimeoutException` when the complete request does not finish before the timeout.
- The async stream overload accepts the .NET timer maximum and rejects larger timeout values before writing.
- A timed-out stream request closes the stream so it cannot be reused with corrupted protocol state.
- A stream close failure does not replace the request `TimeoutException`; both underlying failures remain available.
- The host/port async overload throws `TimeoutException` when a connected server receives the request but does not respond.
- The host/port cancellation overload propagates cancellation during TCP connection.
