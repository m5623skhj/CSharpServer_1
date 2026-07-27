# CSharpServer/CSharpServer/Content/EchoPacketHandler.cs

## Purpose

Payload-level echo handler.

## Namespace

`CSharpServer.Content`

## Types

### `EchoPacketHandler`

Receives decoded payload bytes and sends the same bytes back.

Rejects null packet senders during construction.

## Public Methods

### `Handle(byte[] payload)`

Rejects a null payload, then passes the received payload to the configured packet sender.

### `HandleAsync(byte[] payload, CancellationToken cancellationToken)`

Rejects a null payload, then passes the payload to the configured asynchronous sender and propagates cancellation.

## Notes

This class does not know about packets, streams, or sockets. It only handles content behavior.
