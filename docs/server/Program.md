# CSharpServer/CSharpServer/Program.cs

## Purpose

Server executable entry point.

## Namespace

`CSharpServer`

## Types

### `Program`

Internal static entry class for the server process.

## Behavior

- Parses arguments through `ServerOptions` before registering shutdown handling.
- Writes validation errors and usage text to standard error and returns exit code `1`.
- Creates a `CancellationTokenSource` for process shutdown.
- Registers a `Console.CancelKeyPress` handler.
- Prevents immediate process termination when `Ctrl+C` is pressed and requests cancellation.
- Runs `ServerApplication` asynchronously with the shutdown token.
- Removes the console event handler before exiting.
- Returns exit code `0` after normal shutdown.

## Notes

TCP listener setup is delegated to `ServerApplication`.
