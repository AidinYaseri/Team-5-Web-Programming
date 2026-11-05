# Chloe work log — R-PIV

This log records the local work for WI-4, WI-11, and WI-10. No GitHub issue was created from this workspace.

## Research

- Read the WI-4, WI-10, and WI-11 descriptions and acceptance criteria in `docs/milestone-3/issues.md`.
- Confirmed the existing frontend is a .NET 10 Blazor Server project and that the API already uses typed HTTP access for pets.
- Reviewed `frontend-dev.agent.md`: it allows Razor/Blazor, requires HTTP-only API calls, and calls for validation, loading, and readable error states.
- Reviewed `testing.agent.md`: it requires deterministic xUnit and WebApplicationFactory tests and points to `.github/skills/testing/SKILL.md`, which is not present in this local checkout.
- Confirmed the API did not yet have Notifications or Appointments controllers, and that authentication provider settings and local-user claim mapping remain external dependencies.

## Plan

1. Keep the existing Blazor Server stack and add server-side OIDC authorization-code sign-in with PKCE and a token-forwarding HTTP handler.
2. Implement Notifications first, with current-user filtering, unread filtering, idempotent read tracking, and no public create endpoint.
3. Implement Appointments with future-date and double-booking conflicts, owner-only access, and notification creation on booking and cancellation.
4. Add README/diagram updates and Chloe's reflection sections; leave Darcy's persistence notes as a review gate.
5. Add domain and API integration tests, then build the solution. Do not create issues or contact the remote from this local workflow.

## Implement

- Added the Blazor OIDC configuration, PKCE, sign-in/out routes, token retrieval, and typed clients for Notifications and Appointments.
- Added API contracts, user-scoped controllers/services, EF repositories, scope policies, and development CORS configuration.
- Added booking and notification domain rules, plus xUnit unit and integration coverage.
- Updated the README, Mermaid architecture diagrams, reflection draft, and this log.

## Validate

- `dotnet build src/Animal.Client/Animal.Client.csproj --no-restore` succeeded with zero warnings and errors.
- `dotnet build src/Animal.API/Animal.API.csproj --no-restore -p:MSBuildEnableWorkloadResolver=false -m:1` succeeded with zero warnings and errors.
- Both test projects compiled with zero warnings and errors; tests have been authored but not run.
- A solution-wide build could not run because this installed SDK is missing its workload resolver locator. Individual project builds succeeded with the resolver disabled.
- Real OIDC sign-in still needs an identity provider configured. Aiden's identity-provider settings must supply the issuer and audience. The API maps the validated token's `sub` to `User.ExternalId`, then uses an internal local-user claim for owner-scoped endpoints.
- The updated Mermaid sources were not rendered to PNG because no Mermaid CLI is installed in this environment.
- The milestone page linked in the assignment could not be retrieved in this environment; the generic-controller lab page was reviewed as reference material.
