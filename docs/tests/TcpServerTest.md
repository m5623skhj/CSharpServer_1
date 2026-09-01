# CSharpServer/UnitTest/Network/TcpServerTest.cs

## Purpose

Tests the public content injection boundary of the reusable `TcpServer`.

## Test Coverage

- A null connection factory is rejected during construction.
- A non-Echo packet handler is created through the injected connection factory.
- The handler transforms a payload and returns the transformed response over real loopback TCP.
- A connection factory that returns `null` faults the client handler with
  `InvalidOperationException` rather than producing an ambiguous null-reference failure.
