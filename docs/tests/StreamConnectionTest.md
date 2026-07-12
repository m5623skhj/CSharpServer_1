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

## Test Coverage

- `ReadOnce` reads an encoded packet and invokes the payload handler.
- `ReadUntilEnd` handles packets split across multiple reads.
- Echo handler wiring writes the same encoded packet back to the stream.
- `Send` writes an encoded packet to the stream.
- `Close` closes the stream.
