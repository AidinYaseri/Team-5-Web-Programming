# Milestone 3 architecture reflection

## Frontend — Chloe

We kept the client in the stack already present in the repository: ASP.NET Core Blazor Server on .NET 10. It lets the team build the UI and typed HTTP clients in C#, while keeping the client as a separate project with no references to `Animal.Domain` or `Animal.Data`. The client reaches the API over HTTP and owns copies of the public request and response contracts.

Sign-in is designed around the OIDC authorization-code flow with PKCE. The OIDC exchange runs on the server; a client secret, if the chosen provider requires one, belongs in server-side user-secrets or environment configuration. The API access token is forwarded by a shared HTTP handler. This choice fits the existing .NET solution and avoids adding a second frontend build toolchain. The trade-off is that UI interactions depend on the server-side Blazor circuit and the client needs an identity provider configured before sign-in can work end to end.

The frontend development agent supports the existing Razor/Blazor stack and requires HTTP-only API access, validation, loading and error states. The testing agent confines its changes to `tests/` and calls out deterministic test data. Those constraints shaped the implementation and test additions. The API resolves the validated token's `sub` to `User.ExternalId` and adds an internal local-user claim; real protected flows still need the identity-provider issuer, audience, and client settings configured.

## Persistence — Chloe draft, pending Darcy's notes

The current design keeps EF Core in `Animal.Data`. `AnimalDbContext` owns the six entity sets and implements the unit-of-work save boundary; one configuration class per entity keeps mapping out of the domain model. Repositories encapsulate database queries, and read queries use `AsNoTracking()` where entities are returned as read-only results. The client only sees HTTP DTOs and has no database dependency.

The Notifications and Appointments slices follow that boundary: repository queries scope reads and updates to the current local user ID, and the application services save the related appointment and notification together through the shared scoped context. SQLite in-memory integration tests exercise the API registrations without requiring a local SQL Server. This section is a draft from the repository state; Darcy's persistence notes should be used to confirm the migration, seed-data, and transaction details before submission.
