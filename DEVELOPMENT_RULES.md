# Development Rules

## Client And Server Changes

When a server-side change introduces behavior that is also needed by the client, the client-side implementation must be developed in the same flow.

The client must follow the same TDD practice as the server:

1. Define the expected client behavior with a failing test first.
2. Implement the minimum client code needed to pass the test.
3. Verify the full solution with build and test commands.

This rule applies to shared protocol behavior, packet encoding/decoding, connection handling, echo behavior, and future client/server interaction features.

## Automatic Post-Change Review

After completing any code modification, the agent must automatically review the completed change before reporting the work as finished. A separate user request for this review is not required.

Passing tests alone is not sufficient. The review must inspect the actual diff and verify all of the following:

1. **Regression impact:** Identify affected callers, shared APIs, protocol behavior, client/server counterparts, tests, and documentation. Confirm that existing behavior outside the requested scope remains unchanged.
2. **Code placement:** Confirm that each change belongs in the selected class, layer, and file, follows existing dependency direction, and does not introduce unnecessary coupling or misplaced responsibility.
3. **Logical correctness:** Recheck assumptions, boundary conditions, exception behavior, cancellation flow, resource lifetime, and error propagation against the intended behavior.
4. **Concurrency safety:** For asynchronous, socket, or shared-state changes, examine races, duplicate close/dispose paths, semaphore or lock balance, task observation, and cancellation timing.
5. **Scope and simplicity:** Confirm that the diff contains only necessary changes, avoids unrelated refactoring, and does not add speculative abstraction or configuration.
6. **Verification quality:** Confirm that tests cover the changed contract, would detect the relevant regression, and are not dependent on avoidable timing or scheduling assumptions. Run the relevant tests and the full build/test suite when appropriate.
7. **Documentation consistency:** Confirm that file-level, test, protocol, and structure documents still match the implementation.

The final report must state the post-change review result explicitly. If no issue is found, say so. If an issue is found within the already approved scope, correct it using the same test-first workflow and repeat the review. If correcting it would expand the agreed scope or materially change behavior, report the finding and obtain user approval before making the additional change.

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
