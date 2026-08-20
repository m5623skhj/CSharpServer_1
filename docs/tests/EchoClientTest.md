# CSharpServer/UnitTest/Client/EchoClientTest.cs

## Purpose

Tests `EchoClient` stream behavior, total request timeout, and connection cancellation.

## Namespace

`UnitTest.Client`

## Types

### `EchoClientTest`

Verifies client request encoding and response decoding.

### `ScriptedStream`

Test-only stream that supplies scripted read bytes, exposes the unread byte count, records written bytes and flushes, and can reject response reads until the request is flushed.

### `WaitingReadStream`

Test-only stream that records writes, keeps async reads pending until cancellation, records disposal, and can simulate a close failure.

## Test Coverage

- `SendEchoRequest` writes an encoded request packet.
- `SendEchoRequestAsync` flushes the complete request before reading the response.
- `SendEchoRequest` decodes one encoded response packet.
- `SendEchoRequest` does not consume a second response that shares the underlying stream read buffer.
- The stream overload supports an empty message and header-only response packet.
- The stream overload accepts a UTF-8 message exactly at `ProtocolLimits.MaxPayloadLength`.
- `SendEchoRequest` throws `EndOfStreamException` when the stream closes before a response is received.
- Header or payload EOF closes the caller-owned stream when a complete response cannot be read.
- Responses split into one-byte reads are reassembled successfully.
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
- The synchronous stream caller-cancellation overload performs no I/O and leaves the stream open when its token is already canceled.
- The synchronous host/port caller-cancellation overload propagates pre-cancellation with the caller token.
- `SendEchoRequestAsync` throws `TimeoutException` when the complete request does not finish before the timeout.
- The async stream overload accepts the .NET timer maximum and rejects larger timeout values before writing.
- A timed-out stream request closes the stream so it cannot be reused with corrupted protocol state.
- A stream close failure does not replace the request `TimeoutException`; both underlying failures remain available.
- The caller-cancellation stream overload propagates `OperationCanceledException` with the caller token and closes the stream after cancellation.
- An already canceled caller token prevents request bytes from being written and leaves the untouched stream open.
- The caller-cancellation stream overload returns a normal response without closing the stream when cancellation is not requested.
- A stream close failure does not replace caller cancellation; both underlying failures remain available.
- The host/port async overload throws `TimeoutException` when a connected server receives the request but does not respond.
- The host/port cancellation overload propagates cancellation during TCP connection.
