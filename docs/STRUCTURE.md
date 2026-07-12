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
    -> EchoClient
      -> TcpClient
      -> PacketEncoder
      -> PacketBuffer

CSharpServer
  Program
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
- Server and client both reuse the same packet classes to avoid wire format drift.

## Server Layers

### Packet Layer

The packet layer is pure byte processing.

- It does not know about sockets, streams, sessions, or content.
- It handles packet framing and malformed payload length defense.

### Network Layer

The network layer adapts byte streams and TCP connections into packet sessions.

- `Session` owns packet encoding/decoding around payload handlers.
- `Connection` connects `Session` to a transport.
- `StreamConnectionReader` reads raw bytes from a stream.
- `StreamConnectionTransport` writes raw bytes to a stream.
- `StreamConnection` composes stream reader, transport, and connection.
- `EchoTcpServer` accepts one TCP client and handles it as an echo stream connection.

### Content Layer

The content layer defines what to do with decoded payloads.

- `EchoPacketHandler` sends the same payload back.
- `EchoStreamConnectionFactory` wires echo behavior into a `StreamConnection`.

## Client Layers

The client currently exists as a test and manual verification tool.

- `Program` parses command line arguments and prints the response.
- `EchoClient` connects to a TCP server, sends an encoded echo request, waits for one encoded response, and decodes it.

## Tests

Tests are grouped by behavior:

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

## Current Intentional Limits

These limits are deliberate and should be addressed in later TDD steps:

- `EchoTcpServer` handles one client through `AcceptAndHandleOnce`.
- `ReadUntilEnd` is synchronous and blocks until stream EOF.
- There is no cancellation token or graceful server shutdown API yet.
- There is no multi-client accept loop yet.
- Send/close/read operations are not thread-safe for concurrent use yet.

Any change that removes one of these limits must add or update tests and documentation in the same workflow.
