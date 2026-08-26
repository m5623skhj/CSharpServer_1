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

### `FailingWriteStream`

Test-only stream that throws a configured `IOException` from synchronous and asynchronous writes, records disposal, and can simulate a close failure.

### `BlockingWriteStream`

Test-only stream that keeps a write active until the test releases it and records close calls.

### `CancellationAwareWriteStream`

Test-only stream that reports cancellation from synchronous writes or keeps an async write pending until cancellation, records disposal, and can simulate a close failure.

### `ConcurrentAsyncWriteStream`

Test-only stream that blocks the first asynchronous write and detects overlapping write calls.

### `QueueingSynchronizationContext`

Test-only synchronization context that records posted continuations without running them automatically.

### `AsynchronouslyCompletingWriteStream`

Test-only stream that keeps an asynchronous write pending until the test completes it without using the caller context.

## Test Coverage

- Constructor rejects a null stream.
- `Send` writes raw data to the stream.
- `Send` flushes the stream after writing a complete frame.
- `Send` rejects null byte arrays before writing.
- `Send` rejects writes after close and returns its send slot after the exception.
- Sync write `IOException` closes the transport, restores the send slot, and prevents later sends.
- A close failure after sync write failure preserves both failures under the original I/O error type.
- Sync write cancellation closes the transport, restores the send slot, and prevents later sends from appending to a potentially partial frame.
- A close failure after sync write cancellation preserves cancellation and both underlying failures.
- `SendAsync` propagates cancellation to an active stream write, closes the transport, and prevents later sends from reusing a potentially partial frame.
- A close failure after active send cancellation does not replace cancellation; both failures remain available.
- Cancellation while waiting for the send slot leaves the active stream open and usable after the current send completes.
- `SendAsync` flushes the stream after writing a complete frame.
- `SendAsync` completes without posting its continuation to a caller synchronization context.
- `SendAsync` rejects writes after close and returns its send slot after the exception.
- Async write `IOException` closes the transport, restores the send slot, and prevents later sends.
- A close failure after async write failure preserves both failures under the original I/O error type.
- Concurrent async sends verify semaphore occupancy and non-overlapping writes.
- `Close` closes the stream.
- Repeated `Close` calls close the stream only once.
- `Close` returns without waiting for a blocked send and disposes the stream; after the test releases the blocked write, that send fails against the disposed stream.
