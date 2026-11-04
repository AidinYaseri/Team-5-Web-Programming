# Milestone 3 GitHub Issues

Ready to paste into GitHub. There's one issue per work item. Each one has the five parts the spec asks for: title, description and user story, acceptance criteria, 3–6 tasks, and one assignee.

Suggested labels: `milestone-3`, plus one of `foundation`, `slice` or `docs`. Put all of them on one GitHub Project board with the columns **Todo → In Progress → In Review → Done**.

Once an issue exists, start every commit message with its number (e.g. `#14: add Shelter capacity rule`), and write `Closes #14` in the PR description.

| # | Title | Assignee | Phase | Depends on |
|---|---|---|---|---|
| WI-1 | Set up Milestone 3 branch, board and layered solution structure | Aiden | 1 | — |
| WI-2 | Add EF Core persistence with DbContext, migrations and repositories | Darcy | 2 | WI-1 |
| WI-3 | Secure the API with OAuth2/OIDC bearer tokens and scope policies | Aiden | 2 | WI-1 |
| WI-4 | Scaffold the frontend client with sign-in and API integration | Chloe | 2 | WI-1 |
| WI-5 | Create layer-specific Copilot agents and a testing skill | Darcy | 2 | WI-1 |
| WI-6 | Add full pet management (CRUD) with browse and edit screens | Darcy | 3 | WI-2, WI-3, WI-4 |
| WI-7 | Add shelter registration with capacity and delete rules | Darcy | 3 | WI-2, WI-3, WI-4 |
| WI-8 | Add user profiles linked to the signed-in identity | Aiden | 3 | WI-2, WI-3, WI-4 |
| WI-9 | Add adoption applications with approve/reject review | Aiden | 3 | WI-6, WI-8, WI-11 |
| WI-10 | Add shelter visit appointments with booking rules | Chloe | 3 | WI-6, WI-7 |
| WI-11 | Add user notifications with read tracking | Chloe | 3 | WI-2, WI-3, WI-4 |
| WI-12 | Write Milestone 3 documentation and open the milestone PR | Aiden | 4 | all |

---

## WI-1: Set up Milestone 3 branch, board and layered solution structure

**Assignee:** Aiden · **Labels:** `milestone-3`, `foundation`

### Description
Milestone 3 requires a clean `.Domain` / `.Data` / `.Api` layout, a tracked board and agreed API contracts before parallel work starts. Today the Domain project mixes entities with application service interfaces, and there are stray empty project folders at the repo root.

**User story:** As a team member, I want a clean starting structure and agreed API contracts so that I can build my slice in parallel without merge conflicts.

### Acceptance criteria
- [ ] Branch `feature/milestone-3-layered-service-development` exists on GitHub, created from `main`
- [ ] Domain has `Entities/`, `Enums/`, `Exceptions/`, `Services/` and no package or project references
- [ ] Application service interfaces live in `Animal.API/Services`
- [ ] `dotnet build` has 0 warnings and `dotnet test` passes
- [ ] `docs/milestone-3/api-contracts.md` is approved by all three members (a comment on this issue counts)
- [ ] Issues WI-1 to WI-12 are on the GitHub Project board with assignees

### Tasks
- [x] Create the milestone branch from `main` and push it
- [x] Delete the empty root `Animal.API/`, `Animal.Data/` and `Animal.Domain/` folders
- [x] Restructure Domain and move service interfaces to `Animal.API/Services` (async)
- [x] Draft the API contract sheet
- [ ] Create issues WI-1 to WI-12 and the GitHub Project board
- [ ] Get team sign-off on the contract sheet

---

## WI-2: Add EF Core persistence with DbContext, migrations and repositories

**Assignee:** Darcy · **Labels:** `milestone-3`, `foundation`

### Description
The API stores pets in an in-memory list, so all data is lost on restart and the other five entities can't be saved at all. The milestone requires EF Core in `Animal.Data` with a DbContext, Fluent API configurations, migrations and repositories.

**User story:** As a shelter owner, I want pets, shelters and applications saved in a database so that nothing is lost when the API restarts.

### Acceptance criteria
- [ ] EF Core packages are referenced only by `Animal.Data` (never by Domain)
- [ ] `AnimalDbContext` has a `DbSet` for all six entities, and each entity has its own `IEntityTypeConfiguration<T>`
- [ ] `dotnet ef database update` creates the schema with fake seed data
- [ ] Read methods use `AsNoTracking()`
- [ ] `GET /api/pets` returns seeded pets from the database, and `PetsStartupTests` still passes
- [ ] The CI "Verify EF Core migrations" step runs and passes instead of skipping
- [ ] No connection string with credentials is committed (LocalDB in `appsettings.Development.json` only)

### Tasks
- [ ] Add EF Core packages and `Context/AnimalDbContext.cs`
- [ ] Add one Fluent API configuration per entity in `Configurations/` (keys, lengths, relationships, enums stored as strings)
- [ ] Add a generic repository and an EF `PetRepository` implementing `IPetRepository`
- [ ] Create the `InitialCreate` migration and move `PetSeedData` into `HasData`
- [ ] Register the DbContext and repositories in `Program.cs`, and remove the in-memory registration
- [ ] Update the integration tests to use a test database (EF InMemory or SQLite)

---

## WI-3: Secure the API with OAuth2/OIDC bearer tokens and scope policies

**Assignee:** Aiden · **Labels:** `milestone-3`, `foundation`

### Description
Every endpoint is currently anonymous. The milestone requires the API to act as a resource server: it validates bearer tokens from an OAuth2/OIDC provider and enforces scope-based policies on each operation.

**User story:** As a shelter owner, I want only signed-in users with the right permissions to change data so that adopters can't edit pets or approve their own applications.

### Acceptance criteria
- [ ] JWT bearer authentication validates signature, issuer, audience and lifetime, with authority and audience read from configuration
- [ ] `UseAuthentication()` runs before `UseAuthorization()`
- [ ] There is one named policy per scope in the contract sheet, and every endpoint is either policy-protected or explicitly `[AllowAnonymous]`
- [ ] The current user is read from token claims only (`ICurrentUser`)
- [ ] Automated tests prove: no token → 401; invalid, expired, wrong-issuer and wrong-audience tokens → 401; valid token missing the scope → 403; right scope → 200 while a different scope fails; public endpoints work without a token
- [ ] No secrets are committed (user-secrets or environment variables only)
- [ ] `docs/milestone-3/auth.md` documents the flow, the roles, the validation settings and the endpoint → policy table

### Tasks
- [ ] Pick the identity provider and register the API and client app, defining the scopes
- [ ] Add `AddAuthentication().AddJwtBearer()` with config-driven authority and audience
- [ ] Add scope policies and the `ICurrentUser` service
- [ ] Add a test token helper (local signing key plus a `WebApplicationFactory` override)
- [ ] Write the auth verification tests
- [ ] Write `docs/milestone-3/auth.md`

---

## WI-4: Scaffold the frontend client with sign-in and API integration

**Assignee:** Chloe · **Labels:** `milestone-3`, `foundation`

### Description
The milestone requires a frontend client (`.Client`) that talks to the API only over HTTP. Every slice adds its screens to this shell, so it has to exist first.

**User story:** As an adopter, I want a website where I can browse pets and sign in so that I don't need Swagger or curl to use the service.

### Acceptance criteria
- [ ] `src/Animal.Client` runs locally, with run steps in the README
- [ ] Sign-in uses the authorization-code flow with PKCE, and no client secret is in the browser code
- [ ] A shared API client attaches the access token and shows readable messages for 400, 401, 403, 404, 409 and 500
- [ ] The API allows the client origin through CORS (configurable, dev origin only)
- [ ] The public "Browse pets" page lists pets from `GET /api/pets` with species, age and availability filters
- [ ] The client never references `Animal.Data`, `Animal.Domain` or the database

### Tasks
- [ ] Create the client project (React + Vite + TypeScript, or the team's chosen stack)
- [ ] Add PKCE sign-in and sign-out with the provider's library
- [ ] Add the typed API client with token handling and error mapping
- [ ] Add routing, the layout and the navbar, plus CORS in the API
- [ ] Build the "Browse pets" page end to end

---

## WI-5: Create layer-specific Copilot agents and a testing skill

**Assignee:** Darcy · **Labels:** `milestone-3`, `foundation`

### Description
The milestone requires custom agents in `.github/agents/` and skills in `.github/skills/`, so Copilot gets only the context and tool access each layer needs. Every slice issue has to be implemented with these agents using the R-PIV flow, so they're needed before Phase 3.

**User story:** As a developer, I want a Copilot agent for each layer so that AI-generated code keeps to our architecture without me repeating the rules in every prompt.

### Acceptance criteria
- [ ] Six agents exist, each with frontmatter (`description`, `name`, `tools`) and folder constraints: `domain-dev`, `data-persistence`, `api-service`, `frontend-dev`, `testing`, and `code-review` (read-only: `read`, `search`)
- [ ] `.github/skills/testing/SKILL.md` describes the xUnit, `WebApplicationFactory` and test-token patterns used in this repo
- [ ] Each agent is tried once on a real task, with the result noted in the R-PIV log
- [ ] `copilot-instructions.md` and the prompt files point to the agents
- [ ] Aiden reviews `api-service` and `code-review`, and Chloe reviews `frontend-dev` and `testing`

### Tasks
- [ ] Write `domain-dev.agent.md` and `data-persistence.agent.md`
- [ ] Write `api-service.agent.md` and `code-review.agent.md`
- [ ] Write `frontend-dev.agent.md` (once the frontend stack is decided) and `testing.agent.md`
- [ ] Write `.github/skills/testing/SKILL.md`
- [ ] Update `copilot-instructions.md` and the prompt files to reference the agents

---

## WI-6: Add full pet management (CRUD) with browse and edit screens

**Assignee:** Darcy · **Labels:** `milestone-3`, `slice`

### Description
Only `GET /api/pets` (search) exists. Shelters need to add, edit and remove pets, and adopters need a details page. The contracts are in `api-contracts.md` → Pets.

**User story:** As shelter staff, I want to add, update and remove pets so that the adoption list is always accurate.

### Acceptance criteria
- [ ] `GET /api/pets/{id}`, `POST`, `PUT` and `DELETE` work as described in the contract sheet (201/200/204/400/404/409)
- [ ] Reads are public, and writes require `pets.write`
- [ ] Domain rules: age 0–100, name and species required; a pet with an open adoption application can't be deleted (409)
- [ ] Creating a pet for a shelter that doesn't exist returns 404
- [ ] Client: pet details page, plus add and edit forms with validation messages (shown only to users with write access)
- [ ] Domain unit tests for the rules, and integration tests for every endpoint including 401 and 403

### Tasks
- [ ] Domain: add pet invariants and the delete rule to `PetRules`
- [ ] Data: add repository methods for "has open application" and "shelter exists"
- [ ] API: extend `PetService` (the request DTOs already exist)
- [ ] API: add the controller endpoints with `[Authorize(Policy = "pets.write")]` and OpenAPI attributes
- [ ] Client: details page and add/edit forms
- [ ] Tests: unit and integration tests, including auth cases

---

## WI-7: Add shelter registration with capacity and delete rules

**Assignee:** Darcy · **Labels:** `milestone-3`, `slice`

### Description
Shelters exist only as an entity, with no service, repository, endpoints or screens. The contracts are in `api-contracts.md` → Shelters.

**User story:** As shelter staff, I want to register and manage my shelter so that adopters can see where each pet is.

### Acceptance criteria
- [ ] All shelter endpoints in the contract sheet work, including `GET /api/shelters/{id}/pets`
- [ ] Reads are public, and writes require `shelters.write`
- [ ] Domain rules: name and address required, and valid email and phone formats; a shelter that still has pets can't be deleted (409)
- [ ] Client: shelter list, shelter details showing its pets, and add/edit forms
- [ ] Domain unit tests plus integration tests, including 401, 403, 404 and 409

### Tasks
- [ ] Domain: shelter rules and the delete rule
- [ ] Data: shelter repository
- [ ] API: shelter DTOs, `ShelterService` implementing `IShelterService`
- [ ] API: `SheltersController` with policies
- [ ] Client: shelter screens
- [ ] Tests

---

## WI-8: Add user profiles linked to the signed-in identity

**Assignee:** Aiden · **Labels:** `milestone-3`, `slice`

### Description
Adoptions and appointments belong to a user, but a `User` row has to be linked to the identity provider's `sub` claim. The contracts are in `api-contracts.md` → Users.

**User story:** As an adopter, I want to manage my own profile so that shelters can contact me about my applications.

### Acceptance criteria
- [ ] `User` has a unique `ExternalId` holding the token's `sub` (added with a migration)
- [ ] `GET` and `PUT /api/users/me` work. The profile is created on the first `PUT`, and `GET` returns 404 until then
- [ ] The user is always found from the token, never from a body or route id
- [ ] Email must be unique (409), and there's no endpoint that lists other users
- [ ] Client: "My profile" view and edit form
- [ ] Tests cover your own profile, a duplicate email, and 401/403

### Tasks
- [ ] Domain: add `ExternalId` and the email/phone rules
- [ ] Data: user configuration, migration and repository
- [ ] API: user DTOs and `UserService` using `ICurrentUser`
- [ ] API: `UsersController` with `/me` routes
- [ ] Client: profile screen
- [ ] Tests

---

## WI-9: Add adoption applications with approve/reject review

**Assignee:** Aiden · **Labels:** `milestone-3`, `slice`

### Description
This is the core workflow: adopters apply for a pet, and shelter staff approve or reject. It depends on Pets (WI-6), Users (WI-8) and Notifications (WI-11). The contracts are in `api-contracts.md` → Adoptions.

**User story:** As an adopter, I want to apply to adopt a pet and follow the status of my application so that I know whether I can take the pet home.

### Acceptance criteria
- [ ] Applying for an unavailable pet, or a second open application for the same pet, returns 409
- [ ] Approving sets the pet to unavailable, rejects the other pending applications for that pet, and notifies each applicant
- [ ] Only applications with `Pending` status can be approved or rejected (otherwise 409)
- [ ] Users see only their own applications. Listing all of them and approve/reject require `adoptions.review`
- [ ] Status is returned as a string (`"Pending"`)
- [ ] Client: apply button on the pet page, a "My applications" list, and a reviewer queue with approve/reject
- [ ] Unit tests for every rule, and integration tests for the full apply → approve flow, including 403 for non-reviewers

### Tasks
- [ ] Domain: `AdoptionRules` (can apply, can approve/reject, side effects)
- [ ] Data: adoption configuration and repository
- [ ] API: adoption DTOs, and `AdoptionService` using the pet repository and `INotificationService`
- [ ] API: `AdoptionsController` with policies
- [ ] Client: apply flow, "My applications" and the reviewer queue
- [ ] Tests

---

## WI-10: Add shelter visit appointments with booking rules

**Assignee:** Chloe · **Labels:** `milestone-3`, `slice`

### Description
Adopters book a visit to meet a pet at its shelter. The contracts are in `api-contracts.md` → Appointments.

**User story:** As an adopter, I want to book a visit to meet a pet so that I can decide before applying.

### Acceptance criteria
- [ ] Booking in the past, or into a slot already taken at that shelter, returns 409
- [ ] Only the owner can view or cancel an appointment. Cancelling one that's already cancelled or completed returns 409
- [ ] Booking and cancelling create a notification
- [ ] Status is returned as a string (`"Scheduled"`)
- [ ] Client: "Book a visit" form on the pet page, plus a "My appointments" list with cancel
- [ ] Unit tests for the date and double-booking rules, and integration tests, including 401 and 403

### Tasks
- [ ] Domain: `AppointmentRules`
- [ ] Data: appointment configuration and repository
- [ ] API: appointment DTOs and `AppointmentService`
- [ ] API: `AppointmentsController` with policies
- [ ] Client: booking and "My appointments" screens
- [ ] Tests

---

## WI-11: Add user notifications with read tracking

**Assignee:** Chloe · **Labels:** `milestone-3`, `slice`

### Description
Adoptions and appointments need to tell users when something changes. `INotificationService` is used by other services, so build it early. The contracts are in `api-contracts.md` → Notifications.

**User story:** As an adopter, I want to see notifications about my applications and visits so that I don't miss a decision.

### Acceptance criteria
- [ ] `INotificationService.CreateAsync` is available for other services to call. There is no public create endpoint
- [ ] `GET /api/notifications?unreadOnly=` returns only the current user's notifications, newest first
- [ ] `POST /api/notifications/{id}/read` returns 204 and can safely be called twice. Marking someone else's notification gives 404
- [ ] Client: notification bell with an unread count, plus a list with "mark as read"
- [ ] Tests cover only-my-notifications, calling mark-as-read twice, and 401/403

### Tasks
- [ ] Domain: notification rules (message required, length limit)
- [ ] Data: notification configuration and repository
- [ ] API: notification DTOs and `NotificationService`
- [ ] API: `NotificationsController` with policies
- [ ] Client: notification bell and list
- [ ] Tests

---

## WI-12: Write Milestone 3 documentation and open the milestone PR

**Assignee:** Aiden · **Labels:** `milestone-3`, `docs`

### Description
The milestone is submitted as a milestone-level PR from `feature/milestone-3-layered-service-development` into `main`, plus a 2–3 page architecture and reflection document under `docs/`.

**User story:** As the team, we want our design and process documented so that the teacher and future teammates can see how the system was built and how AI was used.

### Acceptance criteria
- [ ] README and diagrams show the Client layer and the auth flow
- [ ] `docs/milestone-3/architecture-reflection.md` (2–3 pages) covers: the layering and frontend choice, the work split, where agents helped or needed fixing, and what the human gates caught. Each member writes a section
- [ ] The code-review agent has been run on the full milestone diff, and its critical findings are resolved
- [ ] The milestone PR lists the work items, milestone number and title, change summary, validation results and human sign-off
- [ ] At least one peer approves the PR. It's merged, the branch is kept, and the link is submitted on Léa

### Tasks
- [ ] Update the README and diagrams
- [ ] Write the reflection, with a section from each member
- [ ] Collect evidence (board screenshot, PR links, test results) in `docs/milestone-3/`
- [ ] Run the code-review agent and fix the critical findings
- [ ] Open the milestone PR, get approval, merge, and submit on Léa
