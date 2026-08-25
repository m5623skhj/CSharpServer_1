# CSharpServer/UnitTest/Application/ServerApplicationTest.cs

## Purpose

Tests the executable server lifetime without sending a real console signal.

## Namespace

`UnitTest.Application`

## Types

### `ServerApplicationTest`

Verifies cancellation behavior for `ServerApplication`.

### `QueueingSynchronizationContext`

Test-only synchronization context that queues posted continuations so application shutdown context capture can be detected deterministically.

## Test Coverage

- `RunAsync` rejects null options before server startup.
- An already canceled token returns without attempting to bind an occupied loopback port.
- `RunAsync` starts with an OS-assigned port and returns after cancellation is requested.
- `RunAsync` completes cancellation without posting its continuation to a caller synchronization context.
