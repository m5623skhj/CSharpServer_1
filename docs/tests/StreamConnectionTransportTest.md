# CSharpServer/UnitTest/Network/StreamConnectionTransportTest.cs

## Purpose

Tests stream transport behavior.

## Namespace

`UnitTest.Network`

## Types

### `StreamConnectionTransportTest`

Verifies `StreamConnectionTransport` write and close behavior.

### `TrackingStream`

Test-only stream that records whether it was disposed.

## Test Coverage

- `Send` writes raw data to the stream.
- `Close` closes the stream.
