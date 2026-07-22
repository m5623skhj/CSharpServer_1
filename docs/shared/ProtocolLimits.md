# CSharpServer/CSharpServer/Packet/ProtocolLimits.cs

## Purpose

Defines packet protocol limits shared by server and client code.

## Namespace

`CSharpServer.Packet`

## Types

### `ProtocolLimits`

Static protocol limit definitions.

## Public Members

### `MaxPayloadLength`

The maximum encoded or decoded payload length is 4096 bytes.

Server packet decoding, packet encoding, and client message validation use this same value.
