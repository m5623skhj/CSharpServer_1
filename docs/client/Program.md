# CSharpServer/CSharpClient/Program.cs

## Purpose

Client executable entry point.

## Namespace

`CSharpClient`

## Types

### `Program`

Internal static entry class for the client process.

## Behavior

- Reads host, port, and message from command line arguments.
- Defaults to `127.0.0.1`, port `5000`, and message `hello`.
- Creates an `EchoClient`.
- Sends one echo request.
- Prints the decoded echo response.

## Notes

The actual packet send/receive logic is in `EchoClient`.
