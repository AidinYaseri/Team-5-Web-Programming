# Milestone 3 architecture reflection

## Frontend — Chloe

We chose ASP.NET Core Blazor Server on .NET 10, matching the .NET stack already used by the API. Keeping `Animal.Client` as a separate project let us build the UI and typed HTTP clients in C# without referencing `Animal.Domain` or `Animal.Data`. The client owns copies of the public request and response contracts and reaches backend data only through the REST API.

The client includes pet browsing, filtering, details and editing, visit booking, an appointments list, and notifications. Typed API clients centralize serialization and status handling. The pages distinguish loading, empty, API-error, and unreachable-API states so a failed request can be explained without exposing an unhandled exception as the page's only feedback.

Sign-in uses the OIDC authorization-code flow with PKCE. The code exchange and any client secret stay server-side; a shared HTTP handler attaches the access token to API requests. The API resolves the validated token's `sub` through the unique `User.ExternalId` value and supplies an internal local-user claim to owner-scoped endpoints. This avoids depending on a provider sending the application's numeric database ID.

The main trade-off is the server-side Blazor circuit: interactive pages depend on the client process remaining available, and every API-backed screen depends on the API being started as well. The README documents both terminal and Visual Studio startup. End-to-end sign-in still needs the team's identity-provider Authority, Audience, ClientId, and scopes configured; the committed defaults intentionally contain no provider-specific credentials. The frontend and testing agents reinforced the existing Blazor choice, HTTP-only boundary, readable error states, and deterministic API tests.

## Persistence — Chloe

EF Core is isolated in `Animal.Data`, and the domain project has no EF dependency. `AnimalDbContext` exposes sets for all six entities and applies the entity configurations from that assembly. `AddAnimalData` registers the context as scoped, maps `IUnitOfWork` to that same context, and registers the repositories. This keeps schema details and query code out of the API controllers and domain rules.

The configurations define keys, column lengths, indexes, and delete behavior. `PetConfiguration` and `ShelterConfiguration` seed five fictional pets and two fictional shelters through `HasData`; the initial migration creates the relational schema. The later `AddUserExternalId` migration adds a unique provider-subject column and backfills existing rows before making it required. That allows authentication to resolve an opaque OIDC `sub` to the local integer user key without putting provider-specific identity assumptions in the domain model.

Repositories keep owner filters in database queries. Read-only lists use `AsNoTracking()`, while rows that a service will update remain tracked. `IUnitOfWork` makes saving explicit: appointment creation or cancellation stages both the appointment change and its notification, then saves them with one `SaveChangesAsync` call. Marking a notification read is its own operation and saves that change directly. The client never sees EF entities or a database connection; it receives API DTOs over HTTP.

Integration tests use an open in-memory SQLite connection and create the schema from the same EF configurations and seed data, while development uses SQL Server LocalDB. This keeps integration tests isolated and repeatable, though SQLite cannot verify every SQL Server-specific migration or behavior. The persistence details here are based on the checked-in code. Darcy's separate handoff notes weren't present in the local folder, so compare this section with his notes before submission if they add design decisions or trade-offs not recorded in the implementation.
