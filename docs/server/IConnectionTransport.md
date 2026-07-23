# CSharpServer/CSharpServer/Network/IConnectionTransport.cs

## Purpose

Defines the transport boundary used by `Connection`.

## Namespace

`CSharpServer.Network`

## Types

### `IConnectionTransport`

Interface for sending raw encoded packet bytes and closing the underlying transport.

## Members

### `Send(byte[] data)`

Writes raw bytes to the transport.

### `SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)`

Writes raw bytes asynchronously and propagates cancellation to the transport.

### `Close()`

Closes the transport.

## Notes

Implementations decide how bytes are physically written, for example to a `Stream`.
