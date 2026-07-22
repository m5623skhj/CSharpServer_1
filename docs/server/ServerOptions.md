# CSharpServer/CSharpServer/ServerOptions.cs

## Purpose

Validates server command-line arguments before the TCP listener starts.

## Namespace

`CSharpServer`

## Types

### `ServerOptions`

Immutable validated server startup options.

## Public Members

### `Usage`

Provides the server command-line usage text.

### `Port`

Contains the validated listener port. Port `0` is allowed for OS assignment.

### `MaxConcurrentClients`

Contains the positive maximum number of clients handled at the same time.

### `ClientIdleTimeout`

Contains the positive duration allowed between client reads.

### `TryParse(string[] args, out ServerOptions? options, out string? error)`

- Uses port `5000` when no argument is supplied.
- Defaults to 100 concurrent clients and a 30-second client idle timeout.
- Accepts a port, maximum concurrent client count, and idle timeout in milliseconds.
- Rejects invalid ports, non-positive limits, and extra arguments without throwing parsing exceptions.
- Includes `Usage` in validation errors.
