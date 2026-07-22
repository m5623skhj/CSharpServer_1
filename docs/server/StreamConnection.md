# CSharpServer/CSharpServer/Network/StreamConnection.cs

## Purpose

Composes stream reading, stream writing, and packet session handling.

## Namespace

`CSharpServer.Network`

## Types

### `StreamConnection`

High-level connection wrapper for a `Stream`.

## Public Methods

### `ReadOnce()`

Reads one chunk from the stream through `StreamConnectionReader`.

### `ReadUntilEnd()`

Repeatedly calls `ReadOnce()` until EOF.

### `ReadUntilEndAsync(CancellationToken cancellationToken)`

Repeatedly awaits `StreamConnectionReader.ReadOnceAsync` until EOF and propagates cancellation.

### `Send(byte[] payload)`

Sends a payload through the internal `Connection`.

### `Close()`

Closes the internal connection transport.

## Notes

`ReadUntilEnd()` remains available for the sequential synchronous server flow. Concurrent server flows use `ReadUntilEndAsync()`.
