# CSharpServer/UnitTest/Application/ExecutableProgramTest.cs

## Purpose

Tests command-line validation and expected network failures at the executable process boundary.

## Namespace

`UnitTest.Application`

## Types

### `ExecutableProgramTest`

Starts the built server and client assemblies as child `dotnet` processes and verifies their observable results.

### `ProcessResult`

Test-only result record containing a child process exit code, standard output, and standard error.

## Test Coverage

- An invalid server port returns exit code `1` and writes server usage text to standard error.
- An invalid client port returns exit code `1` and writes client usage text to standard error.
- An empty client host returns exit code `1` and usage text without an unhandled exception.
- A server bind failure returns exit code `1` and writes a concise network error to standard error.
- A client request timeout returns exit code `1` and writes a concise network error to standard error.
- A server close before response returns exit code `1` without an unhandled exception.
- A malformed response returns exit code `1` with concise protocol-error output.
- Validation failures do not print unhandled-exception text or write to standard output.
- Expected network failures do not print exception type names or write to standard output.
- Child processes are limited to five seconds and are terminated if the limit is exceeded.
