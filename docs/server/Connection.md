# CSharpServer/CSharpServer/Network/Connection.cs

## Purpose

Adapts a `Session` to a transport.

## Namespace

`CSharpServer.Network`

## Types

### `Connection`

Combines payload handling with an `IConnectionTransport`.

Rejects a null transport or payload handler during construction.

## Public Methods

### `ReceiveFromTransport(ReadOnlyMemory<byte> data)`

Passes raw bytes from the transport into the internal `Session`. Invalid packet data makes session receive processing unusable, so later receives are rejected before buffering more bytes.

### `ReceiveFromTransportAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)`

Passes raw bytes into asynchronous session processing, forwards the caller cancellation token, and propagates packet handler failures. Packet or handler `InvalidDataException` makes later receives fail before additional data is buffered.

### `Send(byte[] payload)`

Sends a payload through the internal `Session`, which encodes it before transport write. Throws `ObjectDisposedException` without encoding or calling the transport after the connection has been closed.

### `SendAsync(byte[] payload, CancellationToken cancellationToken)`

Encodes and sends a payload through the asynchronous transport path. Throws `ObjectDisposedException` without calling the transport after the connection has been closed.

### `Close()`

Marks the connection closed and internal session receive processing unusable before closing the underlying transport. Later sends and receives therefore throw `ObjectDisposedException` without reaching packet handlers or a transport implementation that does not enforce its own closed state. If transport close fails, that exception is propagated while the connection remains closed.

## Notes

This class does not read from the transport directly. Reading is handled by stream-specific adapters.
