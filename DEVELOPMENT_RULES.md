# Development Rules

## Client And Server Changes

When a server-side change introduces behavior that is also needed by the client, the client-side implementation must be developed in the same flow.

The client must follow the same TDD practice as the server:

1. Define the expected client behavior with a failing test first.
2. Implement the minimum client code needed to pass the test.
3. Verify the full solution with build and test commands.

This rule applies to shared protocol behavior, packet encoding/decoding, connection handling, echo behavior, and future client/server interaction features.
