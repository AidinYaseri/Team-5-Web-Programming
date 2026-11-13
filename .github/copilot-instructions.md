# Copilot Instructions: Animal Adoption API

## 1. Overview

- **Purpose:** REST API for managing shelters, pets, users, adoption applications, appointments and notifications. Team 5 course project (Web Programming III, John Abbott College).
- **Language / framework:** C# 14 on .NET 10 (`net10.0`), ASP.NET Core Web API with controllers, EF Core, `Nullable` and `ImplicitUsings` enabled.
- **Database:** SQL Server via EF Core (code-first migrations).
- **API docs:** built-in OpenAPI (`AddOpenApi`/`MapOpenApi`), served at `/openapi/v1.json` in Development only.
- **Target runtime:** cross-platform .NET 10 (developed on Windows/Visual Studio, CI on Ubuntu).
- **Status:** Milestone 3. All six slices (Pets, Shelters, Users, Adoptions, Appointments, Notifications) work end to end (Domain, Data, Api, Blazor client) with JWT bearer auth. Follow the architecture below and the endpoint contracts in `docs/milestone-3/api-contracts.md`.

## 2. Project layout

```
.github/workflows/Ci.yml        CI: restore, build, test, EF migration script check
docs/diagrams/                  .mmd sources + .png exports (service, class, database diagrams)
src/Animal.API/                 Web layer: Controllers/, Contracts/ (DTOs), Services/ (application services), Authorization/, ErrorHandling/, Program.cs, appsettings*.json, Animal.API.http
src/Animal.Domain/              Entities/, Enums/, Exceptions/, Queries/, Services/ (pure business rules); no package or project references
src/Animal.Data/                EF Core: Context/ (incl. design-time factory), Configurations/, Migrations/, SeedData/, Repositories/
src/Animal.Client/              Blazor Server UI: OIDC sign-in, pages, typed HttpClient services; talks to the API over HTTP only
tests/                          Animal.UnitTests (domain, services, client, architecture) and Animal.IntegrationTests (API endpoints, repositories)
.github/agents/                 Layer-specific Copilot agents (see section 8)
.github/skills/                 Skills the agents follow (testing)
.github/prompts/                Reusable Copilot prompt files (prime an issue, write a plan, review a PR)
.vscode/mcp.json                GitHub MCP server config for Copilot Agent mode
Team-5-Web-Programming.slnx     Solution file (.sln has the same projects)
README.md                       Architecture, diagrams, getting started, team conventions
```

Project references (keep them one-way): `Animal.API → Animal.Domain, Animal.Data` and `Animal.Data → Animal.Domain`. `Animal.Domain` must never reference `API` or `Data`. `Animal.Client` has no project references at all. `tests/Animal.UnitTests/Architecture/LayerDependencyTests.cs` fails the build if these rules are broken.

Key config: `src/Animal.API/appsettings.json` and `appsettings.Development.json`. The dev URLs are `http://localhost:5015` and `https://localhost:7298`.

## 3. Development rules

**Architecture**
- Layering: Controller → application service (`Animal.API/Services`) → repository (`Animal.Data/Repositories`) → SQL Server. Application services call pure rules in `Animal.Domain/Services`, map entities to DTOs and save once through `IUnitOfWork`.
- Controllers stay thin: bind and validate input, call an application service, return the HTTP result. No business logic and no `DbContext` or repository in controllers.
- Application service interfaces and implementations live in `Animal.API/Services`. Repository interfaces and implementations live in `Animal.Data/Repositories`. Register services in `Program.cs` and repositories in `Animal.Data/DependencyInjection.cs`.
- Entities live in `Animal.Domain/Entities`. Don't return entities directly from endpoints. Use request/response DTOs in `Animal.API/Contracts`.
- Errors: throw the exceptions in `Animal.Domain/Exceptions`. `DomainExceptionHandler` turns them into ProblemDetails (not found → 404, broken rule → 400, invalid status / duplicate / conflict → 409), so controllers don't catch them.
- EF mapping goes in `IEntityTypeConfiguration<T>` classes in `Animal.Data/Configurations`, not in data annotations or a giant `OnModelCreating`.
- Cross-service behaviour documented in the README: `IAdoptionService` uses `IPetService` and `INotificationService`. `IAppointmentService` uses `INotificationService`.
- Keep `docs/diagrams/*.mmd`, the README diagram blocks and the PNGs in sync when services, models or relationships change.

**Naming and style**
- Namespaces match folders (e.g. `Animal.Domain.Entities`), with block-scoped `namespace X { ... }` like the existing code. One public type per file, and the file name matches the type.
- PascalCase for types, methods and properties. `_camelCase` for private fields. Interfaces start with `I`. Async methods end in `Async` and accept a `CancellationToken`.
- Controllers are plural and end in `Controller` (`PetsController`), with `[ApiController]` and `[Route("api/[controller]")]`.
- Respect nullable annotations. Don't suppress warnings with `!` unless you can justify it.
- Use `async`/`await` end to end for I/O. Never use `.Result` or `.Wait()`.
- Return proper status codes (`201` with `CreatedAtAction`, `204`, `400`/`ValidationProblem`, `404`, `409`). Use `ProblemDetails` for errors.
- Match the surrounding code's style, comment density and formatting. Don't reformat unrelated code.

**Data**
- Change the schema only through EF migrations. Never edit an applied migration or hand-edit the model snapshot.
- Seed data goes in `Animal.Data/SeedData` and must contain fake data only.

## 4. Build and tests

Run these from the repo root, in this order (this is the same order CI uses):

```bash
dotnet restore Team-5-Web-Programming.slnx
dotnet build Team-5-Web-Programming.slnx --no-restore --configuration Release
dotnet test Team-5-Web-Programming.slnx --no-build --configuration Release
```

Run the API locally:

```bash
dotnet run --project src/Animal.API
```

EF Core (install once with `dotnet tool install --global dotnet-ef`):

```bash
# Animal.Data has its own design-time factory (AnimalDbContextFactory), so it is also the startup project.
dotnet ef migrations add <Name> --project src/Animal.Data --startup-project src/Animal.Data
dotnet ef database update --project src/Animal.Data --startup-project src/Animal.Data
dotnet ef migrations has-pending-model-changes --project src/Animal.Data --startup-project src/Animal.Data
# CI check: the migration script must generate cleanly
dotnet ef migrations script --idempotent --configuration Release --project src/Animal.Data/Animal.Data.csproj --startup-project src/Animal.Data/Animal.Data.csproj
```

- Unit tests go in `tests/Animal.UnitTests` (domain rules, application services with fakes, client API classes, architecture rules). Integration tests go in `tests/Animal.IntegrationTests`: endpoints through `AnimalApiFactory` (real `Program.cs`, SQLite in memory) and repositories through `SqliteTestDatabase`. Follow `.github/skills/testing/SKILL.md`.
- Protected endpoints are tested with real signed tokens from `AnimalApiFactory.CreateToken(subject:, scopes:)` or `CreateAuthenticatedClient(...)`. Never add a fake authentication handler.
- New services and controller logic must ship with tests.
- The CI workflow (`.github/workflows/Ci.yml`) runs on pushes to `main` and on PRs into `main` or a milestone branch (`feature/milestone-3-layered-service-development`, `feature/milestone-4-security`), and must pass. The .NET version in CI must match `<TargetFramework>` in the `.csproj` files.
- Use `src/Animal.API/Animal.API.http` for manual endpoint checks.

## 5. Security

- **Secrets:** never commit connection strings with credentials, passwords, API keys or tokens. Use `dotnet user-secrets` locally and environment variables or a secret store in deployment. `appsettings*.json` may only hold non-secret defaults.
- **Privacy:** `User` records are personal data (name, contact details, etc.). Never log PII or full request bodies. Don't expose other users' data or internal IDs or fields beyond what a DTO needs. Seed and test data must be fictional.
- **Authentication and authorization:** the API is a JWT bearer resource server (see `docs/milestone-3/auth.md`). All policies are registered in `AuthorizationPolicies.Configure`, with a deny-by-default fallback policy. Every endpoint has `[Authorize(Policy = AuthorizationPolicies.X)]` or an explicit `[AllowAnonymous]`. The current user comes only from `ICurrentUser` (`Subject` from the token's `sub`, `UserId` from the local mapping); never take a user id from a body, header or route. Never hand-roll token validation.
- **Access rules:** users may only read and modify their own applications, appointments and notifications. Only shelter staff or admins may manage pets and shelters, and approve or reject adoption applications (`ApproveApplication`/`RejectApplication`).
- **Input and data access:** validate all DTOs (data annotations or FluentValidation). Use EF Core/LINQ or parameterized queries only, never string-concatenated SQL. Don't bind entities directly from requests (avoid over-posting).
- **Transport and config:** keep `UseHttpsRedirection()`. Restrict `AllowedHosts` and configure CORS explicitly for production. Expose OpenAPI and Swagger in Development only.
- **Dependencies:** add only well-maintained NuGet packages and keep versions current.

## 6. Change checklist

Before considering a change done:

1. `dotnet build` succeeds for the whole solution with no new warnings.
2. `dotnet test` passes, with tests added or updated for new or changed behaviour.
3. If models or configurations changed, there is a new EF migration, and `dotnet ef migrations script` runs cleanly.
4. Layer boundaries are respected (no `Domain → Data/API` references, no business logic in controllers, no entities in API responses).
5. No secrets, PII or real data in code, config, logs, seed data or tests.
6. Endpoints have validation, correct status codes and appropriate authorization.
7. Diagrams and README are updated if services, models or structure changed.
8. Work is on a branch off the current milestone branch (`feature/milestone-4-security`) and goes through a pull request into it. CI is green and at least one teammate has reviewed it before merging.
9. The diff contains only the intended changes (no `bin/`, `obj/`, `.vs/` or `*.user` files).

## 7. AI workflow (GitHub Copilot + MCP)

- Copilot Agent mode connects to GitHub through the MCP server in `.vscode/mcp.json`, so it can read, search and create issues and PRs in this repo.
- Every change starts from a GitHub issue. Search existing issues and code first so the new issue doesn't duplicate or conflict with other work.
- Follow Research, Plan, Implement, Validate. Use `.github/prompts/prime-issue.prompt.md` to research and restate the issue, then `.github/prompts/create-plan.prompt.md` in a fresh session to write the plan. Implement one plan step at a time and check in after each one. Don't accept one big unreviewed change.
- Before opening a PR, run the build and tests from section 4. After opening it, run `.github/prompts/review-pr.prompt.md` and record each finding as fixed or deferred in the PR.
- Milestone notes (research briefs, plans, gate reviews, auth, reflection) go in `docs/milestone-3/`.

## 8. WI-5 Agents and skills

- #file:'.github/agents/domain-dev.agent.md'
- #file:'.github/agents/data-persistence.agent.md'
- #file:'.github/agents/api-service.agent.md'
- #file:'.github/agents/frontend-dev.agent.md'
- #file:'.github/agents/testing.agent.md'
- #file:'.github/agents/code-review.agent.md'
- #file:'.github/skills/testing/SKILL.md'
