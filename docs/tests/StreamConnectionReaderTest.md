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

Test-only stream that waits asynchronously until its read cancellation token is canceled.

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
- `ReadOnceAsync` stops waiting and propagates cancellation without invoking the data handler.
- Repeated async reads reuse the same backing buffer.
- Normal and idle-timeout reads complete stream and handler continuations without posting to a caller synchronization context.
