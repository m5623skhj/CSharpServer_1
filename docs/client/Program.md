# CSharpServer/CSharpClient/Program.cs

## Purpose

Client executable entry point.

## Namespace

`CSharpClient`

## Types

### `Program`

Internal static entry class for the client process.

## Behavior

- Parses host, port, message, and total request timeout through `ClientOptions`.
- Writes validation errors and usage text to standard error and returns exit code `1`.
- Creates an `EchoClient`.
- Sends one echo request asynchronously with the configured timeout covering connect, write, and read.
- Prints the decoded echo response.
- Returns exit code `0` after a successful response.
- Writes expected socket, I/O, and request timeout errors to standard error without a stack trace.
- Writes malformed response errors as protocol errors without a stack trace.
- Returns exit code `1` when an expected network error occurs.

## Notes

The actual packet send/receive logic is in `EchoClient`. Unexpected programming errors are not converted into network errors.
