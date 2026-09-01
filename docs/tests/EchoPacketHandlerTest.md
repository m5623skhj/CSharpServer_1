# CSharpServer/UnitTest/Content/EchoPacketHandlerTest.cs

## Purpose

Tests payload-level echo behavior.

## Namespace

`UnitTest.Content`

## Types

### `EchoPacketHandlerTest`

Verifies that `EchoPacketHandler` sends back the exact payload it receives.

## Test Coverage

- Null synchronous and asynchronous sender arguments are rejected.
- Null payloads are rejected before synchronous or asynchronous sender invocation.
- `Handle` calls the supplied connection sender once.
- The sent payload equals the received payload.
- `HandleAsync` passes the same payload and cancellation token to the supplied sender.
