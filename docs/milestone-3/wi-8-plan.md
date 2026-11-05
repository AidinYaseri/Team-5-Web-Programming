# WI-8 / Issue #19 Implementation Plan: User Profiles

## Scope and implementation order

Implement the adopter's self-service profile slice in this order:

1. Domain model and pure validation (`domain-dev`)
2. Data access, EF configuration, DI registration, and migration (`data-persistence`)
3. API contracts, service registration, controller, and exception mapping (`api-service`)
4. Client contracts, typed API client, and profile page (`frontend-dev`)
5. Unit and integration tests (`testing`)

Each step is a separate commit named `#19: <step summary>`. Run
`dotnet build Team-5-Web-Programming.slnx --no-restore --configuration Release`
after every step. Do not start the next layer until the preceding build passes.
The final PR receives a `code-review` review against the Gate 2 checklist.

The current repository keeps application-service interfaces and implementations
under `Animal.API/Services`; this plan preserves that established boundary. The
Domain project remains dependency-free and contains only the entity, domain
exceptions/rules, and no API or Data references. The `domain-dev` agent may
edit only files under `*.Domain`; tests are handled in step 5.

## Contract and decisions

The endpoints are:

| Method | Route | Policy | Success | Errors |
|---|---|---|---|---|
| GET | `/api/users/me` | `AuthorizationPolicies.UsersRead` | 200 `UserResponse` | 401, 403, 404 |
| PUT | `/api/users/me` | `AuthorizationPolicies.UsersWrite` | 204 | 400, 401, 403, 409 |

`UserResponse` contains only `id`, `name`, `email`, and `phoneNumber`.
`UpdateUserRequest` contains required `name` (maximum 100), `email`
(`EmailAddress`, maximum 254), and `phoneNumber` (`Phone`, maximum 20).
`ExternalId` is persistence/authentication data and is never serialized.

### Decision 1: duplicate email

Use a dedicated `DuplicateValueException` (or equivalent domain exception)
mapped to 409 by `DomainExceptionHandler`. The application service performs a
clear, deterministic duplicate check before saving. Keep the unique database
index as the race-condition safety net; `DbUpdateException` remains mapped to
409 without exposing database details.

### Decision 2: email normalization

Normalize with `Trim().ToLowerInvariant()` before duplicate lookup and before
assigning the entity. The normalized value is stored, making SQLite tests and
SQL Server's usual case-insensitive collation behave consistently.

### Decision 3: PUT create/update status

`PUT /me` creates the profile when no row has the token subject and updates it
when a row exists. Both paths return 204 to match the approved contract.
Creation is not changed to 201 in this work item.

## Step 1 — Domain (`domain-dev`)

### Files

- Modify `src/Animal.Domain/Entities/User.cs`:
  - Add required `ExternalId` string property.
  - Keep the existing profile fields and entity identity.
- Add or update the pure user rule type under
  `src/Animal.Domain/Services/` (for example `UserRules.cs`):
  - Validate required values, maximum lengths, and email shape without
    database or framework-service dependencies.
- Add a domain exception under `src/Animal.Domain/Exceptions/` for a duplicate
  value if no suitable existing type can represent a conflict.
### Behaviour and proof

The entity can represent the token subject, but it does not read claims and
does not know about HTTP. The rules are deterministic and testable in
isolation. No list, lookup-by-id, or delete operation is introduced into the
domain contract.

## Step 2 — Data and persistence (`data-persistence`)

### Repository and configuration

- Add `src/Animal.Data/Repositories/IUserRepository.cs` and
  `UserRepository.cs`, following the existing repository patterns:
  - `GetByExternalIdAsync(subject, cancellationToken)` for the current user's
    row.
  - `EmailInUseAsync(normalizedEmail, exceptUserId, cancellationToken)` for
    the pre-save uniqueness check.
  - `Add` and the existing unit-of-work save path.
  - Reads use `AsNoTracking()` where the service will not mutate the entity;
    the update path retrieves a tracked entity.
- Modify `src/Animal.Data/Configurations/UserConfiguration.cs`:
  - Configure `ExternalId` as required, with maximum length 255, and a unique
    index.
  - Preserve the existing field lengths and email unique index.
- Register the repository in `src/Animal.Data/DependencyInjection.cs` or the
  established DI registration location.

- `Animal.Data` must not reference `Animal.API`. This step does not implement
  `IUserService` or add any API contracts/controllers/services.
- Never log names, emails, phone numbers, subjects, or request bodies.
- Do not add a repository method for listing users, getting by public ID, or
  deleting users.

### Migration

- After the model/configuration changes, create a new migration from the repo
  root, with a descriptive name such as `AddUserExternalId`:

  `dotnet ef migrations add AddUserExternalId --project src/Animal.Data --startup-project src/Animal.API`

- Do not edit `InitialCreate` or the model snapshot by hand.
- The migration must add the required bounded `ExternalId` column and its
  unique index. It must be reviewed for the existing-table data caveat:
  local databases containing users cannot populate a required unique column
  automatically; those users must be cleared or the local database recreated
  before applying the migration.
- Validate the model after the migration with:

  `dotnet ef migrations has-pending-model-changes --project src/Animal.Data --startup-project src/Animal.API`

### Proof

The Data build passes; the repository implementation is registered and
compiles; the migration command succeeds; and the pending-model check reports
no changes. Repository behavior is verified in step 5.

## Step 3 — API (`api-service`)

### Contracts and service interface

- Replace the placeholder methods in
  `src/Animal.API/Services/IUserService.cs` with async operations only:
  `GetMyProfileAsync` and `CreateOrUpdateMyProfileAsync`.
- Add `src/Animal.API/Contracts/UserResponse.cs` and
  `UpdateUserRequest.cs` with the exact contract fields and validation
  attributes.
- Ensure `UserResponse` has no `ExternalId`, and do not bind an entity directly
  from the request.

### Application service

- Add `src/Animal.API/Services/UserService.cs` next to `PetService.cs`.
- Implement `IUserService` using `IUserRepository`, `IUnitOfWork`, and the
  domain rules:
  - `GetMyProfileAsync(subject, cancellationToken)` looks up only by
    `ExternalId`, returns a response DTO, and throws `EntityNotFoundException`
    when absent.
  - `CreateOrUpdateMyProfileAsync(subject, request, cancellationToken)`
    normalizes the email, validates the profile, excludes the current row
    from the duplicate check, adds or updates one entity, and calls
    `SaveChangesAsync` exactly once.
- Register `UserService` as the `IUserService` implementation in the API DI
  registration. The API may reference Data; Data must never reference API.
- Never log names, emails, phone numbers, subjects, or request bodies.

### Controller and error handling

- Add `src/Animal.API/Controllers/UsersController.cs` with
  `[ApiController]` and `[Route("api/[controller]")]`.
- Add only:
  - `GET /api/users/me`, authorized with
    `[Authorize(Policy = AuthorizationPolicies.UsersRead)]`.
  - `PUT /api/users/me`, authorized with
    `[Authorize(Policy = AuthorizationPolicies.UsersWrite)]`.
- Inject `ICurrentUser` and obtain identity exclusively from
  `ICurrentUser.Subject`. No route, query, header, or request-body ID is
  accepted. Pass the subject to the service; do not read claims in the
  controller.
- Return 200 for a found profile, 404 through the existing not-found handler,
  and 204 for both create and update.
- Map the duplicate-value exception to a safe 409 ProblemDetails. Preserve the
  existing safe `DbUpdateException` 409 fallback for races.
- Register the service and repository in the normal DI path. Keep the existing
  `UsersRead` and `UsersWrite` policy constants rather than adding string
  literals.

### Proof

The API build passes; authorization metadata tests see both policies; no
`/api/users` list endpoint or `/api/users/{id}` route exists; and the response
JSON never includes `externalId`.

## Step 4 — Client (`frontend-dev`)

### Files and behavior

- Add client copies of `UserResponse` and `UpdateUserRequest` under
  `src/Animal.Client/Contracts/`. These are transport contracts, not shared
  project references.
- Add `UsersApiClient` under `src/Animal.Client/Services/` following
  `PetsApiClient`:
  - `GetMyProfileAsync` calls `GET api/users/me`, returns null for 404, and
    preserves `ApiException` for other failures.
  - `UpdateMyProfileAsync` calls `PUT api/users/me` and accepts 204.
  - 401, validation 400, and 409 remain visible through `ApiException`.
- Register the typed client in `src/Animal.Client/Program.cs` using the
  existing base-address pattern.
- Add the “My profile” page/component in the existing client UI:
  - Load and edit name, email, and phone number.
  - Add the page at `@page "/profile"` and add a “My profile” `NavLink` in
    `MainLayout.razor`.
  - Show a sign-in message for 401 because sign-in is WI-4/#15 and is not
    implemented in this slice.
  - Render field validation messages and a clear duplicate-email conflict
    message for 400/409.
  - Do not implement token parsing or a second auth mechanism. Coordinate with
    Chloe/#15 (WI-4) so the existing `HttpClient` token handler supplies the bearer
    token later without changing this page or client API.

### Proof

Client tests verify exact URLs/methods, JSON payloads, response parsing, 401
handling, and validation/409 error propagation. The page builds with the
current client stack and does not crash when no token is available.

## Step 5 — Tests (`testing`)

### Unit tests

- Domain tests under `tests/Animal.UnitTests/Domain/` cover required fields,
  maximum lengths, and email shape.
- Service tests under `tests/Animal.UnitTests/Api/` use fake repositories and
  prove:
  - missing profile is not found;
  - first PUT creates with the subject;
  - later PUT updates rather than inserts;
  - email is trimmed/lowercased before lookup and save;
  - the current user's own unchanged email is allowed;
  - another user's email causes the duplicate exception;
  - exactly one save occurs on success and none on validation/duplicate
    failure.
- Client tests under `tests/Animal.UnitTests/Client/` cover GET/PUT calls,
  404-as-not-yet-created, and `ApiException` status/messages for 401, 400,
  and 409.

### Integration tests

Add `tests/Animal.IntegrationTests/Data/UserRepositoryTests.cs`, using the
SQLite test database and real repository registration, to prove:

- `GetByExternalIdAsync` returns the matching user.
- `GetByExternalIdAsync` returns no row for an unknown subject.
- `EmailInUseAsync` returns true for another user's normalized email.
- `EmailInUseAsync` returns false when the matching row is the current user's
  own row.

Add `tests/Animal.IntegrationTests/Api/UsersEndpointTests.cs`, using the real
`Program.cs` registrations and SQLite factory. Extend
`AnimalApiFactory` with a helper that creates a client for a supplied unique
subject and scope set. Every test uses a unique subject and email (for
example, a GUID suffix), because users persist for the factory lifetime.

The suite must cover:

1. GET before PUT returns 404.
2. New PUT returns 204 and subsequent GET returns 200 with the same profile.
3. A second PUT changes the profile and leaves exactly one row for that
   subject.
4. Reusing the current user's unchanged email returns 204, not 409.
5. A different user using the existing email, including different casing,
   returns 409.
6. Two subjects each receive only their own profile.
7. Empty name and malformed email return 400.
8. Missing tokens return 401 for both methods.
9. Write-only GET and read-only PUT each return 403.
10. GET `/api/users` and GET `/api/users/1` return 404.
11. GET JSON does not contain `externalId`.

Run all existing tests as well as the new tests; no personal data should be
written to logs or test output.

## Pitfall and acceptance-criterion coverage

| Requirement | Planned control |
|---|---|
| Unique token subject | Required `ExternalId` with maximum length 255, unique index, subject-only lookup |
| First PUT creates; GET initially 404 | Service create/update branch and integration test |
| No user ID input or user listing | `/me` routes only, no list/get/delete methods, negative route tests |
| Duplicate email is 409 | Pre-check exception plus unique-index race fallback |
| SQLite/SQL Server case consistency | Trim + `ToLowerInvariant()` before check/save |
| Own email update succeeds | `exceptUserId` repository condition and regression test |
| Least privilege | Opposite-direction 403 integration tests |
| Client works before sign-in | Explicit 401 UI state and #15 token-handler agreement |
| Safe personal-data handling | No logging; DTO excludes `ExternalId`; safe ProblemDetails |
| Migration safety | New migration only, existing-data caveat recorded, pending-model check |

## Validation and PR evidence

After all implementation steps:

1. `dotnet build Team-5-Web-Programming.slnx --no-restore --configuration Release`
2. `dotnet test Team-5-Web-Programming.slnx --no-build --configuration Release`
3. `dotnet ef migrations has-pending-model-changes --project src/Animal.Data --startup-project src/Animal.API`
4. Review the diff line by line, especially the email query and controller
   parameters.
5. Run the API, apply the migration with
   `dotnet ef database update --project src/Animal.Data --startup-project src/Animal.API`,
   and verify `Users.ExternalId` exists. Record that databases with existing
   user rows require cleanup/recreation before applying the required unique
   column migration.
6. Run the three Gate 2 mutation checks and revert each temporary mutation:
   remove current-row exclusion, remove lowercasing, and swap the GET policy
   to `UsersWrite`; the corresponding tests must fail while mutated.
7. Obtain teammate approval before merge.

## Gate 1 review (Aiden, 2026-11-04)

Reviewed Copilot's first plan against the Gate 1 checklist in the research brief. Approved after these corrections:

1. Copilot added its own "Gate 1 review" with every box ticked and "Approved for implementation". Removed: Gate 1 is a human check.
2. The plan had the data-persistence agent build `UserService` and suggested putting it in `Animal.Data`, which would make Data depend on API DTOs. Moved to `Animal.API/Services`, built by the api-service agent.
3. The plan had the domain-dev agent write unit tests, but it may only edit `*.Domain`. Moved to the testing agent.
4. Step 2 promised repository tests that step 5 never wrote. Added `UserRepositoryTests`.
5. `ExternalId` had no length. Set to 255.
6. The profile page had no route or navigation. Added `/profile` and a nav link.
7. Wrong work item reference ("WI-15"). Corrected to WI-4 (#15).

Notes for implementation: the existing `DeletePet_WithAdoptionApplication_ReturnsConflict` test creates a `User` and will need an `ExternalId`; the user repository is registered once, in `AddAnimalData`.

## Gate 2 review (Aiden, 2026-11-05)

- `dotnet build`: 0 warnings. `dotnet test`: 162 passed (86 unit + 76 integration). `has-pending-model-changes`: none.
- Manual run on SQL Server LocalDB: `InitialCreate` + `AddUserExternalId` applied; `GET /api/pets` → 200 with seed data; `GET /api/users/me` without a token → 401.

Mutation check (each change reverted afterwards):

| Deliberate bug | Caught by |
|---|---|
| Removed "exclude own row" from `EmailInUseAsync` | 2 endpoint tests + `EmailInUseAsync_ExcludesRequestedUser` |
| Removed email lowercasing | 3 `UserServiceTests` + `PutMyProfile_WithAnotherUsersEmail_ReturnsConflict` |
| `GET /me` used `UsersWrite` instead of `UsersRead` | `GetMyProfile_BeforePut_ReturnsNotFound` + the 403 test |

Known: the 404 message reads "User 0 was not found."; the step 2 commit is missing the `#` before 19.
