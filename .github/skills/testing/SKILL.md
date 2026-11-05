# Testing Skill

## Goal
Provide a consistent testing approach across domain, persistence, API, and Blazor UI changes.

## Scope
- Unit tests for domain rules and component logic.
- Integration tests for data and service orchestration.
- API tests for endpoint contracts and error handling.
- UI behavior checks for important Blazor interactions.

## Workflow
1. Define expected behavior and edge cases.
2. Add or update tests nearest to the changed code.
3. Run focused tests first, then broader suites.
4. Fix failing tests before merge.

## Quality checks
- Prefer deterministic tests.
- Assert observable behavior over implementation details.
- Cover success, validation, and failure paths.
