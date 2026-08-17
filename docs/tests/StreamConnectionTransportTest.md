# CSharpServer/UnitTest/Network/StreamConnectionTransportTest.cs

## Purpose

Tests stream transport behavior.

## Namespace

`UnitTest.Network`

## Types

### `StreamConnectionTransportTest`

Verifies `StreamConnectionTransport` write, flush, and close behavior.

### `TrackingStream`

Test-only stream that records flushes plus whether and how many times it was disposed.

### `BlockingWriteStream`

Test-only stream that keeps a write active until the test releases it and records close calls.

### `CancellationAwareWriteStream`

Test-only stream that keeps an async write pending until cancellation.

### `ConcurrentAsyncWriteStream`

Test-only stream that blocks the first asynchronous write and detects overlapping write calls.

## Test Coverage

- Constructor rejects a null stream.
- `Send` writes raw data to the stream.
- `Send` flushes the stream after writing a complete frame.
- `Send` rejects null byte arrays before writing.
- `Send` rejects writes after close and returns its send slot after the exception.
- `SendAsync` propagates cancellation to the stream write.
- `SendAsync` flushes the stream after writing a complete frame.
- `SendAsync` rejects writes after close and returns its send slot after the exception.
- Concurrent async sends verify semaphore occupancy and non-overlapping writes.
- `Close` closes the stream.
- Repeated `Close` calls close the stream only once.
- `Close` returns without waiting for a blocked send and disposes the stream; after the test releases the blocked write, that send fails against the disposed stream.
