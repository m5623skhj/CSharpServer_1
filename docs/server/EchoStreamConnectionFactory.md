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
- Creates a `StreamConnection`.
- Wires the echo handler so responses are encoded and written to the same stream.
- Returns the configured connection.

## Notes

The factory avoids self-referential connection wiring. Echo responses are encoded directly to the stream used by the created connection.
