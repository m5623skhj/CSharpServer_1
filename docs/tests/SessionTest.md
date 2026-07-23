# CSharpServer/UnitTest/Session/SessionTest.cs

## Purpose

Tests `Session` receive/send behavior.

## Namespace

`UnitTest.Session`

## Types

### `SessionTest`

Verifies session-level packet framing around payload handlers.

### `ConcurrentPacketHandler`

Test-only handler that detects whether two packet callbacks execute at the same time.

## Test Coverage

- Complete packet receive invokes the packet handler.
- Incomplete packet data is buffered until complete.
- Multiple received packets are handled in order.
- `Send` encodes payloads before invoking the sender.
- One session's sent packet can be received by another session.
- Concurrent receive calls do not execute packet handlers at the same time.
- Concurrent receives verify the semaphore slot is held during the first handler and restored afterward.
