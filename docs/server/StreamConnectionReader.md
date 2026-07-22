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

- Allocates a buffer using the configured buffer size.
- Calls `Stream.Read`.
- Returns `false` when EOF is reached.
- Invokes the data handler and returns `true` when bytes are read.
- Serializes concurrent calls so the stream and data handler are accessed by one read operation at a time.

### `ReadOnceAsync(CancellationToken cancellationToken)`

- Waits asynchronously for exclusive reader access.
- Reads one chunk with `Stream.ReadAsync` and the supplied cancellation token.
- Returns `false` at EOF or forwards the read bytes to the data handler and returns `true`.
- Propagates cancellation through `OperationCanceledException`.

## Constructor Behavior

- Rejects zero or negative buffer sizes.

## Notes

Synchronous and asynchronous calls share one `SemaphoreSlim`. Concurrent calls wait until the active read and its data handler complete.
