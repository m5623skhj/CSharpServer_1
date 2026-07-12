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
- Wires the echo handler so responses are sent through the same connection.
- Returns the configured connection.

## Notes

The factory hides the self-referential wiring needed for echo behavior.
