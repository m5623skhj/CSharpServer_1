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

### `TryParse(string[] args, out ServerOptions? options, out string? error)`

- Uses port `5000` when no argument is supplied.
- Accepts one integer port from `0` through `65535`.
- Rejects invalid ports and extra arguments without throwing parsing exceptions.
- Includes `Usage` in validation errors.
