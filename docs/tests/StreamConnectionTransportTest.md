# CSharpServer/UnitTest/Network/StreamConnectionTransportTest.cs

## Purpose

Tests stream transport behavior.

## Namespace

`UnitTest.Network`

## Types

### `StreamConnectionTransportTest`

Verifies `StreamConnectionTransport` write and close behavior.

### `TrackingStream`

Test-only stream that records whether and how many times it was disposed.

### `BlockingWriteStream`

Test-only stream that keeps a write active until the test releases it and records close calls.

## Test Coverage

- `Send` writes raw data to the stream.
- `Close` closes the stream.
- Repeated `Close` calls close the stream only once.
- `Close` waits for an active `Send` to complete before closing the stream.
