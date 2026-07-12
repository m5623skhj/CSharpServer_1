# CSharpServer/CSharpServer/Network/Connection.cs

## Purpose

Adapts a `Session` to a transport.

## Namespace

`CSharpServer.Network`

## Types

### `Connection`

Combines payload handling with an `IConnectionTransport`.

## Public Methods

### `ReceiveFromTransport(byte[] data)`

Passes raw bytes from the transport into the internal `Session`.

### `Send(byte[] payload)`

Sends a payload through the internal `Session`, which encodes it before transport write.

### `Close()`

Closes the underlying transport.

## Notes

This class does not read from the transport directly. Reading is handled by stream-specific adapters.
