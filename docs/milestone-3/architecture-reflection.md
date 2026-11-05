# Milestone 3 architecture reflection

## Frontend — Chloe

We chose ASP.NET Core Blazor Server on .NET 10, matching the .NET stack already used by the API. Keeping `Animal.Client` as a separate project let us build the UI and typed HTTP clients in C# without referencing `Animal.Domain` or `Animal.Data`. The client owns copies of the public request and response contracts and reaches backend data only through the REST API.

The client includes pet browsing, filtering, details and editing, visit booking, an appointments list, and notifications. Typed API clients centralize serialization and status handling. The pages distinguish loading, empty, API-error, and unreachable-API states so a failed request can be explained without exposing an unhandled exception as the page's only feedback.

Sign-in uses the OIDC authorization-code flow with PKCE. The code exchange and any client secret stay server-side; a shared HTTP handler attaches the access token to API requests. The API resolves the validated token's `sub` through the unique `User.ExternalId` value and supplies an internal local-user claim to owner-scoped endpoints. This avoids depending on a provider sending the application's numeric database ID.

The main trade-off is the server-side Blazor circuit: interactive pages depend on the client process remaining available, and every API-backed screen depends on the API being started as well. The README documents both terminal and Visual Studio startup. End-to-end sign-in still needs the team's identity-provider Authority, Audience, ClientId, and scopes configured; the committed defaults intentionally contain no provider-specific credentials. The frontend and testing agents reinforced the existing Blazor choice, HTTP-only boundary, readable error states, and deterministic API tests.

## Persistence — Chloe draft, pending Darcy's notes

The current design keeps EF Core in `Animal.Data`. `AnimalDbContext` owns the six entity sets and implements the unit-of-work save boundary; one configuration class per entity keeps mapping out of the domain model. Repositories encapsulate database queries, and read queries use `AsNoTracking()` where entities are returned as read-only results. The client only sees HTTP DTOs and has no database dependency.

The Notifications and Appointments slices follow that boundary: repository queries scope reads and updates to the current local user ID, and the application services save the related appointment and notification together through the shared scoped context. SQLite in-memory integration tests exercise the API registrations without requiring a local SQL Server. This section is a draft from the repository state; Darcy's persistence notes should be used to confirm the migration, seed-data, and transaction details before submission.
