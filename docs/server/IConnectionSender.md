# CSharpServer/CSharpServer.Networking/Network/IConnectionSender.cs

## Purpose

Public payload-sending boundary supplied to content packet handlers.

## Methods

- `Send(byte[] payload)` encodes and sends one payload synchronously.
- `SendAsync(byte[] payload, CancellationToken cancellationToken)` encodes and sends one
  payload asynchronously while preserving caller cancellation.

The interface intentionally omits raw transport access and connection lifetime operations.
