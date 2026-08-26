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

Closes the transport when write or flush throws `OperationCanceledException` or `IOException` because a partial frame may make the connection unsafe to reuse. If closing also fails, the original exception type remains primary and retains both failures in an inner `AggregateException`.

### `SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)`

- Waits asynchronously for exclusive send access.
- Writes and flushes one complete frame before releasing send access.
- Propagates cancellation to `Stream.WriteAsync` and `Stream.FlushAsync`.
- Leaves the stream open when cancellation occurs while waiting for exclusive send access because no frame bytes have been written.
- Closes the stream when cancellation interrupts an active write or flush because a partial frame may make the connection unsafe to reuse.
- Preserves cancellation as the primary failure if closing the stream also fails, retaining both failures in an inner `AggregateException`.
- Closes the transport when write or flush throws `IOException` and preserves both the I/O and close failures if cleanup also fails.
- Avoids capturing the caller's synchronization context across semaphore, write, and flush waits.
- Rejects sends after close.

### `Close()`

Closes the stream once without waiting behind an active send.

Repeated close calls have no effect.

## Notes

Sync and async sends share one semaphore, including each frame's flush, so buffered streams expose a complete frame before another send begins. Cancellation while waiting for that semaphore does not affect another active send. Cancellation reported after sync or async I/O starts closes the transport before releasing the send slot so later sends cannot append to a partial frame. Sync and async `IOException` failures follow the same unusable-transport rule. Close uses a separate state lock so it can close the underlying stream without waiting for an active send. Whether stream disposal immediately interrupts a pending write or flush depends on the concrete `Stream` implementation.

Async send internals avoid synchronization-context capture so callers that synchronously bridge the returned `ValueTask` do not depend on pumping a UI or single-threaded context.

The test assembly can inspect the internal available send slot count for deterministic serialization checks.
