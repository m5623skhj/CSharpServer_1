# CSharpServer.Networking

Length-prefixed TCP connection primitives for C# clients and servers.

Implement `IConnectionPacketHandler` for content behavior, create a `StreamConnection` for each
accepted stream, and inject that connection factory into `TcpServer`.

```csharp
var server = new TcpServer(
    IPAddress.Loopback,
    port: 7777,
    inBufferSize: 4096,
    (stream, bufferSize) =>
        new StreamConnection(stream, bufferSize, packetHandler));
```

Handlers receive an `IConnectionSender` for synchronous or asynchronous payload replies. Calls
for one connection are serialized, but handlers for different clients can run concurrently.
