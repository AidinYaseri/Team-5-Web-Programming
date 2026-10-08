# Implementation plan: Filter pets by species, age and availability

Issue: #7
Owner: Darcy
Generated with: `.github/prompts/create-plan.prompt.md`, in a **fresh** Copilot session (not the priming one), then reviewed and edited.

## 1. Summary

Add an optional filter to the pet list. `IPetService` gets a `SearchPets` method that takes a `PetSearchCriteria` (species, max age, available only). An in-memory implementation with fake seed data stands in until EF Core exists. A new `PetsController` exposes `GET /api/pets`, validates the query string through a DTO and returns `PetResponse` DTOs. Tests cover the filter logic and the endpoint.

## 2. Files to touch

| File | Project | Change | Why |
|---|---|---|---|
| `Models/PetSearchCriteria.cs` | Domain | New | Holds the optional filters |
| `Services/IPetService.cs` | Domain | Edit | Add `List<Pet> SearchPets(PetSearchCriteria criteria)` |
| `SeedData/PetSeedData.cs` | Data | New | 5 fake pets |
| `Services/InMemoryPetService.cs` | Data | New | Implements all of `IPetService`, filter logic lives here |
| `Contracts/PetSearchQuery.cs` | API | New | Query DTO with validation (`[StringLength(50)]`, `[Range(0, 100)]`) |
| `Contracts/PetResponse.cs` | API | New | Response DTO, so the entity isn't returned |
| `Controllers/PetsController.cs` | API | New | `GET /api/pets` |
| `Program.cs` | API | Edit | Register `IPetService` |
| `Animal.API.http` | API | Edit | Replace the weatherforecast request with pet requests |
| `tests/Animal.Data.Tests/*` | tests | New | Unit tests for `SearchPets` (Aiden) |
| `tests/Animal.API.Tests/*` | tests | New | Endpoint tests (Aiden) |
| `Team-5-Web-Programming.slnx` | root | Edit | Add the test projects (Aiden) |
| `README.md`, `docs/diagrams/services-class-diagram.mmd` / `.png` | docs | Edit | Add `SearchPets` to the class diagram |

## 3. Order of changes

1. **Domain:** add `PetSearchCriteria` and the `SearchPets` method on `IPetService`.
2. **Data:** add `PetSeedData`, then `InMemoryPetService` with `SearchPets`. Make `GetAvailablePets` call `SearchPets` so the logic isn't written twice.
3. **API contracts:** add `PetSearchQuery` and `PetResponse`.
4. **API controller:** add `PetsController.GetPets`, which maps the query to criteria, calls the service and maps the result to DTOs.
5. **Wiring:** register the service in `Program.cs`.
6. **Tests:** service unit tests, then endpoint tests (done with Aiden in Step 7).
7. **Docs:** `.http` file, class diagram `.mmd`, README block, PNG export.

## 4. Verification

| Step | Check |
|---|---|
| 1 | `dotnet build` passes (Domain compiles, nothing implements the interface yet) |
| 2 | `dotnet build`. Unit tests for `SearchPets` pass |
| 3, 4 | `dotnet build`. No logic in the controller besides mapping |
| 5 | `dotnet run --project src/Animal.API`, then send the requests in `Animal.API.http`. `GET /api/pets` must return the 5 seed pets, not `[]` |
| 6 | `dotnet test`: everything green, no new warnings |
| 7 | README diagram renders on GitHub and shows `SearchPets` |

## 5. Risks and open questions

- Adding a method to `IPetService` is a contract change. Nobody implements it yet, so it's fine now, but the future EF Core service will need `SearchPets` too.
- `InMemoryPetService` is temporary. Keep it simple and don't build features on top of it.
- Repo rules want async methods with `CancellationToken`, but all existing interfaces are sync. **Decision:** stay sync to match the other interfaces and open a follow-up issue.
- Species: exact match, ignores case. Partial search is out of scope.

## What we changed from Copilot's plan

Copilot's raw plan is saved as [implementation-plan-copilot.md](implementation-plan-copilot.md). We gave it the team's decisions from step 4 in the prompt, so the file list and shapes were already right. What we still had to fix:

| Copilot's plan | What we changed | Why |
|---|---|---|
| No Verification section, even though the prompt file asks for one | Added the Verification table above | Each step needs a check we can run before moving on. |
| Step 6 says the controller returns DTOs "sorted by Id ascending" | Sorting stays in `InMemoryPetService.SearchPets` | Sorting in the controller is logic in the controller. The service already returns them in order. |
| Seed data inside `InMemoryPetService` | Separate `PetSeedData` class | Tests can build the service with their own pets. |
| `GetAvailablePets` implemented on its own | `GetAvailablePets` calls `SearchPets` with `AvailableOnly = true` | Same logic written once, and it's what we told it in step 4. |
| Step 7 does DI, the `.http` file, the diagram and the README all at once | Split into Wiring and Docs | Small steps are easier to check. |
| All tests at the very end | Unit tests right after the Data step, endpoint tests after the controller | Matches R-PIV, each step gets validated. |
| Risks didn't mention DI picking the wrong constructor | Added a manual check in Verification step 5 (`GET /api/pets` must not return `[]`) | That's the bug we actually hit, see step 6. |
| "Step 1" was saving the plan, and it marked it done and offered to start coding right away | Ignored, we stopped after the plan | The prompt file says stop so the plan can be reviewed. |

What we kept: the file list, the Domain to Data to API order, singleton registration, the risk about changing `IPetService`, and the open question about who regenerates the PNG (answer: we do, with mermaid-cli, in the same commit).
