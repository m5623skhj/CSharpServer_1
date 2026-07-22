# CSharpServer/CSharpClient/Program.cs

## Purpose

Client executable entry point.

## Namespace

`CSharpClient`

## Types

### `Program`

Internal static entry class for the client process.

## Behavior

- Parses host, port, message, and response timeout through `ClientOptions`.
- Writes validation errors and usage text to standard error and returns exit code `1`.
- Creates an `EchoClient`.
- Sends one echo request asynchronously with the configured response timeout.
- Prints the decoded echo response.
- Returns exit code `0` after a successful response.

## Notes

The actual packet send/receive logic is in `EchoClient`.
