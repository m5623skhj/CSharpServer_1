# CSharpServer/CSharpServer/Packet/PacketEncoder.cs

## Purpose

Creates protocol packets from payload bytes.

## Namespace

`CSharpServer.Packet`

## Types

### `PacketEncoder`

Static helper for encoding payloads.

## Public Methods

### `Encode(byte[] payload)`

Returns a new byte array in this format:

```text
[4 bytes: little-endian payload length][payload bytes]
```

The length header is written with an explicit little-endian conversion.

## Notes

This class is shared by server and client code through the `CSharpServer` project reference.
