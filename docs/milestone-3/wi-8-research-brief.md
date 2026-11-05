# WI-8 (#19) Users: research brief for the Copilot agents

Use this to prime Copilot in the **Research** step of R-PIV. Copilot writes the plan; you check it with the Gate 1 checklist at the end.

Prepared with Claude Code as research input. Say so in the PR and in the reflection, and keep Copilot's own plan as the plan of record.

---

## 1. Goal and acceptance criteria (from #19)

**User story:** As an adopter, I want to manage my own profile so that shelters can contact me about my applications.

- [ ] `User` has a unique `ExternalId` holding the token's `sub` (added with a migration)
- [ ] `GET` and `PUT /api/users/me` work. The profile is created on the first `PUT`, and `GET` returns 404 until then
- [ ] The user is always found from the token, never from a body or route id
- [ ] Email must be unique (409), and there's no endpoint that lists other users
- [ ] Client: "My profile" view and edit form
- [ ] Tests cover your own profile, a duplicate email, and 401/403

Contract (`docs/milestone-3/api-contracts.md` → Users):

| Method | Route | Policy | Request | Success | Errors |
|---|---|---|---|---|---|
| GET | `/api/users/me` | `users.read` | — | 200 `UserResponse` | 401, 403, 404 (no profile yet) |
| PUT | `/api/users/me` | `users.write` | `UpdateUserRequest` | 204 | 400, 401, 403, 409 (email taken) |

`UserResponse` = `{ id, name, email, phoneNumber }`. `UpdateUserRequest` = `name` (required, ≤ 100), `email` (required, `[EmailAddress]`, ≤ 254), `phoneNumber` (required, `[Phone]`, ≤ 20).

## 2. Current code (branch `feature/milestone-3-layered-service-development`)

- `src/Animal.Domain/Entities/User.cs`: `Id`, `Name`, `Email`, `PhoneNumber`. No `ExternalId`.
- `src/Animal.Data/Configurations/UserConfiguration.cs`: lengths 100 / 254 / 20, unique index on `Email`. Table already exists in `InitialCreate`; no users are seeded.
- No `IUserRepository` yet. Follow `IPetRepository` / `PetRepository` and `IUnitOfWork` (saves happen in the application service, once).
- `src/Animal.API/Services/IUserService.cs`: a **placeholder** with sync methods returning entities, including `GetUsers()` and `DeleteUser()`. It doesn't match the contract and must be **replaced**, not implemented.
- `src/Animal.API/Services/ICurrentUser.cs` / `HttpCurrentUser.cs` (from WI-3): `Subject` returns the `sub` claim. Use it; don't read claims anywhere else.
- `src/Animal.API/Authorization/AuthorizationPolicies.cs`: `UsersRead` and `UsersWrite` already exist and are registered.
- `src/Animal.API/ErrorHandling/DomainExceptionHandler.cs`: `EntityNotFoundException` → 404, `DomainValidationException` → 400, `InvalidStatusTransitionException` → 409, `DbUpdateException` → 409.
- `PetService`, `PetRules`, `PetsController`, `PetServiceTests` and `PetsEndpointTests` are the pattern to copy for each layer.
- `tests/Animal.IntegrationTests/Api/AnimalApiFactory.cs` already has `CreateToken(subject:, scopes:, ...)`. `CreateAuthenticatedClient()` only gives `pets.write` with a fixed subject.
- `src/Animal.Client`: no sign-in yet (that's #15, Chloe). `PetsApiClient` is the pattern for an API client class.

## 3. Constraints for the plan

- Order of work: Domain → Data → Api → Client → tests, one agent per layer: `domain-dev`, `data-persistence`, `api-service`, `frontend-dev`, `testing`. `code-review` on the PR.
- Domain stays dependency-free. Data references Domain only. The client talks to the API over HTTP only, with its own copy of the DTOs.
- The user is found **only** through `ICurrentUser.Subject` → `ExternalId`. No user id in a route, query or body. `ExternalId` is never returned in a response.
- No endpoint lists users, gets a user by id, or deletes a user. Remove those methods from `IUserService`.
- Reads use `AsNoTracking()` except where the service changes and saves the entity.
- Migration through `dotnet ef migrations add ...` only. Never edit `InitialCreate` or the snapshot by hand.
- Don't log names, emails or phone numbers (personal data).

## 4. Decisions to make in the plan (state the choice)

1. **"Email taken" → 409.** The contract says 409, but `DomainValidationException` gives 400. Options: (a) add a Domain exception such as `DuplicateValueException` and map it to 409 in `DomainExceptionHandler`, or (b) let the unique index fail and rely on `DbUpdateException` → 409. (a) gives a clear message and is testable without the database; (b) still matters as the safety net when two requests race. Recommended: (a), with (b) as backup.
2. **Email normalization.** Store emails trimmed and lowercased, and compare that way, so `Sam@Example.com` and `sam@example.com` are the same address. See pitfall 2.
3. **PUT creates or updates.** First `PUT` creates the row with `ExternalId = Subject`; later ones update it. The contract says 204 for both. Keep 204 so it matches the sheet; mention that 201 on create would be an alternative for the team to decide.

## 5. Pitfalls (check the plan handles each)

1. **Updating your own profile with the same email must not be "email taken".** The uniqueness check has to exclude the current user's own row (e.g. `EmailInUseAsync(email, exceptUserId)`). Test: PUT, then PUT again with the same email → 204.
2. **SQLite and SQL Server disagree on case.** SQL Server's default collation is case-insensitive, so its unique index treats `Sam@x.com` = `sam@x.com`. SQLite (used by the integration tests) is case-sensitive. Without normalization, the tests and production behave differently. Normalizing in the service (decision 2) fixes both.
3. **Users persist between tests.** `AnimalApiFactory.ResetPets` only resets pets. Each integration test class shares one database, so every test should use its own `sub` and email (e.g. with a `Guid`), or tests will see each other's users.
4. **The migration adds a required, unique column to an existing table.** It's fine in CI and for teammates with an empty `Users` table. Anyone with rows in their local `Users` table gets a unique-index error on `""`. Note it in the PR: they need to clear local users first or recreate the database.
5. **Least privilege works both ways.** `users.write` alone must not allow `GET /me` (403), and `users.read` alone must not allow `PUT /me` (403). Test both.
6. **Proving there's no list endpoint.** `GET /api/users` and `GET /api/users/1` should return 404. The `ControllerEndpoints_HaveExplicitAuthorizationMetadata` test from WI-3 will also check the new controller automatically.
7. **The client can't call `/me` until #15 adds sign-in.** The profile page can be built now against `UserResponse`/`UpdateUserRequest`, but it must handle 401 with a "sign in to see your profile" message instead of crashing. Agree with Chloe on how the token will be attached (her `HttpClient` setup) so the page doesn't need changing later.

## 6. Tests the plan must include

**Integration** (`tests/Animal.IntegrationTests/Api/UsersEndpointTests.cs`; add a factory helper that creates a client for a given `sub` and scopes):

| Case | Expected |
|---|---|
| `GET /me` before any `PUT` | 404 |
| `PUT /me` (new user), then `GET /me` | 204, then 200 with the same data |
| `PUT /me` again with changes | 204, and still exactly one row for that `sub` |
| `PUT /me` again with your own unchanged email | 204 (not 409) |
| Another user `PUT`s an email you already use (different case too) | 409 |
| Two users: each `GET /me` returns only their own profile | 200 / 200, different data |
| Invalid body (empty name, bad email) | 400 |
| No token on `GET /me` and `PUT /me` | 401 |
| `users.write` only → `GET /me`; `users.read` only → `PUT /me` | 403 / 403 |
| `GET /api/users` and `GET /api/users/1` | 404 |
| `ExternalId` doesn't appear in the `GET /me` JSON | — |

**Unit:** `UserRules` (required fields, lengths, email shape) in `Animal.UnitTests/Domain`; `UserService` with fake repositories in `Animal.UnitTests/Api` (create vs update, email normalization, own-email allowed, duplicate email → exception, saves once, nothing saved on failure); the client's profile API calls in `Animal.UnitTests/Client` (same style as `PetsApiClientTests`).

Every existing test must still pass, and the migration must pass `dotnet ef migrations has-pending-model-changes` (no pending changes).

---

## 7. Prompts for each R-PIV stage

**Research** (Agent mode, default agent; attach this file, `api-contracts.md`, `User.cs`, `UserConfiguration.cs`, `IUserService.cs`, `ICurrentUser.cs`, `PetService.cs`, `DomainExceptionHandler.cs`, `AnimalApiFactory.cs`):
> Read the attached files for issue #19. Summarize the architectural constraints, the files that will change per layer, the dependencies on other work items (especially #15), and anything in the brief that conflicts with the current code. Don't change any files.

**Plan:**
> Write a file-by-file implementation plan for #19, layer by layer: Domain, Data (including the migration command), Api, Client, tests. For each step say which agent does it (domain-dev, data-persistence, api-service, frontend-dev, testing) and how we'll know it worked. State your choice for the 3 decisions in section 4, and show how each pitfall in section 5 is handled. Cover every acceptance criterion and every row in section 6. Don't write code yet.

Review with the Gate 1 checklist, correct it in the chat, save the final version as `docs/milestone-3/wi-8-plan.md` with your Gate 1 review at the bottom.

**Implement:** one agent per layer, in order, one commit per step (`#19: ...`), `dotnet build` after each.

**Validate:**
> Run `dotnet build` and `dotnet test` for the whole solution and `dotnet ef migrations has-pending-model-changes --project src/Animal.Data --startup-project src/Animal.API`. Report warnings and failures. Don't change anything.

**Code review** (`code-review` agent):
> Review the diff for #19 against the Human Gate Checklist: layer boundaries, user identity from the token only, no list/get-by-id endpoint, least-privilege policies, email uniqueness and normalization, personal data not logged or over-exposed (`ExternalId`), migration safety, and test coverage. Group findings by severity.

---

## Gate 1 checklist (review Copilot's plan before any code)

- [ ] Work goes Domain → Data → Api → Client → tests, each with the right agent
- [ ] `ExternalId` (string, required, unique, length set) added through a new migration; `InitialCreate` untouched
- [ ] `IUserService` placeholder replaced: async, DTOs, only "get my profile" and "create or update my profile"
- [ ] User looked up only via `ICurrentUser.Subject`; no id in route/body/query; `ExternalId` not in any response
- [ ] `GET /me` → `users.read`, `PUT /me` → `users.write`, both via `AuthorizationPolicies` constants
- [ ] Choice made for "email taken" → 409 (decision 1), with the unique index as backup
- [ ] Emails normalized (trim + lowercase) before the check and before saving
- [ ] Uniqueness check excludes the current user's own row
- [ ] Integration tests use a unique `sub` and email per test
- [ ] Least-privilege tests in both directions (read-only can't PUT, write-only can't GET)
- [ ] Test proving `/api/users` and `/api/users/{id}` don't exist
- [ ] Client page handles 401 (no sign-in yet) and validation/409 errors; token wiring agreed with Chloe
- [ ] Migration checked with `has-pending-model-changes`; PR notes the local-database caveat
- [ ] No personal data logged

## Gate 2 checklist (after implementation)

- [ ] `dotnet build` 0 warnings, `dotnet test` all pass, no pending model changes
- [ ] Read the diff line by line, especially the repository's email check and the controller (no id parameters)
- [ ] Mutation check: (1) remove the "exclude own row" condition → the own-email test must fail; (2) remove email lowercasing → the different-case duplicate test must fail; (3) change `[Authorize(Policy = UsersRead)]` to `UsersWrite` on `GET /me` → a 403 test must fail. Revert each.
- [ ] Run the API locally, apply the migration (`dotnet ef database update ...`), and check the new `ExternalId` column exists
- [ ] Wait for a teammate's approval before merging
