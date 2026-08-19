# CSharpServer/CSharpClient/EchoClient.cs

## Purpose

Reusable echo client logic for manual client execution and tests.

## Namespace

`CSharpClient`

## Types

### `EchoClient`

Sends a length-prefixed echo request and reads one length-prefixed response.

## Public Methods

### `SendEchoRequest(string host, int port, string message)`

- Executes the async request path synchronously with a five-second default timeout.
- Rejects a null host or message before opening a connection.
- Rejects empty or whitespace-only hosts before opening a connection.
- Rejects ports outside `1..65535` before opening a connection.
- Rejects messages larger than `ProtocolLimits.MaxPayloadLength` when encoded as UTF-8 before opening a connection.
- Rejects strings that cannot be encoded as valid UTF-8 before opening a connection.

### `SendEchoRequest(string host, int port, string message, TimeSpan requestTimeout)`

- Executes the async host/port path synchronously with the supplied timeout.
- Covers connect, write, and response read.
- Rejects a null host or message before opening a connection.
- Rejects empty or whitespace-only hosts before opening a connection.
- Rejects ports outside `1..65535` before opening a connection.
- Rejects messages larger than `ProtocolLimits.MaxPayloadLength` when encoded as UTF-8 before opening a connection.
- Rejects strings that cannot be encoded as valid UTF-8 before opening a connection.

### `SendEchoRequest(Stream stream, string message)`

- Executes the async stream path synchronously with a five-second default timeout.
- Rejects a null stream or message before writing the request.
- Rejects messages larger than `ProtocolLimits.MaxPayloadLength` when encoded as UTF-8 before allocating the payload.
- Rejects strings that cannot be encoded as valid UTF-8 before writing request bytes.

### `SendEchoRequest(Stream stream, string message, TimeSpan requestTimeout)`

- Executes the async stream path synchronously with the supplied timeout.
- Rejects a null stream or message before writing the request.
- Rejects messages larger than `ProtocolLimits.MaxPayloadLength` when encoded as UTF-8 before allocating the payload.
- Rejects strings that cannot be encoded as valid UTF-8 before writing request bytes.
- Returns the decoded response or throws `TimeoutException` when the request does not complete.

### `SendEchoRequestAsync(string host, int port, string message, TimeSpan requestTimeout)`

- Rejects zero, negative, or .NET timer-limit-exceeding timeout values before opening a connection.
- Rejects a null host or message before opening a connection.
- Rejects empty or whitespace-only hosts before opening a connection.
- Rejects ports outside `1..65535` before opening a connection.
- Rejects messages larger than `ProtocolLimits.MaxPayloadLength` when encoded as UTF-8 before opening a connection.
- Rejects strings that cannot be encoded as valid UTF-8 before opening a connection.
- Applies one timeout token to TCP connection, request write, and response read.
- Closes the client and stream after the request completes or fails.

### `SendEchoRequestAsync(string host, int port, string message, CancellationToken cancellationToken)`

- Rejects a null host or message before opening a connection.
- Rejects empty or whitespace-only hosts before opening a connection.
- Rejects ports outside `1..65535` before opening a connection.
- Rejects messages larger than `ProtocolLimits.MaxPayloadLength` when encoded as UTF-8 before opening a connection.
- Rejects strings that cannot be encoded as valid UTF-8 before opening a connection.
- Passes caller cancellation to `TcpClient.ConnectAsync`.
- Uses the same token for request write and response read.
- Propagates `OperationCanceledException` to the caller.

### `SendEchoRequestAsync(Stream stream, string message, TimeSpan requestTimeout)`

- Rejects zero, negative, or .NET timer-limit-exceeding timeout values before writing the request.
- Rejects a null stream or message before writing the request.
- Rejects messages larger than `ProtocolLimits.MaxPayloadLength` when encoded as UTF-8 before allocating the payload.
- Rejects strings that cannot be encoded as valid UTF-8 before writing request bytes.
- Encodes and writes one echo request packet asynchronously.
- Flushes the complete request packet before waiting for its response.
- Reads exactly one response header and its declared payload asynchronously.
- Leaves bytes belonging to a later response unread on a caller-owned stream.
- Cancels the wait when the timeout expires.
- Closes the supplied stream when EOF arrives before a complete response packet.
- Closes the supplied stream when an invalid packet length makes response framing unusable.
- Closes the supplied stream when request or response I/O fails after the operation begins.
- Closes the supplied stream after timeout because a partial or late response cannot be safely correlated with a later request.
- Preserves `EndOfStreamException` as the primary failure if closing the supplied stream after an incomplete response also fails.
- Preserves `InvalidDataException` as the primary failure if closing the supplied stream after a protocol error also fails.
- Preserves `IOException` as the primary failure if closing the supplied stream after an I/O error also fails.
- Preserves `TimeoutException` as the primary failure if closing the supplied stream also fails, while retaining both underlying exceptions.
- Returns the response as a UTF-8 string and rejects invalid UTF-8 payload bytes as a protocol error.

### `SendEchoRequestAsync(Stream stream, string message, CancellationToken cancellationToken)`

- Rejects a null stream or message before writing the request.
- Rejects oversized or invalid UTF-8 request strings before writing request bytes.
- Rejects an already canceled caller token before writing bytes and leaves the untouched stream open.
- Passes caller cancellation through request write, flush, and response reads.
- Propagates `OperationCanceledException` with the caller token instead of translating it to `TimeoutException`.
- Closes the caller-owned stream after cancellation because a partial request or late response cannot be safely correlated with a later request.
- Preserves cancellation as the primary failure if stream cleanup also fails, retaining both the cancellation and close failures in an inner `AggregateException`.

## Message Boundaries

Empty messages are valid and are encoded as header-only packets. Messages whose UTF-8 representation is exactly `ProtocolLimits.MaxPayloadLength` bytes are also valid; only larger messages are rejected.

## Failure Behavior

Throws `EndOfStreamException` if the stream closes before a complete response packet is received.

Caller-owned streams are closed after an incomplete response reaches EOF. If stream cleanup also fails, the `EndOfStreamException` retains both the incomplete-response and close failures in an inner `AggregateException`.

Throws `InvalidDataException` when a response contains an invalid packet length.

Throws `InvalidDataException` when a complete response payload is not valid UTF-8 instead of silently replacing invalid bytes.

Caller-owned streams are closed after an invalid response length or invalid UTF-8 payload because the peer violated the client's response protocol. If stream cleanup also fails, the `InvalidDataException` retains both the protocol and close failures in an inner `AggregateException`.

Caller-owned streams are also closed after other request or response `IOException` failures because a partial frame may remain. If cleanup fails, the `IOException` retains both I/O failures in an inner `AggregateException`.

The TimeSpan overloads throw `TimeoutException` if the complete request does not finish before the configured timeout.

Timeout values up to `UInt32.MaxValue - 1` milliseconds are supported; larger values are rejected with the caller-facing timeout parameter name.

If caller-owned stream cleanup also fails after timeout, the `TimeoutException` contains an `AggregateException` with both the original cancellation and close failure.

Caller-owned streams must be treated as unusable after a request timeout. The client closes them to enforce this protocol boundary.

Caller-requested cancellation during a stream request follows the same unusable-stream rule, but remains an `OperationCanceledException` associated with the caller token rather than becoming a timeout.

Async internals avoid synchronization-context capture so synchronous wrappers do not deadlock UI or test contexts.

Successful caller-owned stream requests flush their complete request before reading and consume only their own response frame, allowing buffered streams and later frames to support subsequent operations.
