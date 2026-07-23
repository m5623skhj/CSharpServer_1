# CSharpServer/CSharpClient/ClientOptions.cs

## Purpose

Validates client command-line arguments before opening a TCP connection.

## Namespace

`CSharpClient`

## Types

### `ClientOptions`

Immutable validated echo client options.

## Public Members

### `Usage`

Provides the client command-line usage text.

### `Host`, `Port`, `Message`, `RequestTimeout`

Contain the validated connection target, echo message, and total request timeout.

### `TryParse(string[] args, out ClientOptions? options, out string? error)`

- Defaults to host `127.0.0.1`, port `5000`, message `hello`, and a five-second request timeout.
- Rejects empty or whitespace-only hosts before connection startup.
- Accepts ports from `1` through `65535`.
- Rejects messages larger than `ProtocolLimits.MaxPayloadLength` when encoded as UTF-8.
- Requires a positive integer request timeout in milliseconds.
- Rejects invalid values and extra arguments without throwing parsing exceptions.
- Includes `Usage` in validation errors.
