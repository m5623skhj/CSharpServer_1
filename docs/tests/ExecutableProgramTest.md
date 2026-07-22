# CSharpServer/UnitTest/Application/ExecutableProgramTest.cs

## Purpose

Tests server and client command-line validation at the executable process boundary.

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
- Validation failures do not print unhandled-exception text or write to standard output.
- Child processes are limited to five seconds and are terminated if the limit is exceeded.
