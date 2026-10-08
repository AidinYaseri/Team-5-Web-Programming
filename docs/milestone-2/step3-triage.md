# Step 3: Triage and search

Owner: Chloe

## Prompt for Copilot (Agent mode, GitHub MCP on)

```
Before we start on the pet filter issue, check AidinYaseri/Team-5-Web-Programming for
anything related or conflicting:
1. Search all issues (open and closed) for pet, filter, search, species, age.
2. List all pull requests and say if any touch pets, IPetService or controllers.
3. Search the code for IPetService, GetAvailablePets, PetsController and any existing
   IPetService implementation.
4. Tell me if this issue duplicates or conflicts with anything, and what files it will touch.
```

## Results (checked on 2026-10-07)

| Search | Result |
|---|---|
| Issues, open and closed | None. The repo had 0 issues, so this will be the first one. |
| Pull requests | 6, all closed: #1 CI workflow, #2 to #4 file structure, #5 diagrams in README, #6 repo organisation and service diagram. None touch pet logic. |
| `IPetService` | Only `src/Animal.Domain/Services/IPetService.cs` plus the README and `docs/diagrams/services-class-diagram.mmd`. No implementation exists. |
| `GetAvailablePets` | Declared on `IPetService` only. No callers. |
| `PetsController` | Doesn't exist. `src/Animal.API/Controllers/` is empty, but the service diagram in the README already plans a `PetsController`. |
| DbContext / repositories | Don't exist. The `Animal.Data` folders are empty. |
| Branches | `main`, `Copilot_instructions`, plus old branches from closed PRs. Only `Copilot_instructions` has newer work (repo AI guidance), and it doesn't touch pet code. |

## Conclusion

- **No duplicate.** There are no issues at all and no PR or branch has pet filtering.
- **No conflict**, but two things to watch:
  1. Whoever builds the EF Core layer will have to replace `InMemoryPetService` with a real implementation that also has `SearchPets`. Mention this in the issue so they know.
  2. Adding a method to `IPetService` is a contract change. Nobody implements the interface yet, so nothing breaks today.
- The `PetsController` we add matches the name already planned in the service diagram, so the diagram stays correct.

<!-- Paste a screenshot of Copilot's MCP search output here as evidence. -->
