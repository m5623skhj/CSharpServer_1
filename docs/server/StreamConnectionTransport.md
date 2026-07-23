# CSharpServer/CSharpServer/Network/StreamConnectionTransport.cs

## Purpose

Stream-based implementation of `IConnectionTransport`.

## Namespace

`CSharpServer.Network`

## Types

### `StreamConnectionTransport`

Writes raw bytes to a `Stream` and closes it.

## Public Methods

### `Send(byte[] data)`

Writes the provided data while holding exclusive send access.

Concurrent sends are serialized so packet bytes from separate calls cannot overlap.

Throws `ObjectDisposedException` after the transport has been closed.

### `SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)`

- Waits asynchronously for exclusive send access.
- Propagates cancellation to `Stream.WriteAsync`.
- Rejects sends after close.

### `Close()`

Closes the stream once without waiting behind an active send.

Repeated close calls have no effect.

## Notes

Sync and async sends share one semaphore. Close uses a separate state lock so it can interrupt a blocked stream write.
