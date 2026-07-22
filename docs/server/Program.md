# CSharpServer/CSharpServer/Program.cs

## Purpose

Server executable entry point.

## Namespace

`CSharpServer`

## Types

### `Program`

Internal static entry class for the server process.

## Behavior

- Creates a `CancellationTokenSource` for process shutdown.
- Registers a `Console.CancelKeyPress` handler.
- Prevents immediate process termination when `Ctrl+C` is pressed and requests cancellation.
- Runs `ServerApplication` asynchronously with the shutdown token.
- Removes the console event handler before exiting.

## Notes

TCP listener setup and argument handling are delegated to `ServerApplication`.
