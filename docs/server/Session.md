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
- Serializes concurrent receive calls through packet assembly and handler execution.

### `Send(byte[] payload)`

- Encodes the payload with `PacketEncoder`.
- Sends the encoded packet through the configured packet sender.

## Notes

Concurrent `Receive` calls are serialized to protect packet buffer state and handler order.

`Send` synchronization depends on the configured packet sender. Server connections use the thread-safe `StreamConnectionTransport` sender.
