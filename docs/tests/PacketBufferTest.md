# CSharpServer/UnitTest/Packet/PacketBufferTest.cs

## Purpose

Tests packet buffering and decoding rules.

## Namespace

`UnitTest.Packet`

## Types

### `PacketBufferTest`

Verifies `PacketBuffer` behavior for complete, incomplete, and malformed packets.

## Test Coverage

- Null byte arrays are rejected before appending.
- Incomplete headers return `false`.
- Incomplete payloads return `false`.
- Complete packets return payload bytes.
- Multiple complete packets are returned in order.
- Incomplete next packet fragments remain buffered.
- Remaining data can complete a previously incomplete packet.
- Negative payload length throws `InvalidDataException`.
- Payload length exceeding the configured maximum throws `InvalidDataException`.
- An incomplete `Int32.MaxValue` payload returns `false` without overflowing the total packet length calculation.
