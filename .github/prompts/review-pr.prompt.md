---
agent: 'agent'
description: 'Review a pull request diff against the repository rules and record every finding.'
---

Review pull request #${input:pr:PR number} in this repository. Use the GitHub MCP server to read the PR description and the full diff.

Check the diff against `.github/copilot-instructions.md`, especially section 6 (change checklist). Look for:

- Bugs: wrong logic, null handling, edge cases, DI or startup problems.
- Layer violations: business logic in controllers, entities returned from endpoints, `Animal.Domain` referencing other projects.
- Missing or weak tests: new behaviour without a test, tests that would still pass if the code was broken.
- Validation and status codes on endpoints.
- Secrets, PII or real data in code, config, seed data or tests.
- Docs and diagrams that no longer match the code.
- Files that should not be in the diff (`bin/`, `obj/`, `.vs/`, `*.user`).

Output a table with one row per finding: number, severity (High, Medium, Low), file and line, what is wrong, and a suggested fix. Do not change any code. A person decides for each finding whether it is fixed or deferred.
