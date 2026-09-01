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

- Rejects a null stream and non-positive buffer sizes at the factory boundary.
- Creates an `EchoPacketHandler`.
- Creates a `StreamConnection` through its public `IConnectionPacketHandler` constructor.
- Lets the connection supply a restricted sender to synchronous and asynchronous handler calls.
- Returns the configured connection.

## Notes

The factory uses only public networking-library APIs and remains in the Echo content assembly.
