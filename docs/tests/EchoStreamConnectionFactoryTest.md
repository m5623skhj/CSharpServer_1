# CSharpServer/UnitTest/Content/EchoStreamConnectionFactoryTest.cs

## Purpose

Tests echo stream connection wiring.

## Namespace

`UnitTest.Content`

## Types

### `EchoStreamConnectionFactoryTest`

Verifies that the factory returns a connection capable of echoing received packets.

### `AsyncEchoStream`

Test-only stream that rejects sync writes and records async writes.

## Test Coverage

- Factory-created connection reads an encoded packet from a stream.
- The same encoded packet is written back to the stream.
- Echo responses and explicit connection sends use the same stream transport.
- The async read loop returns echo responses through `WriteAsync` instead of synchronous `Write`.
