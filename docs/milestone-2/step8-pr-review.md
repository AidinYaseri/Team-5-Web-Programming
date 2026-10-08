# Step 8: Pull request and review record

Owner: Aiden

## Prompt used to open the PR (Copilot Agent mode, GitHub MCP)

```
Open a pull request in AidinYaseri/Team-5-Web-Programming from branch Copilot_instructions into main.
Title: "Add species, age and availability filters to GET /api/pets"
Use the "PR description" section of docs/milestone-2/step8-pr-review.md as the body,
link it to issue #7, and request a review from the rest of Team 5.
```

PR link: [#8](https://github.com/AidinYaseri/Team-5-Web-Programming/pull/8), `Copilot_instructions` into `main`, 14 commits. CI check passed. Merged by Chloe as `f23a44c`.

**What didn't go to plan:** the PR was opened with no description (the body below never got pasted in), it wasn't linked to #7 so #7 didn't close on merge, and it was merged before anyone (or Copilot) left a review on GitHub. Findings 1 to 8 below come from our own review during Step 7, not from a review on the PR. See "Still to do" at the bottom.

## PR description

> **Add species, age and availability filters to GET /api/pets**
>
> Closes #7
>
> **What changed**
> - `IPetService` gets `SearchPets(PetSearchCriteria)`. The new `PetSearchCriteria` model holds the optional filters (`Species`, `MaxAge`, `AvailableOnly`).
> - `InMemoryPetService` in `Animal.Data/Services` implements `IPetService` with fake seed data (`Animal.Data/SeedData/PetSeedData.cs`). It's temporary until `AnimalDbContext` exists. `GetAvailablePets` now reuses `SearchPets`.
> - New `PetsController` with `GET /api/pets?species=&maxAge=&availableOnly=`. Input binds to the `PetSearchQuery` DTO (`species` max 50 chars, `maxAge` 0 to 100). Output is the `PetResponse` DTO, never the entity. Bad input returns 400 `ValidationProblem`.
> - `Program.cs` registers the service through a factory (see review finding 1).
> - New test projects `tests/Animal.Data.Tests` (19 cases) and `tests/Animal.API.Tests` (16 cases), added to the solution.
> - `Animal.API.http`, the README class diagram and `services-class-diagram.mmd` updated.
>
> **Behaviour**
> - Species matching ignores case and surrounding spaces, and is exact (`Do` does not match `Dog`).
> - `maxAge` is inclusive. Filters combine with AND. No filters returns every pet. No match returns `[]`.
>
> **How it was tested**
> `dotnet build` (0 errors, no new warnings) and `dotnet test` (35/35 passing). Manual checks with `Animal.API.http`. Details in `docs/milestone-2/step7-test-evidence.md`.
>
> **Out of scope**
> Paging, sorting, partial name search, EF Core persistence, authentication.

## Review pass

Findings 1 to 8 come from validation (Step 7) and a manual review of the diff against the change checklist in `.github/copilot-instructions.md` (section 6). After the PR is open, run `.github/prompts/review-pr.prompt.md` in Copilot Agent mode on it and add any new findings as rows 9 and up, each with an outcome.

| # | Severity | File | Finding | Found by | Outcome |
|---|---|---|---|---|---|
| 1 | High | `src/Animal.API/Program.cs` | `AddSingleton<IPetService, InMemoryPetService>()` made DI pick the `IEnumerable<Pet>` constructor and inject an empty list, so the real API always returned `[]`. All tests still passed. | Manual run during validation (Step 7) | **Fixed.** Registered with a factory, `_ => new InMemoryPetService()`. Added `PetsStartupTests` that uses the real DI setup. It failed before the fix and passes after. |
| 2 | Medium | `README.md`, `docs/diagrams/services-class-diagram.mmd` | The class diagram didn't list the new `SearchPets` method (checklist item 7). | Review against the checklist | **Fixed** in the `.mmd`, the README block and `services-class-diagram.png` (re-exported with mermaid-cli). |
| 3 | Low | `src/Animal.API/Animal.API.http` | Still called the template's `/weatherforecast/`, which doesn't exist. | Review | **Fixed.** Replaced with 4 `/api/pets` requests, including one that should return 400. |
| 4 | Medium | `src/Animal.Domain/Services/IPetService.cs` | Repo rules say I/O methods are `async` and take a `CancellationToken`. `SearchPets` is synchronous. | Review against the rules | **Deferred.** All six service interfaces are synchronous right now. Changing only one would be inconsistent. Better done in one follow-up issue when EF Core is added. |
| 5 | Medium | `src/Animal.Data/Services/InMemoryPetService.cs` | Search returns a new list, but the `Pet` objects inside are the stored ones. A caller that edits a returned `Pet` changes the store without calling `UpdatePet`. | Review | **Deferred.** The API isn't affected because the controller maps to `PetResponse`. The class goes away when EF Core is added. |
| 6 | Low | `src/Animal.API/Controllers/PetsController.cs` | No `[Authorize]` or `[AllowAnonymous]`. The rules say deny by default. | Review against the security section | **Deferred.** The project has no authentication yet. Browsing pets should stay public, so add `[AllowAnonymous]` to this action when auth is added. |
| 7 | Low | Test output | `Failed to determine the https port for redirect` is logged in the API tests. | Reading the test logs | **No change.** It comes from `UseHttpsRedirection()` in the test host, which has no HTTPS port. It doesn't affect results, and the rules say to keep the middleware. |
| 8 | Low | `src/Animal.API/Contracts/PetSearchQuery.cs` | The `maxAge` limit of 100 is a choice, not a rule from the issue. | Review | **No change.** It's there to reject obviously wrong input. Noted in the PR description. |

**Checklist (section 6 of `copilot-instructions.md`)**

| Item | Status |
|---|---|
| 1. Build succeeds, no new warnings | Yes (13 old `CS8618` warnings in the models, none new) |
| 2. Tests pass, new tests for new behaviour | Yes, 35/35 |
| 3. EF migration if models changed | Not needed. No EF Core yet, and `PetSearchCriteria` isn't an entity |
| 4. Layer boundaries | Yes. Domain references nothing, the controller only maps and calls the service, DTOs in and out |
| 5. No secrets or real data | Yes. Seed and test pets are made up |
| 6. Validation, status codes, authorization | Validation and 200/400 yes. Authorization deferred (finding 6) |
| 7. Diagrams and README updated | Yes (finding 2) |
| 8. Branch and PR, CI green, teammate review | Branch and PR yes (#8), CI green. No review before the merge. Copilot reviewed it afterwards (findings 9 to 13) |
| 9. Only intended files in the diff | Yes. No `bin/`, `obj/`, `.vs/` or `*.user` files |

## Follow-up issues to open

- Make the service interfaces async with `CancellationToken` (finding 4).
- Add `[AllowAnonymous]` to `GET /api/pets` when authentication is added (finding 6).

## Fixing it after the merge

We asked Copilot (GitHub MCP) to clean up after the merge:

- PR #8 description updated with the text above.
- Issue #7 closed with ["Done in #8."](https://github.com/AidinYaseri/Team-5-Web-Programming/issues/7#issuecomment-6064400146)
- Follow-up issues created: [#9 Make service interfaces async with CancellationToken](https://github.com/AidinYaseri/Team-5-Web-Programming/issues/9) and [#10 Add [AllowAnonymous] to GET /api/pets when authentication is added](https://github.com/AidinYaseri/Team-5-Web-Programming/issues/10).

We also asked it to run `review-pr` on #8. Instead of reviewing the diff, it requested a Copilot code review job, found none, and just copied findings 1 to 8 back from the PR description. That isn't a review, so we asked again and told it to read the diff itself and only report new findings, numbered from 9.

## Copilot's review of #8 (findings 9 to 13)

It read every changed file under `src/` and `tests/` and found no new layer violations, secrets, stale docs or build output in the diff. Its new findings, with what we decided:

| # | Severity (Copilot) | File | Finding | Outcome |
|---|---|---|---|---|
| 9 | High | `src/Animal.Data/SeedData/PetSeedData.cs` | Says `=> [ new Pet {...}, ... ]` is "JavaScript-style" and won't compile. | **Wrong, no change.** That's a C# 12 collection expression. The project targets .NET 10, `dotnet build` has 0 errors and CI on #8 passed. |
| 10 | High | Both test files | Same claim for `Assert.Equal([1, 2, 3], ids)` and `new InMemoryPetService([ ... ])`. | **Wrong, no change.** Same reason. All 35 tests compile and pass. |
| 11 | Medium | `PetsEndpointTests.cs` | Use typed `new[] { ... }` so xUnit picks the right `Assert.Equal` overload. | **No change.** Follows from finding 10. The asserts already compare sequences, which is why the tests can fail (they did in the Step 7 mutation check). |
| 12 | Low | `PetSearchQuery.cs` | `AvailableOnly` is `bool`, so it can't tell "not sent" from "false". | **No change.** That's on purpose: issue #7 says default false, and `GetPets_AvailableOnlyFalse_EqualsNoFilter` tests it. |
| 13 | Low | `InMemoryPetService.cs` | Species is trimmed, not a bug, maybe document it. | **No change.** Copilot said itself it isn't a bug. The behaviour is in the issue and the PR description. |

So the review gave us nothing we had to fix. Its two "High" findings were false: it didn't know the newer C# syntax and didn't check the build or CI result before calling it a compile error. If we had applied its suggested patch, we would have rewritten working code across three files for nothing.
