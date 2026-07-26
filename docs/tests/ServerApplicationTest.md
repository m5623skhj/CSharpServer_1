# CSharpServer/UnitTest/Application/ServerApplicationTest.cs

## Purpose

Tests the executable server lifetime without sending a real console signal.

## Namespace

`UnitTest.Application`

## Types

### `ServerApplicationTest`

Verifies cancellation behavior for `ServerApplication`.

## Test Coverage

- `RunAsync` rejects null options before server startup.
- `RunAsync` starts with an OS-assigned port and returns after cancellation is requested.
