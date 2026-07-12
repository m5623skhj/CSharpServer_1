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

## Failure Behavior

Throws `InvalidOperationException` if the stream closes before a complete response packet is received.
