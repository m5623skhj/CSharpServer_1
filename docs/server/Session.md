# CSharpServer/CSharpServer/Network/Session.cs

## Purpose

Connects packet framing to payload-level handlers.

## Namespace

`CSharpServer.Network`

## Types

### `Session`

Owns a `PacketBuffer` and uses `PacketEncoder` for outgoing payloads.

## Public Methods

### `Receive(byte[] data)`

- Appends raw received data to the internal `PacketBuffer`.
- Reads all currently complete packets.
- Invokes the payload handler for each packet in order.
- Serializes concurrent receive calls through packet assembly and handler execution.

### `ReceiveAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)`

- Serializes async processing with synchronous receive calls.
- Awaits decoded packet handlers in packet order.
- Propagates cancellation to packet handling.

### `Send(byte[] payload)`

- Encodes the payload with `PacketEncoder`.
- Sends the encoded packet through the configured packet sender.

### `SendAsync(byte[] payload, CancellationToken cancellationToken)`

Encodes the payload and sends it through the asynchronous packet sender.

## Notes

Sync and async receive calls share one semaphore to protect packet buffer state and handler order.

`Send` synchronization depends on the configured packet sender. Server connections use the thread-safe `StreamConnectionTransport` sender.
