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

- Opens a `TcpClient`.
- Connects to the target host and port.
- Gets the network stream.
- Delegates request/response handling to the stream overload.

### `SendEchoRequest(Stream stream, string message)`

- Encodes `message` as UTF-8 bytes.
- Uses `PacketEncoder` to create a request packet.
- Writes the request packet to the stream.
- Reads and decodes one response packet with `PacketBuffer`.
- Returns the response as a UTF-8 string.

### `SendEchoRequestAsync(string host, int port, string message, TimeSpan requestTimeout)`

- Rejects zero or negative timeout values before opening a connection.
- Applies one timeout token to TCP connection, request write, and response read.
- Closes the client and stream after the request completes or fails.

### `SendEchoRequestAsync(string host, int port, string message, CancellationToken cancellationToken)`

- Passes caller cancellation to `TcpClient.ConnectAsync`.
- Uses the same token for request write and response read.
- Propagates `OperationCanceledException` to the caller.

### `SendEchoRequestAsync(Stream stream, string message, TimeSpan requestTimeout)`

- Rejects zero or negative timeout values.
- Encodes and writes one echo request packet asynchronously.
- Reads one response packet asynchronously.
- Cancels the wait when the timeout expires.
- Returns the response as a UTF-8 string.

## Failure Behavior

Throws `EndOfStreamException` if the stream closes before a complete response packet is received.

Throws `InvalidDataException` when a response contains an invalid packet length.

The TimeSpan overloads throw `TimeoutException` if the complete request does not finish before the configured timeout.
