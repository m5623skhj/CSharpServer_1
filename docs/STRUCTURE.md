# Project Structure

## Solution

The solution is organized into three projects.

| Project | Role |
| :--- | :--- |
| `CSharpServer` | Server executable and shared protocol/network/content code. |
| `CSharpClient` | Test client executable for sending echo requests to the server. |
| `UnitTest` | xUnit tests for packet, session, network, content, server, and client behavior. |

## Current Runtime Shape

```text
CSharpClient
  Program
    -> ClientOptions
    -> EchoClient
      -> TcpClient
      -> PacketEncoder
      -> PacketBuffer

CSharpServer
  Program
    -> ServerOptions
    -> ServerApplication
      -> EchoTcpServer
        -> TcpListener
        -> EchoStreamConnectionFactory
          -> EchoPacketHandler
          -> StreamConnection
            -> StreamConnectionReader
            -> Connection
              -> Session
                -> PacketBuffer
                -> PacketEncoder
            -> StreamConnectionTransport
```

## Protocol

Packets use a length-prefixed binary format.

```text
[4 bytes: little-endian payload length][payload bytes]
```

Responsibilities:

- `PacketEncoder` creates length-prefixed packets from payload bytes.
- `PacketBuffer` accumulates received bytes and returns complete payloads.
- `ProtocolLimits` defines the shared 4096-byte maximum payload length used for encoding, decoding, and client validation.
- Server and client both reuse the same packet classes to avoid wire format drift.
- Packet length headers are written and read with explicit little-endian conversions.

## Server Layers

### Packet Layer

The packet layer is pure byte processing.

- It does not know about sockets, streams, sessions, or content.
- It handles packet framing and malformed payload length defense.

### Network Layer

The network layer adapts byte streams and TCP connections into packet sessions.

- `Session` owns packet encoding/decoding around sync and async payload handlers and serializes receive processing.
- `Connection` connects `Session` to a transport.
- `StreamConnectionReader` serializes synchronous and asynchronous raw reads from a stream.
- `StreamConnectionReader` reuses one read buffer and passes borrowed memory through the internal pipeline.
- `StreamConnectionTransport` serializes sync and async writes while allowing close to interrupt a blocked write.
- Concurrent echo processing propagates cancellation through packet handlers and async stream writes.
- `StreamConnection` composes stream reader, transport, and connection.
- `ServerOptions` validates executable arguments before listener startup.
- `ServerOptions` supplies the concurrent client limit and client idle timeout.
- `ServerApplication` owns listener startup and passes validated resource limits to the TCP server.
- `EchoTcpServer` accepts TCP clients and handles each as an echo stream connection.
- `EchoTcpServer` can run either for a fixed client count or as a cancellable concurrent accept loop.
- A semaphore bounds active client handlers, and slots are released on completion, failure, or cancellation.
- Faulted handlers cancel the accept loop immediately and propagate their original exception.
- Each asynchronous client read has a resettable idle timeout so inactive connections cannot remain indefinitely.
- Concurrent client handlers use cancellation-aware asynchronous stream reads.
- On cancellation, the open-ended `EchoTcpServer` loop closes active clients and waits for handler tasks to finish.
- Client-level malformed packet and connection exceptions are isolated from the server accept loop without swallowing general `InvalidOperationException` failures.

### Content Layer

The content layer defines what to do with decoded payloads.

- `EchoPacketHandler` sends the same payload back.
- `EchoStreamConnectionFactory` wires echo behavior into a `StreamConnection` using one shared transport for echo, send, and close operations.

## Client Layers

The client currently exists as a test and manual verification tool.

- `ClientOptions` validates command-line values and applies a total request timeout without throwing parsing exceptions.
- Client `Program` prints validation errors, sends a request, and converts expected network or protocol failures into exit code `1`.
- `EchoClient` connects to a TCP server, sends an encoded echo request, waits for one encoded response, and decodes it.
- `EchoClient` applies timeout or caller cancellation across TCP connect, request write, and response read.

## Process Error Boundaries

- Server `Program` converts listener socket and I/O failures into concise standard-error output and exit code `1`.
- Client `Program` converts socket, I/O, request timeout, and malformed response failures into concise standard-error output and exit code `1`.
- Unexpected programming errors remain unhandled so they are not hidden as operational network failures.

## Tests

Tests are grouped by behavior:

- `UnitTest.Application`: server options, executable lifetime behavior, and server/client process-boundary validation.
- `UnitTest.Packet`: packet framing and codec behavior.
- `UnitTest.Session`: session-level receive/send behavior.
- `UnitTest.Network`: transport, stream connection, TCP server, and loopback integration behavior.
- `UnitTest.Content`: echo handler and echo connection factory behavior.
- `UnitTest.Client`: client request/response behavior.

## Documentation Layout

Markdown documents are split by project responsibility:

- `docs/server`: server executable, server content, and server network files.
- `docs/client`: client executable and client logic files.
- `docs/shared`: protocol files reused by both server and client.
- `docs/tests`: test files grouped by behavior.

Start from `docs/INDEX.md` when navigating documentation.
