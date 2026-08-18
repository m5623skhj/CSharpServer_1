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

### `QueueingSynchronizationContext`

Test-only synchronization context that records posted continuations without running them automatically.

## Test Coverage

- Null packet handlers and packet senders are rejected during construction.
- Null byte arrays are rejected before receive processing.
- Complete packet receive invokes the packet handler.
- Incomplete packet data is buffered until complete.
- Multiple received packets are handled in order.
- `Send` encodes payloads before invoking the sender.
- One session's sent packet can be received by another session.
- Concurrent async receive calls do not execute packet handlers at the same time.
- The second async receive remains incomplete while the first handler owns the semaphore slot.
- Concurrent receives verify the semaphore slot is restored afterward.
- Async receive passes the decoded payload and caller cancellation token to the handler.
- Cancellation while waiting for the receive slot does not buffer the canceled receive data.
- Handler failure releases the receive slot so later packets can still be processed.
- Async receive completes without posting its continuation to a caller synchronization context.
- Async send passes the encoded packet and caller cancellation token to the sender.
