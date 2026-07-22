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
- Non-numeric and out-of-range ports are rejected with usage text.
- Extra arguments are rejected with usage text.
