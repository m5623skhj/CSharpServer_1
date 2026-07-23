# CSharpServer/UnitTest/Network/StreamConnectionReaderTest.cs

## Purpose

Tests single-read stream reader behavior.

## Namespace

`UnitTest.Network`

## Types

### `StreamConnectionReaderTest`

Verifies `StreamConnectionReader.ReadOnce`.

### `ConcurrentReadTrackingStream`

Test-only stream that detects overlapping read calls while coordinating two reader tasks.

### `CancellationAwareReadStream`

Test-only stream that waits asynchronously until its read cancellation token is canceled.

### `ReadBufferTrackingStream`

Test-only stream that records the backing array supplied to each async read.

## Test Coverage

- When bytes are read, `ReadOnce` calls the handler and returns `true`.
- When EOF is reached, `ReadOnce` does not call the handler and returns `false`.
- Zero buffer size is rejected by the constructor.
- Concurrent `ReadOnce` calls do not overlap stream reads.
- Concurrent read coordination uses explicit request and release signals rather than elapsed-time assertions.
- `ReadOnceAsync` stops waiting and propagates cancellation without invoking the data handler.
- Repeated async reads reuse the same backing buffer.
