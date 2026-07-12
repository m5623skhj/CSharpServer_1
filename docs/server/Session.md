# CSharpServer/CSharpServer/Network/Session.cs

## Purpose

Connects packet framing to payload-level handlers.

## Namespace

`CSharpServer.Network`

## Types

### `Session`

Owns a `PacketBuffer` and uses `PacketEncoder` for outgoing payloads.

## Public Methods

### `Receive(byte[] data)`

- Appends raw received data to the internal `PacketBuffer`.
- Reads all currently complete packets.
- Invokes the payload handler for each packet in order.

### `Send(byte[] payload)`

- Encodes the payload with `PacketEncoder`.
- Sends the encoded packet through the configured packet sender.

## Notes

The class assumes serialized access. Concurrent `Receive` or `Send` calls are not protected.
