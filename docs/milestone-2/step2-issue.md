# Step 2: Create the issue

Owner: Chloe

## Prompt for Copilot (Agent mode, GitHub MCP on)

```
Draft a GitHub issue for AidinYaseri/Team-5-Web-Programming. Don't create it yet, show me the draft.

Feature: let clients filter the pet list. GET /api/pets should accept optional
query parameters species, maxAge and availableOnly.

Read .github/copilot-instructions.md and src/Animal.Domain (Pet model, IPetService)
first. The issue must cover: the user story, desired behaviour, inputs and outputs,
validation rules, constraints (layers, DTOs, tests), acceptance criteria, and what
is out of scope.
```

After reviewing and editing the draft, tell Copilot: `Create the issue with the edited text below and add the label "enhancement".`

## Edited issue (what should end up on GitHub)

**Title:** Filter pets by species, age and availability on GET /api/pets

**Body:**

### User story
As someone looking to adopt, I want to filter the pet list by species, age and whether the pet is still available, so I don't have to scroll through animals I can't or won't adopt.

### Current state
`IPetService` has `GetPets()` and `GetAvailablePets()`, but no way to filter by species or age. There is no `PetsController` and no service implementation yet.

### Desired behaviour
`GET /api/pets` returns pets that match every filter given. All filters are optional.

| Query parameter | Type | Rule |
|---|---|---|
| `species` | string, optional | Exact match, ignores case and surrounding spaces. Blank means no filter. Max 50 characters. |
| `maxAge` | int, optional | Pets with `Age <= maxAge`. Must be 0 to 100. |
| `availableOnly` | bool, optional, default `false` | When `true`, only pets with `IsAvailable = true`. |

### Output
- `200 OK` with a JSON array of pets (`id`, `name`, `species`, `breed`, `age`, `isAvailable`, `shelterId`), sorted by `id`.
- `200 OK` with `[]` when nothing matches.
- `400 Bad Request` with a `ValidationProblem` body when a parameter is invalid (for example `maxAge=-1` or `maxAge=abc`).

### Constraints
- Add `SearchPets(PetSearchCriteria criteria)` to `IPetService`. `PetSearchCriteria` goes in `Animal.Domain/Models`.
- No EF Core yet, so add an in-memory `IPetService` implementation in `Animal.Data` with fake seed data. It gets replaced when the DbContext exists.
- The controller only binds, validates, calls the service and maps to a DTO. No filtering logic in the controller and no entities in responses.
- `GetAvailablePets()` must keep working.
- Unit tests for the filter logic and integration tests for the endpoint.
- Update the class diagram (`.mmd`, README, PNG) and `Animal.API.http`.

### Acceptance criteria
- [ ] `GET /api/pets` with no parameters returns every pet
- [ ] `?species=dog` and `?species=DOG` return the same pets
- [ ] `?maxAge=3` includes pets that are exactly 3
- [ ] `?species=dog&maxAge=5&availableOnly=true` applies all three filters
- [ ] Invalid input returns 400 with a ValidationProblem
- [ ] `dotnet build` has no new warnings and `dotnet test` passes in CI

### Out of scope
Paging, sorting options, partial or fuzzy name search, filtering by shelter or breed, EF Core persistence, authentication.

## What we changed from Copilot's draft

Copilot (GPT-5 mini, Agent mode, GitHub MCP on) wrote the draft below. It read our branch, where `SearchPets` was already there, so a few parts of the draft talk about it as if it already exists. We rewrote it so the issue describes the repo as it is on `main`.

| Copilot's draft | What we did | Why |
|---|---|---|
| Says `IPetService.SearchPets(PetSearchCriteria)` "already exists" | Changed to "add it" and added a Current state section | On `main` it doesn't exist yet. Copilot read our working branch, not `main`. |
| `species` max length "e.g. <= 100" | Picked 50 | It gave an example, not a rule. 50 is plenty for a species name. |
| `maxAge` only has to be >= 0 | 0 to 100 | No upper limit means `maxAge=99999` is valid, which makes no sense. |
| `availableOnly` had no default | Default is `false` | Makes "no filter" clear. |
| No mention of an empty result or sort order | Added `[]` when nothing matches and sorted by `id` | So the tests have a fixed order to check. |
| 500 with ProblemDetails | Removed | ASP.NET already does this, it isn't part of the feature. |
| Filtering in an `Animal.Data` repository with EF query practices | In-memory `IPetService` in `Animal.Data` with seed data | We don't have EF Core or a DbContext yet. |
| CancellationToken and async variants | Left out | The interfaces are sync right now. We'll make a separate issue for async. |
| OpenAPI annotations for the query params | Left out | Nice to have, not needed for this issue. |
| Didn't mention `GetAvailablePets()`, the class diagram or the `.http` file | Added to Constraints | Our repo rules say the diagram has to be updated with code changes. |
| Acceptance criteria had no build warnings check | Added "no new warnings and tests pass in CI" | Matches our definition of done. |

What Copilot got right and we kept: the user story idea, the three optional query params, AND between filters, thin controller, DTOs instead of entities, 400 with ProblemDetails for bad input, unit tests plus `WebApplicationFactory` endpoint tests, and the out of scope list (paging, fuzzy search, auth, schema changes).

<details>
<summary>Copilot's original draft (unedited)</summary>

```text
Title
Feature: allow filtering on GET /api/pets by species, maxAge and availableOnly

Description
- Background: clients need to filter the pet list endpoint. The domain contains Pet entity and IPetService with SearchPets(PetSearchCriteria). Implement query parameters on GET /api/pets to call into the domain search and return DTOs.
- User story: As an API client, I want to request GET /api/pets?species={species}&maxAge={n}&availableOnly={bool} so I can show filtered lists (by species, age cap, and availability) without client-side filtering.

Desired behavior
- GET /api/pets accepts optional query parameters:
  - species (string): case-insensitive exact-match (or mapped by service) on Pet.Species
  - maxAge (int): return pets with Age <= maxAge
  - availableOnly (bool): when true return only pets where IsAvailable == true; when absent or false, include all
- If no filters provided, endpoint returns same list as existing GET /api/pets.
- Controller must map query parameters into a PetSearchCriteria DTO and call IPetService.SearchPets(criteria).
- Response must be an array of Pet response DTOs (not domain entities).

Inputs and outputs
- Inputs (query params):
  - species (optional): string
  - maxAge (optional): integer >= 0
  - availableOnly (optional): boolean (true/false)
- Outputs:
  - 200 OK with JSON array of pet response DTOs: { id, name, species, breed, age, isAvailable, shelterId } (follow existing Contracts naming)
  - 400 Bad Request for invalid parameter values (e.g., non-numeric maxAge, negative maxAge, invalid boolean)
  - 500 for unexpected server errors (use ProblemDetails)

Validation rules
- species: optional; trim whitespace; if present, length limit (e.g., <= 100); reject if too long.
- maxAge: optional; must parse to non-negative integer; if negative or not an integer, return 400 with validation ProblemDetails.
- availableOnly: optional; parse boolean using standard true/false parsing; invalid values -> 400.
- Reject unknown/extra parameters only if they cause parsing failures; don't treat unknown params as errors.

Constraints and implementation notes
- Follow project layering: Controller -> Animal.Domain.Services.IPetService -> Animal.Data repository. No DbContext or business logic in controller.
- Use DTOs in src/Animal.API/Contracts for responses; do not return Animal.Domain.Models.Pet directly.
- Map query params into a PetSearchCriteria model (reuse or add in Animal.Domain if it does not exist). IPetService.SearchPets(PetSearchCriteria) already exists; use it.
- Implement unit tests for the service method behavior (Animal.Data.Tests) and integration tests for the API endpoint (Animal.API.Tests) using WebApplicationFactory<Program> and ConfigureTestServices to inject a test repository/fake data.
- Keep nullable annotations and async naming conventions; use CancellationToken in new async controller/service methods if adding async variants.
- Add OpenAPI docs annotation (query param descriptions) so the generated openapi shows the filters.

Acceptance criteria
- GET /api/pets returns the full list when no query params provided (same behavior as before).
- GET /api/pets?species=dog returns only pets whose Species matches "dog" (case-insensitive).
- GET /api/pets?maxAge=3 returns pets with Age <= 3.
- GET /api/pets?availableOnly=true returns only pets with IsAvailable == true.
- Filters combine with logical AND: e.g., ?species=cat&maxAge=2&availableOnly=true returns pets that satisfy all three.
- Invalid maxAge or availableOnly yields 400 with ProblemDetails describing the error.
- Unit tests cover service-level filtering logic; integration tests cover controller routing, parameter binding, validation, and response DTO shape.
- No domain entities are serialized to clients; responses use Contracts DTOs.
- Code compiles and tests pass.

Out of scope
- Pagination or sorting of the pet list.
- Fuzzy or partial text matching on species (only exact/case-insensitive match as specified).
- Authentication/authorization changes.
- Database schema changes or EF migrations (filtering should be implemented in queries/repository without changing model).
- Performance optimizations beyond normal EF query practices.

Notes / Implementation hints
- Reuse IPetService.SearchPets(PetSearchCriteria); add fields species, maxAge, availableOnly to PetSearchCriteria if missing.
- Controller should be thin: bind query params to a request/criteria DTO, validate, call SearchPets, map results to Contracts/PetResponseDto and return 200.
- Add tests in tests/Animal.API.Tests for these query combinations using in-memory or seeded test data.
```

</details>

Issue link: [#7](https://github.com/AidinYaseri/Team-5-Web-Programming/issues/7), created by Copilot through the GitHub MCP `issue_write` tool with the label `enhancement`.
