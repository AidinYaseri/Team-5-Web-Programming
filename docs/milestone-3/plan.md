# Milestone 3 Plan: Layered Service Development with AI Agents

Team 5 · Animal Adoption API

This plan has three parts. It lists what the repo is missing for Milestone 3, breaks that work into work items with 3 to 6 tasks each, and gives a suggested order. Owners are only a **proposal** until the team agrees. Each work item becomes one GitHub issue on the team's GitHub Project board, and each task becomes a sub-issue or a checklist item in it.

## 0. Decisions to make first

These block parts of the plan, so settle them before Phase 1.

| # | Decision | Default in this plan | Blocks |
|---|---|---|---|
| D1 | Frontend stack: React or ASP.NET Core (Blazor/Razor) | React + TypeScript (Vite) in `src/Animal.Client` | WI-4, all slice client tasks |
| D2 | Identity provider: Entra ID, Auth0, Keycloak or Duende (check Lecture 09) | Whatever Lecture 09 uses. Tests use locally signed test tokens either way | WI-3 |
| D3 | "Two independently runnable services per team member": one API host or several | Ask the teacher. Until then, one `Animal.API` host with six vertical slices, two per member | Solution layout |
| D4 | Where work items and PRs live | **Decided: GitHub** (Issues + Projects + PRs). The spec says to submit an Azure DevOps PR link on Léa, so confirm with the teacher that a GitHub PR link is accepted | WI-1, WI-12 |
| D5 | Database for dev: SQL Server LocalDB or SQLite | SQL Server LocalDB in dev, EF InMemory/SQLite in tests | WI-2 |

## 1. Gap list

What exists today compared to what the milestone asks for.

| Spec section | What we have | What's missing |
|---|---|---|
| Branch & PR | Work on GitHub, current branch `Copilot_instructions` | `feature/milestone-3-layered-service-development` off `main` and a milestone-level GitHub PR into `main` |
| 1. Domain layer | `Models/` (6 entities, anemic, public setters) and `Services/` (6 app-service interfaces, all sync) | `Entities/`, `Enums/` (e.g. species, application status), `Exceptions/`, and `Services/` with *pure* business rules. The current service interfaces are application services, so they move out of Domain |
| 1. Data layer | `InMemoryPetService` and `PetSeedData`. No EF Core at all, even though the README and CI mention it | `Context/AnimalDbContext.cs`, `Configurations/` (Fluent API), `Repositories/`, `Migrations/`, seed data through EF |
| 1. API layer | `PetsController` with one GET endpoint, `PetResponse`, `PetSearchQuery` | `Services/` for application services, controllers and DTOs for the other five areas, full CRUD for Pets, proper status codes (201/204/404/409) |
| 1. Client layer | Nothing | `src/Animal.Client` with screens for each slice, an API client, and sign-in |
| 1. Leftovers | Empty `Animal.API/`, `Animal.Data/` and `Animal.Domain/` folders at the repo root (only `bin/obj`) | Delete them |
| 2. Authentication | `app.UseAuthorization()` with no authentication at all | JWT bearer setup with authority and audience in config, `UseAuthentication()` before `UseAuthorization()`, and auth-code + PKCE sign-in in the client |
| 2. Authorization | Every endpoint is anonymous | Named policies per scope (`pets.read`, `pets.write`, …), and each endpoint marked as protected or `[AllowAnonymous]` on purpose |
| 2. Verification | Endpoint tests with no auth | Tests for 401 with no token, rejected bad/expired/wrong-issuer/wrong-audience tokens, 403 for a missing scope, 200 for the right scope, and public endpoints still open |
| 3–4. Work items | Milestone 2 used GitHub issue #7 | A board with work items, tasks, acceptance criteria and one owner each, plus commits and PRs that reference work item IDs |
| 5. Agents & skills | `.github/copilot-instructions.md` and 3 prompt files | `.github/agents/*.agent.md` (domain, data, api, frontend, testing, code-review) and `.github/skills/*/SKILL.md` (at least testing) |
| 6–7. R-PIV & gates | Milestone 2 R-PIV log for one issue | An R-PIV log and a human gate checklist in every PR |
| Tests | `Animal.API.Tests` and `Animal.Data.Tests` | Domain unit tests, and integration tests on EF + auth. Optionally rename to `Animal.UnitTests` / `Animal.IntegrationTests` to match the spec |
| CI | GitHub Actions on `main`, with a migration check that skips when there's no EF | Keep it. PRs into `main` (including the milestone PR) already trigger it |
| Deliverables | — | A 2–3 page architecture & reflection doc under `docs/` and the auth documentation |

## 2. Work items

Each slice work item follows the spec's 6-task hierarchy: Domain, Data, Contracts + App Service, Controller, Client, Tests. The scopes named below are a proposal, so rename them to match the identity provider.

### Shared foundation (do these first, in order)

#### WI-1: Set up the Milestone 3 branch, board and solution cleanup
- **Owner (proposed):** Aiden
- **Story:** As a team, we want a clean starting point so parallel work doesn't collide.
- **Tasks**
  1. Create `feature/milestone-3-layered-service-development` from `main` and push it with `-u`.
  2. Create a GitHub issue for each of WI-1 to WI-12 (tasks as sub-issues or checklists, one assignee each) and add them to a GitHub Project board.
  3. Delete the empty root `Animal.API/`, `Animal.Data/` and `Animal.Domain/` folders.
  4. Restructure Domain into `Entities/`, `Enums/`, `Exceptions/` and `Services/`, and move the service interfaces to `Animal.API/Services/` (make them async while they move).
  5. Write the **API contract sheet** (`docs/milestone-3/api-contracts.md`): routes, DTOs, status codes and the scope for every endpoint.
- **Acceptance criteria:** `dotnet build` and `dotnet test` pass with zero warnings, Domain has no package references, and the contract sheet is merged before any slice starts.

#### WI-2: Add the EF Core persistence foundation
- **Owner (proposed):** Darcy
- **Story:** As a developer, I want a real database so data survives restarts.
- **Tasks**
  1. Add EF Core packages to `Animal.Data` only, and create `Context/AnimalDbContext.cs` with a `DbSet` for all six entities.
  2. Add `Configurations/` with one `IEntityTypeConfiguration<T>` per entity (keys, required fields, lengths, relationships from the database diagram).
  3. Add a generic repository (based on Lab 08) with `AsNoTracking()` reads, plus a unit-of-work `SaveChangesAsync`.
  4. Create the `InitialCreate` migration and move `PetSeedData` into `HasData`.
  5. Register the DbContext and repositories in `Program.cs`, with the connection string in `appsettings.Development.json` (LocalDB only, no real credentials), and remove `InMemoryPetService`.
- **Acceptance criteria:** `dotnet ef database update` creates the schema with seed data, `GET /api/pets` returns the seeded pets from the DB, and the CI migration step runs instead of skipping.

#### WI-3: Secure the API with OAuth2/OIDC bearer tokens
- **Owner (proposed):** Aiden
- **Story:** As a shelter owner, I want only authorized users to change data.
- **Tasks**
  1. Register the API and the client app with the identity provider (D2), and define the scopes (`pets.read`, `pets.write`, `shelters.write`, `adoptions.read`, `adoptions.write`, …).
  2. Add `AddAuthentication().AddJwtBearer()` with authority and audience read from config (validate signature, issuer, audience and lifetime), and put `UseAuthentication()` before `UseAuthorization()`.
  3. Add named policies (one per scope) and an `ICurrentUser` service that reads the user id from token claims only.
  4. Add a test token helper in the integration test project (a local signing key and a `WebApplicationFactory` override) that can mint good and bad tokens.
  5. Write the five verification tests (401 with no token, bad/expired/wrong issuer/wrong audience, 403 for the wrong scope, 200 for the right scope, public endpoint open).
  6. Write `docs/milestone-3/auth.md`: flow diagram, roles (client, resource owner, authorization server, resource server), validation settings without secrets, and an endpoint → policy table.
- **Acceptance criteria:** all auth tests pass, no secrets are in git (user-secrets or env vars only), and every endpoint is either policy-protected or explicitly `[AllowAnonymous]`.

#### WI-4: Scaffold the frontend client with sign-in
- **Owner (proposed):** Chloe
- **Story:** As an adopter, I want a web page to browse pets and sign in.
- **Tasks**
  1. Create `src/Animal.Client` (React + Vite + TypeScript, or the D1 choice) and add it to the solution or README run steps.
  2. Add the auth-code + PKCE sign-in with the provider's SPA library (no client secret in the browser).
  3. Add a typed API client (`fetch`/`axios`) that attaches the access token and handles 401/403/400/500 errors in a user-friendly way.
  4. Add routing, a layout and a navbar with sign-in/out, plus CORS for the client origin in the API.
  5. Build the public "Browse pets" page against `GET /api/pets` as the first end-to-end check.
- **Acceptance criteria:** the client runs locally, signs in, calls a protected endpoint successfully, and never references `Animal.Data` or the database.

#### WI-5: Create repository agents and skills
- **Owner (proposed):** split, with each person writing the agent for the layer they lead
- **Story:** As developers, we want layer-specific Copilot agents so the AI keeps to our architecture.
- **Tasks**
  1. `.github/agents/domain-dev.agent.md` and `data-persistence.agent.md` (Darcy).
  2. `.github/agents/api-service.agent.md` and `code-review.agent.md` (read-only: `read`, `search`) (Aiden).
  3. `.github/agents/frontend-dev.agent.md` and `testing.agent.md` (Chloe).
  4. `.github/skills/testing/SKILL.md` (xUnit, `WebApplicationFactory`, test token helper, naming), plus optionally `ef-core/SKILL.md` and `auth/SKILL.md`.
  5. Update `copilot-instructions.md` and the prompt files so they point to the agents and the new folder layout.
- **Acceptance criteria:** each agent has frontmatter (description, name, tools) and folder constraints, and each one was tried once on a real task, with the result noted in the R-PIV log.

### Vertical slices (run in parallel after the foundation)

Every slice uses the same 6 tasks, so only the details differ:

1. **Domain:** entity invariants, enums and exceptions, plus pure rules in `Domain/Services`.
2. **Data:** repository methods and configuration tweaks (add a migration if the schema changes).
3. **Contracts + App Service:** request/response DTOs in `Api/Contracts` and the app service in `Api/Services` (repo → domain rule → DTO → save).
4. **Controller:** thin endpoints with status codes, `[Authorize(Policy=…)]` and OpenAPI attributes.
5. **Client:** screens and forms with validation and error messages.
6. **Tests:** domain unit tests plus integration tests for the endpoints, including auth.

| WI | Slice | Owner (proposed) | Key business rules (Domain) | Scopes | Client screens | Depends on |
|---|---|---|---|---|---|---|
| WI-6 | **Manage pets** (finish full CRUD) | Darcy | Age between 0 and 100, required name and species, a pet that isn't available can't be adopted | `pets.read` (GET list public), `pets.write` | Browse/search, pet details, add/edit pet | WI-1–4 |
| WI-7 | **Register shelters** | Darcy | Required name and address, capacity ≥ current pet count, can't delete a shelter that still has pets | `shelters.read`, `shelters.write` | Shelter list, shelter details with its pets, add/edit | WI-1–4 |
| WI-8 | **Manage user profiles** | Aiden | Unique email, user id taken from the token's `sub` claim, users only edit their own profile | `users.read`, `users.write` | My profile, edit profile | WI-1–4 |
| WI-9 | **Submit and review adoption applications** | Aiden | Only available pets, one open application per user per pet, approving marks the pet as unavailable and rejects other open applications | `adoptions.read`, `adoptions.write`, `adoptions.review` | Apply form, my applications, review queue (approve/reject) | WI-6, WI-8, WI-11 (notification call) |
| WI-10 | **Book shelter appointments** | Chloe | Must be in the future, no double-booking of a time slot, only the owner or the shelter can cancel | `appointments.read`, `appointments.write` | Book visit, my appointments, cancel | WI-6, WI-7 |
| WI-11 | **Send and read notifications** | Chloe | Created by adoption/appointment events, users only read their own, mark-as-read can be repeated safely | `notifications.read` | Notification bell/list, mark as read | WI-1–4 (build early, since WI-9 and WI-10 call it) |

**Acceptance criteria for every slice:**
- All endpoints from the contract sheet work and return DTOs, never entities.
- Every business rule above has a domain unit test, including at least one edge case.
- Integration tests cover the happy path, a validation failure (400), not found (404), 401 and 403.
- The client screens work end-to-end against the running API and show readable errors.
- The PR includes the R-PIV log (agent plan + human corrections) and a completed human gate checklist.

### Wrap-up

#### WI-12: Write the milestone documentation and open the milestone PR
- **Owner (proposed):** everyone, with one person collecting
- **Tasks**
  1. Update the README (structure, run steps for API + client, auth setup) and the diagrams (add the Client layer and the auth flow).
  2. Write `docs/milestone-3/architecture-reflection.md` (2–3 pages): the layering and frontend choice, how work was split, where agents helped or needed fixing, and what the human gates caught.
  3. Collect the board export/screenshots, the PR links and the test results into `docs/milestone-3/`.
  4. Run the code-review agent on the full milestone diff and resolve critical findings.
  5. Open the GitHub PR from the milestone branch to `main` with the work items, milestone number/title, change summary, validation results and human sign-off. Submit the link on Léa and **don't delete the branch**.
- **Acceptance criteria:** the PR is merged with at least one peer approval, and every item in the human gate checklist is checked.

## 3. Suggested order

```
Phase 0  Decisions D1–D5 (team meeting, ask teacher about D3)
   │
Phase 1  WI-1  branch, board, cleanup, Domain restructure, API contract sheet
   │
   ├─ Phase 2 (parallel)
   │    WI-2  EF Core foundation        (Darcy)
   │    WI-3  Auth foundation           (Aiden)
   │    WI-4  Client scaffold + sign-in (Chloe)
   │    WI-5  Agents & skills           (everyone, write your layer's agent)
   │
   ├─ Phase 3 (parallel, two slices each, using the agents + R-PIV)
   │    Darcy:  WI-6 Pets        → WI-7 Shelters
   │    Chloe:  WI-11 Notifications → WI-10 Appointments
   │    Aiden:  WI-8 Users       → WI-9 Adoptions (last, needs Pets/Users/Notifications)
   │
Phase 4  WI-12 docs, code-review agent pass, milestone PR on GitHub, submit on Léa
```

Why this order:
- **The contract sheet comes first**, so backend and frontend can work at the same time without waiting on each other (spec section 4).
- **The foundation goes before the slices.** EF Core, auth and the client shell touch `Program.cs` and shared files, so doing them once avoids merge conflicts in six slices.
- **Agents come before slice work.** Phase 3 has to show the R-PIV flow with agents, so they need to exist by then.
- **Notifications are early and Adoptions are last.** Adoptions depend on Pets, Users and Notifications, so building them last means the services they call already exist.

## 4. Per-PR routine (every slice)

1. **Research:** give the layer agent the work item ID, acceptance criteria and relevant files, and ask it to restate the constraints.
2. **Plan:** ask for a file-by-file plan, Domain → Data → Api → Client. **Gate 1:** a human checks the layering and acceptance criteria.
3. **Implement:** call the agents one layer at a time, with small commits that each reference the work item (e.g. `#23: add Shelter capacity rule`).
4. **Validate:** use the testing agent, then `dotnet build` (0 warnings) and `dotnet test`. **Gate 2:** read the diff line by line, run the app (remember the Milestone 2 DI bug that the tests missed), and check that no secrets are staged.
5. **Gate 3:** run the code-review agent on the PR, then get a peer review and fill in the human gate checklist in the PR description before merging.
