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

## Test Coverage

- When bytes are read, `ReadOnce` calls the handler and returns `true`.
- When EOF is reached, `ReadOnce` does not call the handler and returns `false`.
- Zero buffer size is rejected by the constructor.
- Concurrent `ReadOnce` calls do not overlap stream reads.
- `ReadOnceAsync` stops waiting and propagates cancellation without invoking the data handler.
