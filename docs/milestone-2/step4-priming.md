# Step 4: Prime the agent

Owner: Darcy

## How to run it

1. New Copilot Chat session in **Agent** mode with the GitHub MCP tools on.
2. Run the repo prompt file: type `/prime-issue` (VS Code) or `#prompt:prime-issue` (Visual Studio) and enter the issue number. The prompt lives in `.github/prompts/prime-issue.prompt.md`.
3. Let it read the issue and the files. Don't let it write code. If it starts editing, stop it and say "research only, no changes yet".
4. Answer its questions, then copy its restatement below.

## What a good restatement should contain

Compare Copilot's answer against this. Anything missing or wrong goes in the "Corrections" section.

**Files it should find**
- `src/Animal.Domain/Models/Pet.cs` (fields: Id, Name, Species, Breed, Age, IsAvailable, ShelterId)
- `src/Animal.Domain/Services/IPetService.cs` (has `GetPets`, `GetAvailablePets`, no search)
- `src/Animal.API/Program.cs` (only `AddControllers` and `AddOpenApi`, no services registered)
- `src/Animal.API/Controllers/` and `Contracts/` (empty)
- `src/Animal.Data/` (empty folders, no DbContext, no EF Core package)
- `.github/copilot-instructions.md`, README class diagram, `docs/diagrams/services-class-diagram.mmd`, `src/Animal.API/Animal.API.http`

**Problem in its own words**
The API has no way to list or filter pets. We need `GET /api/pets` with optional `species`, `maxAge` and `availableOnly` filters, combined with AND. Since there's no database yet, the filter logic needs an in-memory `IPetService` with fake data, and the controller has to stay thin and return DTOs.

**Questions it should ask (or that we answer up front)**
- Exact or partial species match? Exact, ignoring case.
- Is `maxAge` inclusive? Yes.
- Where does the implementation go if there's no DbContext? `Animal.Data/Services`, in memory, replaced later.
- Should `GetAvailablePets` be removed? No, keep it working.

## What we sent

New chat, Agent mode, GPT-5 mini, GitHub MCP on:

```
#prompt:prime-issue

Issue number: 7. Read the code from the main branch on GitHub using the MCP tools, not my local files, because my local branch already has work in progress. Research only, don't change any files.

Answers to questions you'll probably have: species is an exact match ignoring case and spaces; maxAge is inclusive; there is no DbContext yet, so the implementation is an in-memory IPetService in Animal.Data/Services with seed data; GetAvailablePets stays. If you have other questions, list them at the end.
```

We answered the usual questions up front so the priming didn't need a second round.

## Copilot's first answer (research)

<details>
<summary>Full output</summary>

```text
Research from main (read-only, GitHub files)

Files inspected
- src/Animal.Domain/Models/Pet.cs: domain entity exists (Id, Name, Species, Breed, Age, IsAvailable, ShelterId).
- src/Animal.Domain/Services/IPetService.cs: interface present with GetPet(int id), GetPets(), GetAvailablePets(), CreatePet(Pet pet), UpdatePet(Pet pet), DeletePet(int id). (No SearchPets or PetSearchCriteria on main.)
- src/Animal.Domain/Models/*: other domain models present (User, Shelter, Appointment, Notification, AdoptionApplication).
- src/Animal.API/Program.cs: minimal Web API host: AddControllers, AddOpenApi, MapControllers, UseAuthorization. No controller source files present.
- src/Animal.API/Animal.API.http: sample HTTP file (only weatherforecast example).
- src/Animal.Data/: project exists (Animal.Data.csproj) but no data implementation files / DbContext found.

Key findings
- IPetService currently does NOT include SearchPets or a PetSearchCriteria type on main.
- There is no PetsController in src/Animal.API on main.
- There is no implementation of IPetService in src/Animal.Data on main (no in-memory or EF implementation).
- No DbContext or EF Core mapping/configuration files found on main.
- Program.cs wires controllers and OpenAPI but no service registrations for IPetService.

What I did (MCP calls)
- Read directory listings and files under src/Animal.Domain/Models, src/Animal.Domain/Services, src/Animal.API (Program.cs, Animal.API.http), src/Animal.Data (csproj).
- Verified contents of IPetService.cs and Pet.cs.

Next steps I can take (no changes unless you ask)
- Create a short implementation plan and branch to add PetSearchCriteria, SearchPets, InMemoryPetService with seed data, PetsController and DTOs, unit and integration tests.
- Or implement those changes on a feature branch and open a PR.
Which would you like me to do next?
```

</details>

## How it compares

| Expected | Copilot | OK? |
|---|---|---|
| `Pet.cs` with its 7 fields | Found, fields correct | Yes |
| `IPetService` has `GetPets`, `GetAvailablePets`, no search | Found, listed all 6 methods, said no `SearchPets` on main | Yes |
| `Program.cs` has no services registered | Found | Yes |
| `Controllers/` and `Contracts/` empty | Said no controllers. Didn't mention `Contracts/` | Mostly |
| `Animal.Data` has no DbContext | Found | Yes |
| `Animal.API.http` only has the weatherforecast request | Found | Yes |
| Read issue #7 and the triage comment | Not mentioned in its list of MCP calls | No |
| Read `.github/copilot-instructions.md` | Not mentioned | No |
| README class diagram and `.mmd` | Not read | No |
| Restate the problem in its own words | Skipped. It listed findings and jumped to offering to implement | No |
| List open questions | None | No |

The file research was good and it read `main` like we asked this time. But it skipped steps 1, 2, 4 and 5 of the prompt file, and it offered to write code and open a PR even though the prompt says to stop and wait.

## Copilot's restatement

After the follow-up it read issue #7, `copilot-instructions.md` and the service diagram on `main`. Summary of what it said, in its own structure:

- **Behaviour:** `GET /api/pets` takes optional `species`, `maxAge` and `availableOnly`, returns pets matching all given filters, all pets if none given. 200 with DTOs sorted by id, 200 with `[]` if nothing matches, 400 ValidationProblem for bad input.
- **Inputs and validation:** species trimmed, case-insensitive exact match, blank means no filter, max 50. `maxAge` int 0 to 100, inclusive. `availableOnly` bool, default false. Unknown extra params are not an error.
- **Constraints:** `SearchPets(PetSearchCriteria)` on `IPetService`, criteria in `Animal.Domain/Models`, thin controller, DTOs in `Animal.API/Contracts`, in-memory `IPetService` with seed data in `Animal.Data`, keep `GetAvailablePets()`, unit and endpoint tests, update the diagram, README and `.http` file, register the service in `Program.cs`.
- **Out of scope:** paging, other sort options, fuzzy match, shelter or breed filters, EF, auth.

It matches the issue and what we expected in the section above. It also caught something we hadn't listed: `Program.cs` needs a DI registration.

**Its questions (12):** sync or async, the shape of `PetSearchCriteria`, DTO names, where validation happens, species normalisation (inner spaces, accents), what absent `availableOnly` means, sort direction, DI lifetime, test strategy, who regenerates the diagram PNG, whether registering in `Program.cs` on our branch is OK, and whether `SearchPets` with `availableOnly=true` should equal `GetAvailablePets()`.

The sync vs async question is a real conflict: the repo rules say async I/O, but `IPetService` is all sync.

## Our answers

| Question | Answer |
|---|---|
| Sync or async | Stay sync (`List<Pet> SearchPets(PetSearchCriteria criteria)`) to match the rest of `IPetService`. Making the interface async is its own follow-up issue. |
| `PetSearchCriteria` | Plain class in `Animal.Domain/Models`: `string? Species`, `int? MaxAge`, `bool AvailableOnly`. No other fields. |
| DTOs | `PetSearchQuery` (query binding, with the validation attributes) and `PetResponse`, both in `Animal.API/Contracts`. Don't bind to the domain type. |
| Validation | Data annotations on `PetSearchQuery` (`[StringLength(50)]`, `[Range(0, 100)]`). `[ApiController]` returns the ValidationProblem on its own. |
| Species normalisation | Only trim and ignore case. No inner space or accent handling. Blank after trim means no filter. |
| `availableOnly` absent | Same as false: available and adopted pets both come back. |
| Sort | Ascending by `Id`. Ids are unique so no tie breaker. |
| DI lifetime | Singleton, since the seed data lives in memory. |
| Tests | Unit tests for `InMemoryPetService` in `Animal.Data.Tests`. Endpoint tests with `WebApplicationFactory<Program>`, plus one test that uses the real `Program.cs` registration. |
| Diagram | Update the `.mmd`, the README copy and regenerate the PNG in the same change. |
| `Program.cs` | Yes, register it there on `Copilot_instructions`. |
| `GetAvailablePets` | Stays, and returns the same as `SearchPets` with `AvailableOnly = true`. |

## Corrections we made

- Told it to read `main` on GitHub instead of local files, because in step 2 it mixed them up.
- The first answer skipped the restatement and questions and offered to start coding, so we asked again with the missing steps spelled out.
- Answered its 12 questions (table above), mainly keeping `SearchPets` sync for now.
