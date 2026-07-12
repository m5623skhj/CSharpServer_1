# CSharpServer/CSharpServer/Network/StreamConnectionTransport.cs

## Purpose

Stream-based implementation of `IConnectionTransport`.

## Namespace

`CSharpServer.Network`

## Types

### `StreamConnectionTransport`

Writes raw bytes to a `Stream` and closes it.

## Public Methods

### `Send(byte[] data)`

Writes the provided data to the stream.

### `Close()`

Closes the stream.

## Notes

The class does not synchronize concurrent writes or close operations.
