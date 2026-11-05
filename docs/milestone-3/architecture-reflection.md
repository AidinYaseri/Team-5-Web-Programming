# Milestone 3 architecture reflection

## Layered backend architecture (Aiden)

Our service is one ASP.NET Core API split into the layers the milestone asks for. `Animal.Domain` has the entities, enums, exceptions and pure rules like `PetRules` and `AdoptionRules`, and it has no package or project references at all. `Animal.Data` has EF Core: the `DbContext`, one configuration class per entity, the migrations and the repositories. `Animal.API` has the controllers, the request and response DTOs and the application services, which call the rules, use the repositories and save once through `IUnitOfWork`. `Animal.Client` is a Blazor Server app that only talks to the API over HTTP with its own copies of the DTOs.

We didn't want the one-way dependency rule to live only in a document, so `LayerDependencyTests` reads the `.csproj` files and fails the build if a layer gets a reference it shouldn't have. Errors also go through one place. The services throw domain exceptions and `DomainExceptionHandler` turns them into ProblemDetails (404, 400 or 409), so the controllers stay thin.

The API has six vertical slices: Pets, Shelters, Users, Adoptions, Appointments and Notifications. We read "two services per team member" as two slices each, all in one API host. We still have to confirm that with the teacher.

## Frontend (Chloe)

We chose ASP.NET Core Blazor Server on .NET 10, matching the .NET stack already used by the API. Keeping `Animal.Client` as a separate project let us build the UI and typed HTTP clients in C# without referencing `Animal.Domain` or `Animal.Data`. The client owns copies of the public request and response contracts and reaches backend data only through the REST API.

The client includes pet browsing, filtering, details and editing, visit booking, an appointments list, and notifications. Typed API clients centralize serialization and status handling. The pages distinguish loading, empty, API-error, and unreachable-API states so a failed request can be explained without exposing an unhandled exception as the page's only feedback.

Sign-in uses the OIDC authorization-code flow with PKCE. The code exchange and any client secret stay server-side; a shared HTTP handler attaches the access token to API requests. The API resolves the validated token's `sub` through the unique `User.ExternalId` value and supplies an internal local-user claim to owner-scoped endpoints. This avoids depending on a provider sending the application's numeric database ID.

The main trade-off is the server-side Blazor circuit: interactive pages depend on the client process remaining available, and every API-backed screen depends on the API being started as well. The README documents both terminal and Visual Studio startup. End-to-end sign-in still needs the team's identity-provider Authority, Audience, ClientId, and scopes configured; the committed defaults intentionally contain no provider-specific credentials. The frontend and testing agents reinforced the existing Blazor choice, HTTP-only boundary, readable error states, and deterministic API tests.

## Persistence (Chloe)

EF Core is isolated in `Animal.Data`, and the domain project has no EF dependency. `AnimalDbContext` exposes sets for all six entities and applies the entity configurations from that assembly. `AddAnimalData` registers the context as scoped, maps `IUnitOfWork` to that same context, and registers the repositories. This keeps schema details and query code out of the API controllers and domain rules.

The configurations define keys, column lengths, indexes, and delete behavior. `PetConfiguration` and `ShelterConfiguration` seed five fictional pets and two fictional shelters through `HasData`; the initial migration creates the relational schema. The later `AddUserExternalId` migration (WI-8) adds a required, unique provider-subject column. It does not backfill, so a local database that already has user rows has to be cleared before `dotnet ef database update`. That allows authentication to resolve an opaque OIDC `sub` to the local integer user key without putting provider-specific identity assumptions in the domain model.

Repositories keep owner filters in database queries. Read-only lists use `AsNoTracking()`, while rows that a service will update remain tracked. `IUnitOfWork` makes saving explicit: appointment creation or cancellation stages both the appointment change and its notification, then saves them with one `SaveChangesAsync` call. Marking a notification read is its own operation and saves that change directly. The client never sees EF entities or a database connection; it receives API DTOs over HTTP.

Integration tests use an open in-memory SQLite connection and create the schema from the same EF configurations and seed data, while development uses SQL Server LocalDB. This keeps integration tests isolated and repeatable, though SQLite cannot verify every SQL Server-specific migration or behavior. The persistence details here are based on the checked-in code. Darcy's separate handoff notes weren't present in the local folder, so compare this section with his notes before submission if they add design decisions or trade-offs not recorded in the implementation. Darcy also added a design-time `AnimalDbContextFactory`, so `Animal.Data` is the startup project for `dotnet ef` commands, and CI now always checks that the migration script generates.

## How the work was divided (Aiden)

We split the work into twelve work items (GitHub issues #12 to #23). Each of us owned two backend slices plus their screens. Darcy had Pets and Shelters, Chloe had Appointments and Notifications, and I had Users and Adoptions. On top of that, Darcy did the EF Core foundation and the agents, Chloe did the client shell with sign-in and the README and diagrams, and I did the project setup, auth and the milestone PR. Before anyone started a slice, we wrote `api-contracts.md` with every endpoint, DTO, status code and scope, so the backend and the screens could be built at the same time.

That plan worked for the contracts, but not for the branches. I pushed the first foundation work (the layers, EF Core, the first Pets endpoints and a first client) straight to `main` before we followed the branch rule. Darcy and Chloe then started their branches from that old `main`, so neither of them had my auth (WI-3) or users (WI-8) work. We ended up with three different auth setups. Chloe's branch also merged a three-week-old copy of the milestone branch, because her local copy was out of date. Everything had to be brought back together in one integration PR (#28). The lesson for us is simple: agree on the base branch, and run `git fetch` before branching or merging.

## How the agents helped and where they needed correcting (Aiden)

We made six agents in `.github/agents/` (domain, data, API, frontend, testing and a read-only code reviewer) and a testing skill. Each agent is only allowed to edit its own layer. That made the plans better, because every step had to say which agent does it, and it stopped one agent from quietly changing three layers at once.

For WI-3 (auth) and WI-8 (users) I used Copilot Agent mode with the R-PIV flow. I prepared the research briefs with Claude Code, Copilot wrote the plans, and the agents did the code layer by layer. The agents were fast at the boring parts, like DTOs, repositories, registration and long lists of tests. But every plan needed fixing before any code was written. Copilot's WI-3 plan would have made the OpenAPI document return 401 and gave `auth.md` to an agent that can only edit `src/*.Api/`. It also invented a conflict with the brief that wasn't there. In WI-8, Copilot wrote its own "Gate 1 review", ticked every box and approved its own plan. That plan also put `UserService` in the Data layer, which would have broken the layering. The agents also kept using a different code style than the rest of the repo, even though `copilot-instructions.md` says to match it.

The code review agent was useful as a first pass. On WI-8 it found a migration risk, a missing 401 test and a confusing "User 0 was not found" message. It can only see the diff, though, so it missed a commit message without the issue number.

To be honest about it, not everything went through the repo agents. The first foundation work, the integration of all three branches (#28) and WI-9 (adoptions) were done directly with Claude Code instead of the Copilot agents. The PRs say so, and Claude is listed as co-author on the integration commits.

## How well the human gates worked (Aiden)

Gate 1 (plan review) and Gate 2 (code and test review) caught real problems. Gate 3 (peer review) was our weakest gate.

For Gate 2 we used mutation checks. We broke the code on purpose and made sure a test failed. On WI-3, removing the deny-by-default fallback policy failed no test at all. With the scope check broken, requests with no token could reach the write endpoints. We added tests and `RequireAuthenticatedUser()` to every policy before merging. On WI-8, three planted bugs (skipping the "own row" check on emails, removing the lowercasing, using the wrong policy) were all caught. On WI-9, four planted bugs were all caught, three of them by both a unit test and an integration test.

The tests from earlier gates also protected the integration. WI-3's "every endpoint has explicit auth" test caught that the Shelters GET endpoints would suddenly need sign-in. Reviewing the merged code found a custom auth handler that took the token text as the permission list. Anyone sending `Bearer pets.write` could write pets. It also found a duplicate migration that would have broken `database update`. New tests now prove that a token with a forged `local_user_id` claim can't read someone else's notifications.

Gate 3 is where we failed. PRs #25, #27 and #28 were merged within minutes, without a recorded approval from a teammate, even though our checklist asks for one. The code was tested, but nobody else signed off before the merge. The fix is to get those reviews now, after the fact, and to turn on branch protection with one required approval, so GitHub enforces the rule instead of relying on us to remember it.

