# Step 3: Triage and search

Owner: Chloe

## Prompt for Copilot (Agent mode, GitHub MCP on)

```
Triage issue #7 in AidinYaseri/Team-5-Web-Programming using the GitHub MCP tools. Use the main branch on GitHub, not my local files.
1. Search all issues (open and closed) for pet, filter, search, species, age. Ignore #7 itself.
2. List all pull requests and say if any touch pets, IPetService or controllers.
3. On main, look for IPetService, GetAvailablePets, PetsController and any existing IPetService implementation or DbContext.
4. List the branches and say if any of them already work on pet filtering.
5. Tell me if #7 duplicates or conflicts with anything, what files it will touch, and any risks.
Don't change anything on GitHub yet.
```

We told it to use `main` on GitHub because in step 2 it read our local branch and thought `SearchPets` already existed.

## What Copilot found (2026-10-08)

| Search | Copilot's result |
|---|---|
| Issues, open and closed | Only #7. Nothing else matches pet, filter, search, species or age. |
| Pull requests | #1 to #6, all closed. #1 CI workflow, #2 to #4 Chloe's file structure, #5 diagrams in README, #6 repo organisation and service diagram. None touch pet filtering or controllers. |
| `IPetService` on main | `src/Animal.Domain/Services/IPetService.cs` with `GetPets()` and `GetAvailablePets()`. |
| `GetAvailablePets` | Declared on `IPetService` only. |
| `PetsController` | Not on main. |
| `IPetService` implementation, DbContext | None on main. |
| Branches | `AidinYaseri-patch-1`, `Copilot_instructions`, `adding-diagrams`, `chloe-file-structure`, `main`, `organize-repo`. Copilot said none of them work on pet filtering. |

Files it expects the feature to touch: `PetSearchCriteria.cs` (new), `IPetService.cs`, `InMemoryPetService.cs` (new), a response DTO, `PetsController.cs` (new), `Program.cs` for DI, both test projects, the class diagram files and `Animal.API.http`.

Risks it listed: adding `SearchPets` changes the interface, the in-memory service has to be registered in `Program.cs` and later swapped for EF without changing behaviour, keep the filter out of the controller, don't return entities, test the 400 cases, and check nobody else changes `IPetService` before merging.

## What we checked ourselves

| Copilot said | Checked | Result |
|---|---|---|
| Not sure if `SearchPets` is on main. It mixed up our local copy and main and told us to double check. | `git show origin/main:src/Animal.Domain/Services/IPetService.cs` | Not on main. The interface has `GetPet`, `GetPets`, `GetAvailablePets`, `CreatePet`, `UpdatePet`, `DeletePet`. |
| No branch works on pet filtering | `git log origin/Copilot_instructions` | Wrong. `Copilot_instructions` is our milestone branch and already has the filter work. Copilot only judged the branches by name. It isn't a duplicate, it's the branch this issue gets done on. |
| DTO file `PetResponseDto.cs` | Our repo rules | Just a naming guess. We call it `PetResponse` and add `PetSearchQuery` for the query params. |

## Conclusion

- **No duplicate.** #7 is the only issue about pets and no PR has pet code.
- **No conflict on main.** No controller, no implementation, no DbContext. Adding `SearchPets` is a contract change but nothing implements `IPetService` yet, so nothing breaks.
- **Later risk:** whoever builds the EF Core layer has to replace `InMemoryPetService` and keep the same `SearchPets` behaviour.
- `PetsController` matches the name already planned in the service diagram.
- Copilot was useful for the GitHub side (issues, PRs, branches) but it got confused between local files and main, and it checked branches by name only. We had to verify those two things with git.

Triage comment posted on the issue by Copilot (GitHub MCP `add_issue_comment`): https://github.com/AidinYaseri/Team-5-Web-Programming/issues/7#issuecomment-6063907578
