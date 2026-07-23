# CSharpServer/CSharpServer/Network/StreamConnection.cs

## Purpose

Composes stream reading, stream writing, and packet session handling.

## Namespace

`CSharpServer.Network`

## Types

### `StreamConnection`

High-level connection wrapper for a `Stream`.

## Construction

The public constructor creates a `StreamConnectionTransport` for the supplied stream.

An internal composition constructor accepts an existing transport so factories can share one transport between content handlers and the internal `Connection`.

## Public Methods

### `ReadOnce()`

Reads one chunk from the stream through `StreamConnectionReader`.

### `ReadUntilEnd()`

Repeatedly calls `ReadOnce()` until EOF.

### `ReadUntilEndAsync(CancellationToken cancellationToken)`

Repeatedly awaits `StreamConnectionReader.ReadOnceAsync` until EOF and propagates cancellation.

### `ReadUntilEndAsync(CancellationToken cancellationToken, TimeSpan idleTimeout)`

- Starts a linked timeout for each asynchronous read.
- Resets the idle timeout after every successful read.
- Returns normally when the idle timeout expires.
- Continues to propagate caller-requested cancellation.
- Rejects a zero or negative idle timeout.

### `Send(byte[] payload)`

Sends a payload through the internal `Connection`.

### `SendAsync(byte[] payload, CancellationToken cancellationToken)`

Sends a payload through the cancellation-aware asynchronous connection path.

### `Close()`

Closes the internal connection transport.

## Notes

`ReadUntilEnd()` remains available for the sequential synchronous server flow. Concurrent server flows await asynchronous packet handlers and writes through `ReadUntilEndAsync()`.
