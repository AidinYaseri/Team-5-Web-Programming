---
agent: 'agent'
description: 'Read a GitHub issue and the related code, then restate the problem before any code changes.'
---

You are starting work on GitHub issue #${input:issue:Issue number} in this repository.

1. Use the GitHub MCP server to read the issue, its comments and any linked issues or PRs.
2. Read `.github/copilot-instructions.md` and follow it for the rest of the session.
3. Find and read every file the issue touches (models, service interfaces, controllers, DTOs, tests, `Program.cs`). List them.
4. Restate the problem in your own words: the expected behaviour, the inputs and outputs, the constraints, and what is out of scope.
5. List anything that is unclear or that conflicts with existing code, as questions.

Do not write or change any code in this step. Stop after step 5 and wait for my answers.
