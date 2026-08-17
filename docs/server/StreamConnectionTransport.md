# CSharpServer/CSharpServer/Network/StreamConnectionTransport.cs

## Purpose

Stream-based implementation of `IConnectionTransport`.

## Namespace

`CSharpServer.Network`

## Types

### `StreamConnectionTransport`

Writes raw bytes to a `Stream` and closes it.

Rejects a null stream during construction.

## Public Methods

### `Send(byte[] data)`

Writes and flushes the provided data while holding exclusive send access.

Rejects null byte arrays before waiting for exclusive send access.

Concurrent sends are serialized so packet bytes from separate calls cannot overlap.

Throws `ObjectDisposedException` after the transport has been closed.

### `SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)`

- Waits asynchronously for exclusive send access.
- Writes and flushes one complete frame before releasing send access.
- Propagates cancellation to `Stream.WriteAsync` and `Stream.FlushAsync`.
- Rejects sends after close.

### `Close()`

Closes the stream once without waiting behind an active send.

Repeated close calls have no effect.

## Notes

Sync and async sends share one semaphore, including each frame's flush, so buffered streams expose a complete frame before another send begins. Close uses a separate state lock so it can close the underlying stream without waiting for an active send. Whether stream disposal immediately interrupts a pending write or flush depends on the concrete `Stream` implementation.

The test assembly can inspect the internal available send slot count for deterministic serialization checks.
