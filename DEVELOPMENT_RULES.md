# Development Rules

## Client And Server Changes

When a server-side change introduces behavior that is also needed by the client, the client-side implementation must be developed in the same flow.

The client must follow the same TDD practice as the server:

1. Define the expected client behavior with a failing test first.
2. Implement the minimum client code needed to pass the test.
3. Verify the full solution with build and test commands.

This rule applies to shared protocol behavior, packet encoding/decoding, connection handling, echo behavior, and future client/server interaction features.

## Documentation Updates

When code is modified, the related Markdown documentation must be reviewed in the same workflow.

If the change affects behavior, project structure, public APIs, protocol rules, tests, or client/server responsibilities, update or add Markdown documentation before considering the work complete.

The documentation review must include:

1. Checking whether existing documents describe the changed behavior.
2. Updating outdated descriptions.
3. Adding new documents when a new module, project, class, protocol rule, or workflow is introduced.
4. Keeping structure documents and file-level documents consistent with the current `.cs` files.

This rule applies to server code, client code, shared packet logic, tests, and future tooling or infrastructure code.

Documentation should be placed by responsibility:

- Server code documents go under `docs/server`.
- Client code documents go under `docs/client`.
- Shared protocol documents go under `docs/shared`.
- Test documents go under `docs/tests`.
- Project-wide structure documents stay directly under `docs`.
