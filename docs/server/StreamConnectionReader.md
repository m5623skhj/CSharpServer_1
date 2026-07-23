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

### `ReadOnceAsync(CancellationToken cancellationToken)`

- Waits asynchronously for exclusive reader access.
- Reads one chunk into the reusable buffer with `Stream.ReadAsync` and the supplied cancellation token.
- Returns `false` at EOF or awaits the async data handler and returns `true`.
- Propagates cancellation through `OperationCanceledException`.

## Constructor Behavior

- Rejects zero or negative buffer sizes.
- Allocates one read buffer for the reader lifetime.

## Notes

Synchronous and asynchronous calls share one `SemaphoreSlim`. Public callbacks receive an independent byte array for compatibility; the internal server pipeline consumes borrowed `ReadOnlyMemory<byte>` before the next read.

The test assembly can inspect the internal available read slot count without adding a public runtime API.
