# CSharpServer/CSharpServer/Packet/PacketBuffer.cs

## Purpose

Accumulates raw received bytes and extracts complete payloads from length-prefixed packets.

## Namespace

`CSharpServer.Packet`

## Types

### `PacketBuffer`

Stateful packet decoder for stream-oriented TCP data.

## Public Members

### Constructor

`PacketBuffer(int maxPayloadLength = 4096)`

- Stores a maximum allowed payload length.
- Rejects zero or negative maximum length values.

### `Append(byte[] data)`

Appends newly received bytes to the internal buffer.

### `TryReadPacket(out byte[]? packet)`

- Returns `false` when the header or payload is incomplete.
- Returns `true` and outputs one payload when a complete packet is available.
- Removes the consumed packet from the internal buffer.
- Throws `InvalidOperationException` for negative or oversized payload lengths.
- Reads the length header with an explicit little-endian conversion.

## Notes

The class is not thread-safe. Calls should be serialized by the owner.
