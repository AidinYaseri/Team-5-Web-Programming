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

## Copilot's restatement

<!-- paste here -->

## Corrections we made

<!-- e.g. "It assumed an AnimalDbContext existed and wanted to add a LINQ query to it. Told it there is no DbContext yet." -->
