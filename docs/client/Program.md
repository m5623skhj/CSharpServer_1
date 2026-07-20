# CSharpServer/CSharpClient/Program.cs

## Purpose

Client executable entry point.

## Namespace

`CSharpClient`

## Types

### `Program`

Internal static entry class for the client process.

## Behavior

- Reads host, port, message, and response timeout in milliseconds from command line arguments.
- Defaults to `127.0.0.1`, port `5000`, message `hello`, and a 5000 millisecond response timeout.
- Creates an `EchoClient`.
- Sends one echo request asynchronously with the configured response timeout.
- Prints the decoded echo response.

## Notes

The actual packet send/receive logic is in `EchoClient`.
