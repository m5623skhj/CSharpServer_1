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
- `PacketEncoder` rejects null payloads before reading payload length.
- `PacketBuffer` accumulates received bytes and returns complete payloads.
- `PacketBuffer` rejects null byte arrays so missing receive data is not silently treated as empty input.
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

- `Session` owns packet encoding/decoding around sync and async payload handlers, preserves caller cancellation, and restores serialized receive state after cancellation or handler failure.
- `Session` avoids synchronization-context capture across async receive-slot and packet-handler waits.
- `Session.Receive(byte[])` rejects null byte arrays before appending data to the packet buffer.
- `Session`, `Connection`, `StreamConnectionReader`, and `EchoPacketHandler` reject null collaborators at construction.
- `Connection` connects `Session` to a transport and preserves async handler cancellation and failure propagation.
- `StreamConnectionReader` serializes synchronous and asynchronous raw reads from a stream.
- `StreamConnectionReader` reuses one read buffer and passes borrowed memory through the internal pipeline.
- `StreamConnectionReader` avoids synchronization-context capture across async read-slot, stream-read, and handler waits, including idle-timeout reads.
- `StreamConnectionTransport` serializes each sync and async frame write through its flush while allowing close to dispose the underlying stream without waiting for the send lock; interruption timing depends on the concrete stream.
- `StreamConnectionTransport` avoids synchronization-context capture across async slot, write, and flush waits.
- `StreamConnectionTransport` rejects a null stream at construction so transport failures fail at the API boundary.
- `StreamConnectionTransport.Send(byte[])` rejects null byte arrays before stream writes.
- `StreamConnection` sync and async send paths reject null payloads, encode them, and preserve caller cancellation for asynchronous writes and flushes.
- Concurrent echo processing propagates cancellation through packet handlers and async stream writes.
- `StreamConnection` composes stream reader, transport, and connection.
- `StreamConnection` avoids synchronization-context capture while repeating normal and idle-timeout asynchronous reads.
- `StreamConnection` rejects null streams and packet handlers plus non-positive buffer sizes before composition.
- `ServerOptions` validates executable arguments before listener startup.
- `ServerOptions` rejects null argument arrays before reading parser state.
- `ServerOptions` supplies the concurrent client limit and client idle timeout.
- `ServerApplication` owns listener startup and passes validated resource limits to the TCP server.
- `ServerApplication` rejects null options before reading server configuration.
- `EchoTcpServer` accepts TCP clients and handles each as an echo stream connection.
- `EchoTcpServer` validates its bind port as `0..65535`, preserving port `0` for OS-assigned test and runtime binding.
- `EchoTcpServer` can run either for a fixed client count or as a cancellable concurrent accept loop.
- Fixed-count `EchoTcpServer` accepts and handler waits avoid caller synchronization-context capture.
- A semaphore bounds active client handlers, and slots are released on completion, failure, or cancellation.
- Faulted or unexpectedly canceled handlers stop the accept loop immediately and propagate their completion error.
- Fixed-count mode also stops remaining accepts instead of waiting for the configured count after a handler fault or unexpected cancellation.
- `EchoTcpServer` tracks accepted clients at server scope so disposal and accept failures can close every active connection.
- Disposal cancels both asynchronous accept modes, stops the listener, closes active clients, and disposes cancellation and slot resources.
- Disposal interrupts blocked synchronous accepts with `ObjectDisposedException` rather than exposing listener shutdown socket errors.
- Deferred connection-slot disposal is retried after synchronous handlers complete so shutdown during handling does not skip cleanup.
- Each asynchronous client read has a resettable idle timeout so inactive connections cannot remain indefinitely.
- Idle timeout tokens apply only to pending stream reads; packet handlers and writes retain the server cancellation token.
- Concurrent client handlers use cancellation-aware asynchronous stream reads.
- On cancellation, the open-ended `EchoTcpServer` loop closes active clients and waits for handler tasks to finish.
- Client-level malformed packet and connection exceptions are isolated from the server accept loop without swallowing general `InvalidOperationException` failures.

### Content Layer

The content layer defines what to do with decoded payloads.

- `EchoPacketHandler` sends the same payload back.
- `EchoPacketHandler` rejects null payloads before invoking synchronous or asynchronous senders.
- `EchoStreamConnectionFactory` rejects null streams and non-positive buffer sizes before composing network objects.
- `EchoStreamConnectionFactory` wires echo behavior into a `StreamConnection` using one shared transport for echo, send, and close operations.

## Client Layers

The client currently exists as a test and manual verification tool.

- `ClientOptions` rejects empty hosts, validates command-line values, and applies a total request timeout without throwing parsing exceptions.
- `ClientOptions` rejects null argument arrays before reading parser state.
- `ClientOptions` rejects message strings that cannot be encoded as valid UTF-8 without throwing parsing exceptions.
- Client `Program` prints validation errors, sends a request, and converts expected network or protocol failures into exit code `1`.
- `EchoClient` connects to a TCP server, writes and flushes an encoded echo request, waits for one encoded response, and decodes it.
- `EchoClient` applies timeout or caller cancellation across TCP connect, request write, and response read.
- `EchoClient` rejects null host, stream, and message arguments at the public API boundary before network or stream work begins.
- `EchoClient` rejects empty or whitespace-only hosts before network work begins, matching `ClientOptions`.
- `EchoClient` rejects ports outside `1..65535` before network work begins, matching `ClientOptions`.
- `EchoClient` rejects oversized UTF-8 messages before network work begins, matching `ClientOptions`.
- `EchoClient` rejects request strings that cannot be encoded as valid UTF-8 before network work begins, matching `ClientOptions`.
- `EchoClient` stream overloads reject oversized UTF-8 messages before payload allocation and stream writes.
- `EchoClient` strictly validates response UTF-8 and reports invalid text payloads as protocol errors.
- `EchoClient` reads exactly one response frame so later bytes remain available on caller-owned streams.
- Incomplete responses that reach EOF close caller-supplied streams because the pending packet boundary cannot be recovered.
- Invalid response lengths close caller-supplied streams because packet framing can no longer be safely reused.
- Request or response I/O failures close caller-supplied streams because partial packet state may remain.
- A timeout on a caller-supplied stream closes that stream because the request/response framing can no longer be safely reused.
- Synchronous client methods reuse the async request path with a default or caller-supplied timeout.

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
