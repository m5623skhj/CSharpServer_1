# CSharpServer/UnitTest/Content/EchoStreamConnectionFactoryTest.cs

## Purpose

Tests echo stream connection wiring.

## Namespace

`UnitTest.Content`

## Types

### `EchoStreamConnectionFactoryTest`

Verifies that the factory returns a connection capable of echoing received packets.

### `ConcurrentWriteTrackingStream`

Test-only stream that coordinates two writes and detects whether they overlap.

## Test Coverage

- Factory-created connection reads an encoded packet from a stream.
- The same encoded packet is written back to the stream.
- Echo responses and explicit connection sends are serialized through the shared transport.
