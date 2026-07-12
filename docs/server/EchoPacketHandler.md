# CSharpServer/CSharpServer/Content/EchoPacketHandler.cs

## Purpose

Payload-level echo handler.

## Namespace

`CSharpServer.Content`

## Types

### `EchoPacketHandler`

Receives decoded payload bytes and sends the same bytes back.

## Public Methods

### `Handle(byte[] payload)`

Passes the received payload to the configured packet sender.

## Notes

This class does not know about packets, streams, or sockets. It only handles content behavior.
