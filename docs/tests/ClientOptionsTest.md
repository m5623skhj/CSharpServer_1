# CSharpServer/UnitTest/Client/ClientOptionsTest.cs

## Purpose

Tests client command-line validation.

## Namespace

`UnitTest.Client`

## Types

### `ClientOptionsTest`

Verifies client option defaults, explicit values, and usage-oriented validation failures.

## Test Coverage

- Empty arguments select all client defaults.
- Four valid arguments populate every option.
- Empty and whitespace-only hosts are rejected with usage text.
- Non-numeric and out-of-range ports are rejected with usage text.
- Non-numeric and non-positive request timeouts are rejected with usage text.
- Messages whose UTF-8 byte length exceeds the shared payload limit are rejected with usage text.
- Extra arguments are rejected with usage text.
