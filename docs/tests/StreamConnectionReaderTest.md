# CSharpServer/UnitTest/Network/StreamConnectionReaderTest.cs

## Purpose

Tests single-read stream reader behavior.

## Namespace

`UnitTest.Network`

## Types

### `StreamConnectionReaderTest`

Verifies `StreamConnectionReader.ReadOnce`.

### `ConcurrentAsyncReadTrackingStream`

Test-only async stream that blocks the first read and detects overlapping read calls.

### `CancellationAwareReadStream`

Test-only stream that waits asynchronously until its read cancellation token is canceled, records disposal, and can simulate a close failure.

### `ReadBufferTrackingStream`

Test-only stream that records the backing array supplied to each async read.

### `QueueingSynchronizationContext`

Test-only synchronization context that records posted continuations without running them automatically.

### `AsynchronouslyCompletingReadStream`

Test-only stream that keeps a read pending until the test supplies one byte without using the caller context.

## Test Coverage

- Null stream and data handler constructor arguments are rejected.
- When bytes are read, `ReadOnce` calls the handler and returns `true`.
- When EOF is reached, `ReadOnce` does not call the handler and returns `false`.
- Zero buffer size is rejected by the constructor.
- Concurrent `ReadOnceAsync` calls do not overlap stream reads.
- The second async read remains incomplete while the first read owns the semaphore slot.
- Concurrent reads verify the semaphore slot is restored after completion.
- Cancellation during an active `ReadOnceAsync` closes the stream, restores the read slot, prevents later reads, and does not invoke the data handler.
- A close failure after active read cancellation does not replace cancellation; both failures remain available and the reader remains unusable.
- Cancellation while waiting for the read slot leaves the active stream open and usable after the current read completes.
- Repeated async reads reuse the same backing buffer.
- Normal and idle-timeout reads complete stream and handler continuations without posting to a caller synchronization context.
