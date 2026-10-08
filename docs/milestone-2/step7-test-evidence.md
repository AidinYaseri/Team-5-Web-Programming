# Step 7: Validate

Owner: Aiden
Feature: filter pets by species, max age and availability (`GET /api/pets`)

## Test projects added

Before this milestone the solution had no tests at all. We added two xUnit projects and put them in `Team-5-Web-Programming.slnx`, so `dotnet test` (and CI) runs them automatically.

| Project | Type | What it covers |
|---|---|---|
| `tests/Animal.Data.Tests` | Unit | `InMemoryPetService.SearchPets` filter logic, plus a regression test for `GetAvailablePets` |
| `tests/Animal.API.Tests` | Integration | The real HTTP endpoint through `WebApplicationFactory<Program>`: query binding, JSON output, validation errors, and the real DI setup |

## Test list

**`InMemoryPetServiceTests` (19 test cases)**

| Test | Checks |
|---|---|
| `SearchPets_WithNoFilters_ReturnsAllPetsOrderedById` | No filters returns every pet, sorted by Id |
| `SearchPets_BySpecies_IgnoresCaseAndSurroundingSpaces` (4 cases) | `Dog`, `dog`, `DOG`, `"  dog  "` all match |
| `SearchPets_WithBlankSpecies_DoesNotFilterBySpecies` (3 cases) | `null`, `""` and `"   "` act like no filter |
| `SearchPets_BySpecies_DoesNotMatchPartialNames` | `Do` does not match `Dog` (exact match only) |
| `SearchPets_ByMaxAge_IncludesPetsExactlyThatAge` | `maxAge` is inclusive |
| `SearchPets_WithMaxAgeZero_ReturnsOnlyNewborns` | `maxAge=0` is a real filter, not "no filter" |
| `SearchPets_AvailableOnly_SkipsAdoptedPets` | Adopted pets are left out |
| `SearchPets_WithAllFilters_CombinesThemWithAnd` | Filters are combined with AND |
| `SearchPets_WhenNothingMatches_ReturnsEmptyList` | Empty list, not null |
| `SearchPets_WithNullCriteria_Throws` | `ArgumentNullException` |
| `SearchPets_ReturnsNewList_SoCallersCannotChangeTheStore` | Clearing the result doesn't delete pets |
| `SearchPets_SeesPetsAddedWithCreatePet` | New pets show up in search |
| `GetAvailablePets_StillReturnsOnlyAvailablePets` | Regression: old method still works after being rewritten to use `SearchPets` |
| `DefaultConstructor_LoadsSeedData` | Seed data loads |

**`PetsEndpointTests` (15 test cases, 10 before Copilot's coverage check plus 5 added from it)** use `ConfigureTestServices` to swap in 3 known fake pets.

| Test | Checks |
|---|---|
| `GetPets_WithoutQuery_ReturnsAllPets` | `GET /api/pets` returns 200 and all pets |
| `GetPets_BySpecies_ReturnsMatchingPets` | `?species=dog` |
| `GetPets_WithAllFilters_ReturnsOnlyPetsMatchingEveryFilter` | `?species=Dog&maxAge=5&availableOnly=true` |
| `GetPets_WhenNothingMatches_ReturnsEmptyArray` | 200 with `[]` |
| `GetPets_ReturnsDtoFieldsAsCamelCaseJson` | JSON uses the DTO with camelCase names |
| `GetPets_WithInvalidQuery_ReturnsValidationProblem` (5 cases) | `maxAge=-1`, `maxAge=101`, `maxAge=abc`, `maxAge=3.5`, `availableOnly=maybe` return 400 `application/problem+json` |
| `GetPets_WithSpeciesLongerThan50Characters_ReturnsBadRequest` | `[StringLength(50)]` is enforced |
| `GetPets_BySpecies_UppercaseIsEquivalent` (new) | `?species=DOG` gives the same pets as `?species=dog` through the real endpoint |
| `GetPets_SpeciesExactlyFiftyCharacters_ReturnsOk` (new) | 50 characters is still allowed |
| `GetPets_MaxAge100_ReturnsAllPets` (new) | `maxAge=100` is allowed |
| `GetPets_AvailableOnlyFalse_EqualsNoFilter` (new) | Sending `false` is the same as leaving it out (fake data has one adopted pet, so this means something) |

**`PetsStartupTests` (1 test case)** uses the real `Program.cs` with no overrides.

| Test | Checks |
|---|---|
| `GetPets_WithRealServices_ReturnsSeedData` | Regression for the DI bug below: the real app returns the seed pets, not `[]` |

## Coverage check with Copilot

New chat, Agent mode, GPT-5 mini, GitHub MCP on:

```
Validate issue #7 on this branch. Follow .github/copilot-instructions.md.
1. Run: dotnet build Team-5-Web-Programming.slnx -c Release, then dotnet test Team-5-Web-Programming.slnx --no-build -c Release. Show me the counts.
2. Read issue #7 with the GitHub MCP tools and go through every acceptance criterion and validation rule. For each one, name the test in tests/ that covers it, or say it isn't covered.
3. Look for edge cases the tests miss. Don't write any tests yet, just list them.
```

Copilot ran the build and tests (30/30) and mapped every acceptance criterion in #7 to a test. We checked the mapping against the test files and it was right. It found that the boundaries were only tested on the failing side (51 characters, `maxAge=101`) and that uppercase species was only tested at the service level, not through the endpoint.

It listed 13 edge cases. What we did with them:

| Edge case | Decision | Why |
|---|---|---|
| `?species=DOG` at the endpoint | Added | Acceptance criterion in #7 |
| Species of exactly 50 characters | Added | Boundary of a rule in #7 |
| `maxAge=100` | Added | Boundary of a rule in #7 |
| `maxAge=3.5` | Added to the invalid theory | Has to be an int |
| `availableOnly=false` | Added | Default is false, so it should match no filter |
| Empty `availableOnly=`, repeated params, spaces inside species | Skipped | The issue doesn't say what should happen, so a test would be making up a rule |
| Unknown extra params | Skipped | `[ApiController]` ignores them already, not part of #7 |
| Turkish i casing, large data sets, concurrency, null fields | Skipped | Out of scope for an in-memory demo service |

Then in the same chat:

```
Add only these endpoint tests to tests/Animal.API.Tests/PetsEndpointTests.cs, following the style already in that file (use the existing fake pets and helpers, [Theory]/[InlineData] where it fits):
1. ?species=DOG returns the same ids as ?species=dog
2. species of exactly 50 characters returns 200 (empty array is fine)
3. ?maxAge=100 returns 200 with all pets
4. ?maxAge=3.5 returns 400 application/problem+json (add it to the existing invalid-query theory)
5. ?availableOnly=false returns the same ids as no query
Don't change any code in src/. Then run dotnet build and dotnet test again and show me the counts and the diff.
```

Copilot added all 5 and reported 35/35. The diff it printed in chat was messy (it repeated the `[InlineData]` lines and dropped one test), so we checked the real file with `git diff`. The file was fine.

**Our correction:** the 50 character test ended with `Assert.NotNull(ids)`, which can never fail because the helper already returns a non-null array. We changed it to `Assert.Empty(ids)` so it actually checks the response.

## Commands and results

Run from the repo root, same order as CI:

```bash
dotnet restore Team-5-Web-Programming.slnx
dotnet build Team-5-Web-Programming.slnx --no-restore --configuration Release
dotnet test Team-5-Web-Programming.slnx --no-build --configuration Release
```

Result (.NET SDK 10.0.400, on Aiden's machine):

```
Build succeeded.
    13 Warning(s)
    0 Error(s)

Animal.Data.Tests  Total tests: 19   Passed: 19
Animal.API.Tests   Total tests: 16   Passed: 16
```

All 13 warnings are `CS8618` (non-nullable string properties) in the existing `Animal.Domain/Models` files. They were already there before this change. The change adds no new warnings.

## Showing the tests actually catch bugs

**Red before green.** Before the implementation was added, the test projects did not compile (`InMemoryPetService`, `Animal.API.Contracts` not found). So the tests depend on the new code and are not passing by accident.

**Mutation check.** We changed `p.Age <= criteria.MaxAge` to `p.Age < criteria.MaxAge` in `InMemoryPetService` on purpose and reran the tests:

```
Failed SearchPets_WithMaxAgeZero_ReturnsOnlyNewborns
Failed SearchPets_ByMaxAge_IncludesPetsExactlyThatAge
Failed!  - Failed: 2, Passed: 17, Total: 19 - Animal.Data.Tests.dll
```

Then we put the code back and everything passed again.

## Bug found during validation

When we ran the real API, every request returned `[]`, even though the seed data has 5 pets. All the tests passed at that point, so they had missed it.

- **Cause:** `Program.cs` registered `AddSingleton<IPetService, InMemoryPetService>()`. The DI container picks the constructor with the most parameters it can fill. It can always fill `IEnumerable<Pet>` (with an empty list), so it used `InMemoryPetService(IEnumerable<Pet>)` instead of the constructor that loads the seed data.
- **Why the tests missed it:** the unit tests call `new InMemoryPetService(...)` directly, and the endpoint tests replace the service with their own. Nothing went through the real registration.
- **Fix:** added `PetsStartupTests.GetPets_WithRealServices_ReturnsSeedData` first and confirmed it failed (`Assert.NotEmpty() Failure: Collection was empty`). Then changed the registration to a factory, `AddSingleton<IPetService>(_ => new InMemoryPetService())`. The test passes now.

## Manual check with the running API

`dotnet run --project src/Animal.API --launch-profile http`, then:

```
$ curl "http://localhost:5015/api/pets?species=cat&availableOnly=true"
[{"id":2,"name":"Mochi","species":"Cat","breed":"Siamese","age":1,"isAvailable":true,"shelterId":1},{"id":5,"name":"Luna","species":"Cat","breed":"Domestic Shorthair","age":9,"isAvailable":true,"shelterId":1}]

$ curl "http://localhost:5015/api/pets?species=DOG&maxAge=5"
[{"id":1,"name":"Biscuit","species":"Dog","breed":"Beagle","age":3,"isAvailable":true,"shelterId":1}]

$ curl -i "http://localhost:5015/api/pets?maxAge=-1"
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json; charset=utf-8

{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"MaxAge":["The field MaxAge must be between 0 and 100."]}}
```

The same requests are saved in `src/Animal.API/Animal.API.http`.

## Evidence still to add

- [ ] Screenshot of Test Explorer (or terminal) showing 35/35 passing on your machine
- [ ] Link to the green CI run on the PR
