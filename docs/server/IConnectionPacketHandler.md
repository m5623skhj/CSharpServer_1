# CSharpServer/CSharpServer.Networking/Network/IConnectionPacketHandler.cs

## Purpose

Public content boundary for decoded packet payloads.

## Methods

- `Handle(IConnectionSender sender, byte[] payload)` handles a payload synchronously.
- `HandleAsync(IConnectionSender sender, byte[] payload, CancellationToken cancellationToken)`
  handles a payload asynchronously and receives the connection read-loop cancellation token.

Each callback receives the sender associated with the connection that produced the payload.
Callbacks are serialized for one connection, but callbacks from different connections may run
concurrently.
