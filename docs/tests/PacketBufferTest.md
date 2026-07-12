# CSharpServer/UnitTest/Packet/PacketBufferTest.cs

## Purpose

Tests packet buffering and decoding rules.

## Namespace

`UnitTest.Packet`

## Types

### `PacketBufferTest`

Verifies `PacketBuffer` behavior for complete, incomplete, and malformed packets.

## Test Coverage

- Incomplete headers return `false`.
- Incomplete payloads return `false`.
- Complete packets return payload bytes.
- Multiple complete packets are returned in order.
- Incomplete next packet fragments remain buffered.
- Remaining data can complete a previously incomplete packet.
- Negative payload length throws `InvalidOperationException`.
- Payload length exceeding the configured maximum throws `InvalidOperationException`.
