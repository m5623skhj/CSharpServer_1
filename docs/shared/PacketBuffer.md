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
- Defaults to `ProtocolLimits.MaxPayloadLength`.
- Rejects zero or negative maximum length values.

### `Append(byte[] data)`

Appends newly received bytes to the internal buffer.

### `Append(ReadOnlySpan<byte> data)`

Copies received bytes directly into list storage without creating a sliced intermediate array.

### `TryReadPacket(out byte[]? packet)`

- Returns `false` when the header or payload is incomplete.
- Returns `true` and outputs one payload when a complete packet is available.
- Removes the consumed packet from the internal buffer.
- Throws `InvalidDataException` for negative or oversized payload lengths.
- Reads the length header with an explicit little-endian conversion.
- Reads the header directly from list storage without allocating a temporary array.

## Notes

The class is not thread-safe. Calls should be serialized by the owner.
