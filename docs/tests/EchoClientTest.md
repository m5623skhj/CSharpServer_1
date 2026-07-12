# CSharpServer/UnitTest/Client/EchoClientTest.cs

## Purpose

Tests `EchoClient` behavior without opening a real TCP connection.

## Namespace

`UnitTest.Client`

## Types

### `EchoClientTest`

Verifies client request encoding and response decoding.

### `ScriptedStream`

Test-only stream that supplies scripted read bytes and records written bytes.

## Test Coverage

- `SendEchoRequest` writes an encoded request packet.
- `SendEchoRequest` decodes one encoded response packet.
- `SendEchoRequest` throws `InvalidOperationException` when the stream closes before a response is received.
