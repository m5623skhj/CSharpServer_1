# CSharpServer/UnitTest/Packet/PacketEncoderTest.cs

## Purpose

Tests packet encoding rules.

## Namespace

`UnitTest.Packet`

## Types

### `PacketEncoderTest`

Verifies `PacketEncoder` output.

## Test Coverage

- Non-empty payload is encoded as a 4-byte little-endian length header followed by payload bytes.
- Empty payload is encoded as a header-only packet with length `0`.
- Payloads larger than the shared protocol limit are rejected.
- A payload exactly matching the shared protocol limit is accepted.
