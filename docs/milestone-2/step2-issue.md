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

<!-- Fill this in after running the prompt. Things to look for and note:
- Did Copilot invent fields that aren't on the Pet model (e.g. size, gender, photo)? Remove them.
- Did it suggest putting the filter in the controller? The rules say service.
- Did it make species a partial match? We picked exact match on purpose.
- Did it forget the 400 case or the "no filters returns everything" case? -->

Issue link: <!-- paste once created -->
