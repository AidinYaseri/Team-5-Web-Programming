# Copilot Instructions: Animal Adoption API

## 1. Overview

- **Purpose:** REST API for managing shelters, pets, users, adoption applications, appointments and notifications. Team 5 course project (Web Programming III, John Abbott College).
- **Language / framework:** C# 14 on .NET 10 (`net10.0`), ASP.NET Core Web API with controllers, EF Core, `Nullable` and `ImplicitUsings` enabled.
- **Database:** SQL Server via EF Core (code-first migrations).
- **API docs:** built-in OpenAPI (`AddOpenApi`/`MapOpenApi`), served at `/openapi/v1.json` in Development only.
- **Target runtime:** cross-platform .NET 10 (developed on Windows/Visual Studio, CI on Ubuntu).
- **Status:** early stage. Domain models and service interfaces exist. Controllers, DTOs, repositories, DbContext, migrations and service implementations are still to be written, so follow the architecture below when adding them.

## 2. Project layout

```
.github/workflows/Ci.yml        CI: restore, build, test, EF migration script check
docs/diagrams/                  .mmd sources + .png exports (service, class, database diagrams)
src/Animal.API/                 Web layer: Controllers/, Contracts/ (DTOs), Program.cs, appsettings*.json, launchSettings.json, Animal.API.http
src/Animal.Domain/              Models/ (entities) and Services/ (I*Service interfaces); no dependency on other projects
src/Animal.Data/                EF Core: Context/, Configurations/, Migrations/, SeedData/, Repositories/
tests/                          xUnit test projects (Animal.Data.Tests, Animal.API.Tests)
.github/prompts/                Reusable Copilot prompt files (prime an issue, write a plan, review a PR)
.vscode/mcp.json                GitHub MCP server config for Copilot Agent mode
Team-5-Web-Programming.slnx     Solution file
README.md                       Architecture, diagrams, getting started, team conventions
```

Project references (keep them one-way): `Animal.API → Animal.Domain, Animal.Data` and `Animal.Data → Animal.Domain`. `Animal.Domain` must never reference `API` or `Data`.

Key config: `src/Animal.API/appsettings.json` and `appsettings.Development.json`. The dev URLs are `http://localhost:5015` and `https://localhost:7298`.

## 3. Development rules

**Architecture**
- Layering: Controller → service interface (Domain) → repository/EF Core (Data) → SQL Server.
- Controllers stay thin: bind and validate input, call a service, map the result to an HTTP response. No business logic and no `DbContext` in controllers.
- Service interfaces live in `Animal.Domain/Services`. Implementations go in `Animal.Data` (or a dedicated project if one is added) and are registered in `Program.cs` through DI.
- Entities live in `Animal.Domain/Models`. Don't return entities directly from endpoints. Use request/response DTOs in `Animal.API/Contracts`.
- EF mapping goes in `IEntityTypeConfiguration<T>` classes in `Animal.Data/Configurations`, not in data annotations or a giant `OnModelCreating`.
- Cross-service behaviour documented in the README: `IAdoptionService` uses `IPetService` and `INotificationService`. `IAppointmentService` uses `INotificationService`.
- Keep `docs/diagrams/*.mmd`, the README diagram blocks and the PNGs in sync when services, models or relationships change.

**Naming and style**
- Namespaces match folders (e.g. `Animal.Domain.Models`). One public type per file, and the file name matches the type.
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
dotnet ef migrations add <Name> --project src/Animal.Data --startup-project src/Animal.API
dotnet ef database update --project src/Animal.Data --startup-project src/Animal.API
# CI check: the migration script must generate cleanly
dotnet ef migrations script --idempotent --configuration Release --project src/Animal.Data/Animal.Data.csproj --startup-project src/Animal.API/Animal.API.csproj
```

- Tests live in `tests/<Project>.Tests/` (xUnit) and are listed in the `.slnx`. Service logic gets unit tests in `Animal.Data.Tests`. Endpoints get integration tests in `Animal.API.Tests` using `WebApplicationFactory<Program>`, with `ConfigureTestServices` to swap in known fake data. Keep at least one test that uses the real `Program.cs` registrations so DI mistakes are caught.
- New services and controller logic must ship with tests.
- The CI workflow (`.github/workflows/Ci.yml`) runs on pushes and PRs to `main` and must pass. The .NET version in CI must match `<TargetFramework>` in the `.csproj` files.
- Use `src/Animal.API/Animal.API.http` for manual endpoint checks.

## 5. Security

- **Secrets:** never commit connection strings with credentials, passwords, API keys or tokens. Use `dotnet user-secrets` locally and environment variables or a secret store in deployment. `appsettings*.json` may only hold non-secret defaults.
- **Privacy:** `User` records are personal data (name, contact details, etc.). Never log PII or full request bodies. Don't expose other users' data or internal IDs or fields beyond what a DTO needs. Seed and test data must be fictional.
- **Authentication and authorization:** not implemented yet. When adding them, use ASP.NET Core authentication (JWT bearer or Identity). Hash passwords with the framework hasher and never store them in plain text. `UseAuthorization()` is already in the pipeline, so protect endpoints with `[Authorize]` and deny by default.
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
8. Work is on a branch off `main` and goes through a pull request. CI is green and at least one teammate has reviewed it before merging.
9. The diff contains only the intended changes (no `bin/`, `obj/`, `.vs/` or `*.user` files).

## 7. AI workflow (GitHub Copilot + MCP)

- Copilot Agent mode connects to GitHub through the MCP server in `.vscode/mcp.json`, so it can read, search and create issues and PRs in this repo.
- Every change starts from a GitHub issue. Search existing issues and code first so the new issue doesn't duplicate or conflict with other work.
- Follow Research, Plan, Implement, Validate. Use `.github/prompts/prime-issue.prompt.md` to research and restate the issue, then `.github/prompts/create-plan.prompt.md` in a fresh session to write the plan. Implement one plan step at a time and check in after each one. Don't accept one big unreviewed change.
- Before opening a PR, run the build and tests from section 4. After opening it, run `.github/prompts/review-pr.prompt.md` and record each finding as fixed or deferred in the PR.
- Milestone notes (issue, plan, test evidence, review record, reflection) go in `docs/milestone-2/`.
