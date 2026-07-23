# CSharpServer/UnitTest/Session/SessionTest.cs

## Purpose

Tests `Session` receive/send behavior.

## Namespace

`UnitTest.Session`

## Types

### `SessionTest`

Verifies session-level packet framing around payload handlers.

### `ConcurrentAsyncPacketHandler`

Test-only async handler that blocks the first callback and detects overlapping packet callbacks.

## Test Coverage

- Complete packet receive invokes the packet handler.
- Incomplete packet data is buffered until complete.
- Multiple received packets are handled in order.
- `Send` encodes payloads before invoking the sender.
- One session's sent packet can be received by another session.
- Concurrent async receive calls do not execute packet handlers at the same time.
- The second async receive remains incomplete while the first handler owns the semaphore slot.
- Concurrent receives verify the semaphore slot is restored afterward.
