# Step 6: Implement with R-PIV

Owner: Darcy

The code for every file in the plan is in `src/`. The README and class diagram were updated too.

## How to run each step in Copilot

For each step in `implementation-plan.md`, in Agent mode:

```
Do step <N> of docs/milestone-2/implementation-plan.md only. Follow .github/copilot-instructions.md.
When you're done, run dotnet build and show me the diff. Don't start the next step.
```

Review the diff, fix anything wrong, then move on. Fill in one row per step below.

## Log

| Step | Research | Plan | Implement | Validate | Human correction |
|---|---|---|---|---|---|
| 1 Domain | Read `Pet.cs`, `IPetService.cs` | Add criteria class + 1 method | `PetSearchCriteria.cs`, `IPetService.cs` | `dotnet build` OK | None |
| 2 Data | No DbContext, empty `Animal.Data` | In-memory service, seed data, reuse `SearchPets` for `GetAvailablePets` | `PetSeedData.cs`, `InMemoryPetService.cs` | build OK, 19 unit tests pass | None |
| 3 Contracts | Rules: DTOs in `Contracts/`, validate input | Query DTO + response DTO | `PetSearchQuery.cs`, `PetResponse.cs` | build OK | None |
| 4 Controller | Rules: thin controller, `[ApiController]`, plural name | Map query to criteria, call service, map to DTO | `PetsController.cs` | build OK | None |
| 5 Wiring | Nothing registered yet | `AddSingleton` | `Program.cs` | **Failed manual check:** API returned `[]` | Changed to a factory registration, see below |
| 6 Tests | | | (Aiden) | 30/30 pass, 35/35 after Step 7 | See Step 7 |
| 7 Docs | `.http` still had weatherforecast, diagram missing `SearchPets` | Update both | `Animal.API.http`, README, `.mmd` | Diagram renders | PNG re-exported with mermaid-cli |

## The correction in step 5

`builder.Services.AddSingleton<IPetService, InMemoryPetService>()` compiled and every test passed, but `GET /api/pets` returned `[]` on the real app. DI chose the `InMemoryPetService(IEnumerable<Pet>)` constructor because it can always resolve `IEnumerable<T>` (as an empty list). The fix:

```csharp
// Factory on purpose: otherwise DI picks the IEnumerable<Pet> constructor and injects an empty list.
builder.Services.AddSingleton<IPetService>(_ => new InMemoryPetService());
```

Aiden added `PetsStartupTests` so this gets caught automatically next time.
