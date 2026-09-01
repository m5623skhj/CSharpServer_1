# CSharpServer/CSharpServer/Network/EchoTcpServer.cs

## Purpose

Echo-specific compatibility facade over the reusable `TcpServer`.

## Behavior

The public constructors preserve the previous Echo server configuration and validation. They
pass `EchoStreamConnectionFactory.Create` to the base `TcpServer`, so accepted clients receive
the same synchronous and asynchronous Echo behavior while listener lifecycle, concurrency
limits, idle timeout, cancellation, and disposal remain implemented by the networking library.

Use `TcpServer` directly for non-Echo content.
