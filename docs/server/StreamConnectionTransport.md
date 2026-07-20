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

Writes the provided data to the stream while holding the transport synchronization lock.

Concurrent sends are serialized so packet bytes from separate calls cannot overlap.

Throws `ObjectDisposedException` after the transport has been closed.

### `Close()`

Waits for an active send to finish and closes the stream once.

Repeated close calls have no effect.

## Notes

The class serializes send and close operations with one lock. A close cannot dispose the stream while a send is writing.
