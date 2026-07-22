# CSharpServer/UnitTest/Application/ServerOptionsTest.cs

## Purpose

Tests server command-line validation.

## Namespace

`UnitTest.Application`

## Types

### `ServerOptionsTest`

Verifies server option defaults and usage-oriented validation failures.

## Test Coverage

- Empty arguments select port `5000`.
- Empty arguments select 100 concurrent clients and a 30-second idle timeout.
- Explicit valid connection and timeout values are preserved.
- Non-numeric and out-of-range ports are rejected with usage text.
- Non-positive or non-numeric connection limits and idle timeouts are rejected with usage text.
- Extra arguments are rejected with usage text.
