# CSharpServer.Networking

## Purpose

`CSharpServer.Networking` is the reusable, packable class library shared by clients and
servers. It owns length-prefixed packet framing, serialized stream reads and writes,
connection/session lifecycle, and bounded TCP client hosting.

The project does not reference the Echo content or either executable project.

## Public Composition

Content implements `IConnectionPacketHandler` and creates a `StreamConnection` with that
handler. The handler receives an `IConnectionSender`, which exposes encoded payload sends but
does not expose the raw transport, read loop, or close operation.

`TcpServer` accepts a connection factory:

```csharp
new TcpServer(
    ipAddress,
    port,
    inBufferSize,
    (stream, bufferSize) => new StreamConnection(stream, bufferSize, packetHandler));
```

The factory is invoked concurrently for different clients and must be thread-safe. It must
return a new connection bound to the supplied stream. Returning `null` is rejected with
`InvalidOperationException`.

## Package

The project produces the `CSharpServer.Networking` NuGet package. Consumers should reference a
specific package version rather than a sibling-repository path. During local development the
package can be written to a local NuGet feed with `dotnet pack`.

## Concurrency Boundary

Receive handlers are serialized per connection, and stream writes are serialized per
transport. Handlers for different clients may run concurrently. Shared game state therefore
requires its own synchronization or, preferably, input handoff to a single authoritative game
loop.
