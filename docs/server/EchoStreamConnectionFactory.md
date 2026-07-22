# CSharpServer/CSharpServer/Content/EchoStreamConnectionFactory.cs

## Purpose

Creates a `StreamConnection` wired for echo behavior.

## Namespace

`CSharpServer.Content`

## Types

### `EchoStreamConnectionFactory`

Static factory for composing `EchoPacketHandler` and `StreamConnection`.

## Public Methods

### `Create(Stream stream, int inBufferSize)`

- Creates an `EchoPacketHandler`.
- Creates one `StreamConnectionTransport` for the stream.
- Creates a `StreamConnection`.
- Wires the echo handler and internal connection to the same transport.
- Encodes echo responses and sends them through the transport synchronization boundary.
- Returns the configured connection.

## Notes

The factory avoids self-referential connection wiring while ensuring echo responses, regular sends, and close operations share one transport.
