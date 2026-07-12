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

## Notes

This is a synchronous reader. Async reading and cancellation are not implemented yet.
