# CSharpServer/CSharpServer/Network/StreamConnectionReader.cs

## Purpose

Reads raw bytes from a stream one read at a time.

## Namespace

`CSharpServer.Network`

## Types

### `StreamConnectionReader`

Reads from a `Stream` and forwards read bytes to a data handler.

## Public Methods

### `ReadOnce()`

- Reuses the buffer allocated during construction.
- Calls `Stream.Read`.
- Returns `false` when EOF is reached.
- Invokes the data handler and returns `true` when bytes are read.
- Serializes concurrent calls so the stream and data handler are accessed by one read operation at a time.
- Marks the reader unusable and closes the stream if the read or handler reports cancellation after exclusive read access is acquired.

### `ReadOnceAsync(CancellationToken cancellationToken)`

- Waits asynchronously for exclusive reader access.
- Reads one chunk into the reusable buffer with `Stream.ReadAsync` and the supplied cancellation token.
- Returns `false` at EOF or awaits the async data handler and returns `true`.
- Propagates cancellation through `OperationCanceledException`.
- Leaves the stream open when cancellation occurs while waiting for exclusive reader access because no read or handler work has started.
- Marks the reader unusable and closes the stream when cancellation interrupts an active read or handler, preventing later reads from reusing uncertain packet state.
- Preserves cancellation as the primary failure if closing the stream also fails, retaining both failures in an inner `AggregateException`.
- Avoids capturing the caller's synchronization context across reader-slot, stream-read, and handler waits.

## Internal Async Read Behavior

The idle-timeout overload accepts values up to `UInt32.MaxValue - 1` milliseconds and rejects values outside the supported positive range before reading. It uses a linked token only for the pending stream read. An idle timeout returns `false`; caller cancellation during an active read or handler closes the stream and propagates. After bytes arrive, it invokes the async data handler with the original caller cancellation token so content processing and writes are not classified as client idle time. This overload also avoids synchronization-context capture across its asynchronous waits.

## Constructor Behavior

- Rejects a null stream.
- Rejects a null data handler.
- Rejects zero or negative buffer sizes.
- Allocates one read buffer for the reader lifetime.

## Notes

Synchronous and asynchronous calls share one `SemaphoreSlim`. Public callbacks receive an independent byte array for compatibility; the internal server pipeline consumes borrowed `ReadOnlyMemory<byte>` before the next read.

Once active work is canceled, the unusable state is recorded before stream cleanup. Later reads therefore fail with `ObjectDisposedException` even if stream cleanup itself throws.

Async read internals do not depend on pumping a UI or single-threaded caller context before invoking the handler or releasing the read slot.

The test assembly can inspect the internal available read slot count without adding a public runtime API.
