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

Construction rejects null streams, non-positive buffer sizes, and null packet handlers before composing the reader and connection.

## Public Methods

### `ReadOnce()`

Reads one chunk from the stream through `StreamConnectionReader`. A read or handler `IOException`, or handler `InvalidDataException`, closes the stream and prevents later reads from reusing uncertain packet state.

### `ReadUntilEnd()`

Repeatedly calls `ReadOnce()` until EOF.

### `ReadUntilEndAsync(CancellationToken cancellationToken)`

Repeatedly awaits `StreamConnectionReader.ReadOnceAsync` until EOF and propagates cancellation. Cancellation, `IOException`, or handler `InvalidDataException` during active work closes the stream and prevents later reads from reusing uncertain packet state. The read loop does not capture a caller synchronization context.

### `ReadUntilEndAsync(CancellationToken cancellationToken, TimeSpan idleTimeout)`

- Applies a fresh linked timeout only while each asynchronous stream read is pending.
- Resets the idle timeout after every successful read.
- Returns normally when the idle timeout expires.
- Passes the original caller cancellation token to packet handlers and asynchronous writes.
- Continues to propagate caller-requested cancellation during reads and handlers.
- Accepts idle timeouts up to `UInt32.MaxValue - 1` milliseconds.
- Rejects zero, negative, or .NET timer-limit-exceeding idle timeouts before reading.
- Does not capture a caller synchronization context between reads.

### `Send(byte[] payload)`

Rejects a null payload and sends an encoded packet through the internal `Connection`. A transport `IOException` closes the stream so a partial packet cannot be reused.

### `SendAsync(byte[] payload, CancellationToken cancellationToken)`

Rejects a null payload, sends an encoded packet through the asynchronous connection path, and passes the caller cancellation token to the stream write and flush. Cancellation or `IOException` during active transport I/O closes the stream so a partial packet cannot be reused; cancellation while waiting for the send slot performs no I/O and leaves the active connection open.

### `Close()`

Closes the internal connection transport.

## Notes

`ReadUntilEnd()` remains available for the sequential synchronous server flow. Concurrent server flows await asynchronous packet handlers and writes through `ReadUntilEndAsync()`.
