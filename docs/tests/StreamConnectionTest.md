# CSharpServer/UnitTest/Network/StreamConnectionTest.cs

## Purpose

Tests composed stream connection behavior.

## Namespace

`UnitTest.Network`

## Types

### `StreamConnectionTest`

Verifies stream read, repeated read, echo wiring, send, and close behavior.

### `TrackingStream`

Test-only stream that records disposal.

### `CancellationAwareReadStream`

Test-only stream that keeps an asynchronous read pending until cancellation.

### `AsyncWriteTrackingStream`

Test-only stream that rejects synchronous writes and records asynchronous write data and cancellation.

## Test Coverage

- Construction rejects null streams and packet handlers.
- Construction rejects zero and negative buffer sizes.
- `ReadOnce` reads an encoded packet and invokes the payload handler.
- `ReadUntilEnd` handles packets split across multiple reads.
- `ReadUntilEndAsync` handles packets split across multiple asynchronous reads.
- `ReadUntilEndAsync` stops waiting and propagates cancellation.
- The idle-timeout overload returns normally when a stream read remains idle.
- The idle-timeout overload still propagates caller-requested cancellation.
- Packet handlers receive the caller token rather than the read-only idle timeout token.
- Echo handler wiring writes the same encoded packet back to the stream.
- `Send` writes an encoded packet to the stream.
- `Send` and `SendAsync` reject null payloads.
- `SendAsync` writes the encoded packet asynchronously and forwards the caller cancellation token.
- `Close` closes the stream.
