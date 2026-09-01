# CSharpServer/CSharpServer/Content/EchoPacketHandler.cs

## Purpose

Payload-level echo handler.

## Namespace

`CSharpServer.Content`

## Types

### `EchoPacketHandler`

Implements `IConnectionPacketHandler`, receives decoded payload bytes, and sends the same bytes
back through the callback's `IConnectionSender`.

## Public Methods

### `Handle(IConnectionSender sender, byte[] payload)`

Rejects a null sender or payload, then sends the received payload synchronously.

### `HandleAsync(IConnectionSender sender, byte[] payload, CancellationToken cancellationToken)`

Rejects a null sender or payload, then sends asynchronously while preserving cancellation.

## Notes

This class does not know about packet framing, streams, sockets, or the raw transport.
