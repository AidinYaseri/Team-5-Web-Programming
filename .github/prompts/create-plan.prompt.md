---
agent: 'agent'
description: 'Turn a GitHub issue into a step-by-step implementation plan saved next to the issue notes.'
---

Create an implementation plan for GitHub issue #${input:issue:Issue number}. Read the issue with the GitHub MCP server and follow `.github/copilot-instructions.md`.

The plan must have these sections:

1. **Summary**: one paragraph on what changes and why.
2. **Files to touch**: every file to create or edit, with its project (`Animal.API`, `Animal.Domain`, `Animal.Data`, `tests/...`) and the reason.
3. **Order of changes**: numbered steps from the Domain layer outward (Domain, then Data, then API, then tests, then docs). Each step must be small enough to review on its own.
4. **Verification**: for each step, the command or test that proves it works (`dotnet build`, `dotnet test`, a request in `Animal.API.http`).
5. **Risks and open questions**: anything that could break existing behaviour or that needs a human decision.

Keep layer boundaries: no business logic in controllers, no entities returned from endpoints, `Animal.Domain` references nothing else.

Do not write code yet. Save the plan as `docs/milestone-2/implementation-plan.md` and stop so it can be reviewed.
