# CSharpServer/UnitTest/Packet/PacketCodecTest.cs

## Purpose

Tests encoder and buffer compatibility.

## Namespace

`UnitTest.Packet`

## Types

### `PacketCodecTest`

Verifies that `PacketEncoder` output can be read by `PacketBuffer`.

## Test Coverage

- Encoding a payload and appending it to `PacketBuffer` returns the original payload.
