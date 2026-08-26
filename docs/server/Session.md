# CSharpServer/CSharpServer/Network/Session.cs

## Purpose

Connects packet framing to payload-level handlers.

## Namespace

`CSharpServer.Network`

## Types

### `Session`

Owns a `PacketBuffer` and uses `PacketEncoder` for outgoing payloads.

Rejects null payload handlers and packet senders during construction.

## Public Methods

### `Receive(byte[] data)`

- Rejects null byte arrays with `ArgumentNullException`.
- Appends raw received data to the internal `PacketBuffer`.
- Reads all currently complete packets.
- Invokes the payload handler for each packet in order.
- Serializes concurrent receive calls through packet assembly and handler execution.
- Marks receive processing unusable when packet decoding or the handler reports `InvalidDataException`; later receives fail with `ObjectDisposedException` before appending data.

### `ReceiveAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)`

- Serializes async processing with synchronous receive calls.
- Awaits decoded packet handlers in packet order.
- Passes the caller cancellation token to packet handlers.
- Does not append receive data when cancellation occurs while waiting for the receive slot.
- Releases the receive slot when packet handling fails or is canceled.
- Applies the same terminal receive-state rule when packet decoding or the asynchronous handler reports `InvalidDataException`.
- Avoids capturing the caller's synchronization context while waiting for the receive slot or packet handlers.

### `Send(byte[] payload)`

- Encodes the payload with `PacketEncoder`.
- Sends the encoded packet through the configured packet sender.

### `SendAsync(byte[] payload, CancellationToken cancellationToken)`

Encodes the payload and sends it through the asynchronous packet sender with the caller cancellation token.

## Notes

Sync and async receive calls share one semaphore to protect packet buffer state and handler order. The unusable receive state is recorded before that semaphore is released, so queued receives cannot append to a poisoned packet buffer.

Handler failures other than `InvalidDataException` release the receive slot without making the session unusable, preserving the existing recovery behavior for application-level failures.

Async receive internals avoid synchronization-context capture so a synchronously bridged receive does not depend on pumping a UI or single-threaded context before releasing the receive slot.

`Send` synchronization depends on the configured packet sender. Server connections use the thread-safe `StreamConnectionTransport` sender.

The test assembly can inspect the internal available receive slot count for deterministic serialization checks.
