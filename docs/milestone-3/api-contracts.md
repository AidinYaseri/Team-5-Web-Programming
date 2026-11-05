# API Contract Sheet (Milestone 3)

**Status: DRAFT v2, waiting for sign-off on #12.** The team reviews this before any slice starts (WI-1). Once it's agreed, backend and frontend can be built at the same time against it. To change a contract after that, update this file in a PR and tell whoever owns the client screen.

v2 matches the code on `feature/milestone-3-layered-service-development` (commit `bcfb7fe`). What changed from v1:
- Error codes now match `DomainExceptionHandler`: a broken business rule is **400**, an invalid status change or a delete blocked by other records is **409**. `BusinessRuleException` was replaced by `DomainValidationException` and `InvalidStatusTransitionException`.
- `PUT` returns **204** with no body, like the Pets code does.
- Pet field lengths and the unknown-shelter error (400) match `CreatePetRequest` and `PetRules`. All five Pets endpoints now exist.
- Shelter and user field lengths match the EF Core configurations.
- Added the error body shapes and the enum-as-string setup that Adoptions and Appointments need.

## Open decisions (answer on #12)

1. **Update response:** keep `PUT` → 204 with no body (current code), or switch every slice to 200 with the updated `XxxResponse`?
2. **Business rule errors:** keep 400 for every broken rule (current code), or use 409 for rules about the current state, like "pet is not available" or "slot already taken"? If 409, `DomainExceptionHandler` needs a separate exception type for those.
3. **Scopes:** do the scope names below work with the identity provider we pick? Some providers want a prefix (for example `api://animal/pets.write`).

## Conventions (all slices)

| Topic | Rule |
|---|---|
| Base URL | `http://localhost:5015` (dev). All routes start with `/api/`. Plural resource names. |
| Format | JSON, camelCase. Enums are sent as strings (`"Pending"`, not `0`). Dates are ISO 8601 UTC (`2026-11-03T14:00:00Z`). |
| DTO names | `XxxResponse` for output, `CreateXxxRequest` / `UpdateXxxRequest` for bodies, `XxxQuery` for query strings. They live in `Animal.API/Contracts`. Entities are never returned. |
| Validation | Data annotations on request DTOs. A failure returns **400** `application/problem+json` with an `errors` map (automatic with `[ApiController]`). |
| Not found | The service throws `EntityNotFoundException` → **404** ProblemDetails. |
| Rule broken | The service throws `DomainValidationException` → **400** ProblemDetails with an `errors` list (e.g. age out of range, pet not available). |
| Wrong status | The service throws `InvalidStatusTransitionException` → **409** ProblemDetails (e.g. approving an application that isn't Pending). |
| Blocked by other records | A Restrict foreign key stops the save → **409** ProblemDetails, without the database message (e.g. deleting a shelter that still has pets). |
| Create | **201** with a `Location` header (`CreatedAtAction`) and the created `XxxResponse`. |
| Update | **204**, no body. See open decision 1. |
| Delete / actions with no body | **204**. |
| Enums in JSON | Sent as strings. `Program.cs` doesn't do this yet: the first slice with an enum in a DTO (Adoptions or Appointments) adds `JsonStringEnumConverter` in `AddControllers().AddJsonOptions(...)`. |
| Auth | Bearer access token in `Authorization: Bearer <token>`. No token or a bad token → **401**. Valid token but missing scope → **403**. Scope names below are a proposal until the identity provider is picked (D2). |
| Current user | Comes from the token's `sub` claim only, never from the body or a header. "Mine" routes filter by it. |

### Error body shapes

The client (`PetsApiClient.ReadProblemErrors`) already reads all three, so keep them as they are.

```jsonc
// 400 from [ApiController] model validation: errors is a map of field -> messages
{ "title": "One or more validation errors occurred.", "status": 400,
  "errors": { "Name": ["The Name field is required."] } }

// 400 from a domain rule: errors is a list
{ "title": "One or more business rules were broken.", "status": 400,
  "errors": ["Shelter 99 does not exist."] }

// 404 / 409: detail (when safe to show) and title
{ "title": "The request conflicts with the current status.", "status": 409,
  "detail": "Application 3 is Approved and can't be rejected." }
```

## Scopes

| Scope | Grants |
|---|---|
| `pets.write` | Create, update, delete pets |
| `shelters.write` | Create, update, delete shelters |
| `users.read` / `users.write` | Read / edit your own profile |
| `adoptions.read` / `adoptions.write` | See / submit your own applications |
| `adoptions.review` | Shelter staff: see every application, approve or reject |
| `appointments.read` / `appointments.write` | See / book or cancel your own appointments |
| `notifications.read` | Read your notifications and mark them as read |

Reading pets and shelters is **public** on purpose: adopters should be able to browse before signing in.

---

## Pets (WI-6, Darcy)

| Method | Route | Auth | Request | Success | Errors |
|---|---|---|---|---|---|
| GET | `/api/pets` | Public | `PetSearchQuery` (query) | 200 `PetResponse[]` | 400 |
| GET | `/api/pets/{id}` | Public | — | 200 `PetResponse` | 404 |
| POST | `/api/pets` | `pets.write` | `CreatePetRequest` | 201 `PetResponse` | 400, 401, 403, 404 (shelter doesn't exist, per WI-6) |
| PUT | `/api/pets/{id}` | `pets.write` | `UpdatePetRequest` | 204 | 400, 401, 403, 404 |
| DELETE | `/api/pets/{id}` | `pets.write` | — | 204 | 401, 403, 404, 409 (has applications or appointments) |

All five endpoints exist and are tested, but none are protected yet (401/403 come with WI-3). The DTOs below are in `Animal.API/Contracts`.

```jsonc
// PetSearchQuery: every field optional
{ "species": "dog",        // ≤ 50 chars, exact match, case-insensitive
  "maxAge": 5,             // 0–100, inclusive
  "availableOnly": true }

// PetResponse
{ "id": 1, "name": "Biscuit", "species": "Dog", "breed": "Beagle",
  "age": 3, "isAvailable": true, "shelterId": 1 }

// CreatePetRequest
{ "name": "Biscuit",       // required, ≤ 100
  "species": "Dog",        // required, ≤ 50
  "breed": "Beagle",       // optional, ≤ 100 (stored as "" when left out)
  "age": 3,                // 0–100
  "isAvailable": true,     // optional, defaults to true
  "shelterId": 1 }         // ≥ 1, must exist (checked by PetService → 404, per WI-6)

// UpdatePetRequest: same fields, replaces all of them. isAvailable has no default here.
```

## Shelters (WI-7, Darcy)

| Method | Route | Auth | Request | Success | Errors |
|---|---|---|---|---|---|
| GET | `/api/shelters` | Public | — | 200 `ShelterResponse[]` | — |
| GET | `/api/shelters/{id}` | Public | — | 200 `ShelterResponse` | 404 |
| GET | `/api/shelters/{id}/pets` | Public | — | 200 `PetResponse[]` | 404 |
| POST | `/api/shelters` | `shelters.write` | `CreateShelterRequest` | 201 `ShelterResponse` | 400, 401, 403 |
| PUT | `/api/shelters/{id}` | `shelters.write` | `UpdateShelterRequest` | 204 | 400, 401, 403, 404 |
| DELETE | `/api/shelters/{id}` | `shelters.write` | — | 204 | 401, 403, 404, 409 (still has pets) |

```jsonc
// ShelterResponse
{ "id": 1, "name": "Montreal Paws", "address": "123 Fake St", "phoneNumber": "514-555-0100", "email": "info@example.com" }

// CreateShelterRequest / UpdateShelterRequest
{ "name": "...",           // required, ≤ 100
  "address": "...",        // required, ≤ 200
  "phoneNumber": "...",    // required, [Phone], ≤ 20
  "email": "..." }         // required, [EmailAddress], ≤ 254
```

The `Shelters` table and `IShelterRepository.ExistsAsync` already exist (seeded with 2 fake shelters).

## Users (WI-8, Aiden)

Users are created on their first sign-in (or on the first `PUT /me`). The user is identified by the token's `sub` claim.

| Method | Route | Auth | Request | Success | Errors |
|---|---|---|---|---|---|
| GET | `/api/users/me` | `users.read` | — | 200 `UserResponse` | 401, 403, 404 (no profile yet) |
| PUT | `/api/users/me` | `users.write` | `UpdateUserRequest` | 204 | 400, 401, 403, 409 (email taken) |

There is no "list all users" endpoint, so nobody can read other users' personal data.

```jsonc
// UserResponse
{ "id": 7, "name": "Sam Example", "email": "sam@example.com", "phoneNumber": "514-555-0199" }

// UpdateUserRequest
{ "name": "...",           // required, ≤ 100
  "email": "...",          // required, [EmailAddress], ≤ 254, unique (index in UserConfiguration)
  "phoneNumber": "..." }   // required, [Phone], ≤ 20
```

The provider's opaque `sub` is stored in the unique `User.ExternalId` column (migration `AddUserExternalId`, WI-8). `SubjectToUserIdClaimsTransformation` looks it up on each request and adds the local `Users.Id` as a `local_user_id` claim, so services that work on the caller's own rows (appointments, notifications, adoptions) use `ICurrentUser.UserId`. Those policies also require that mapping, so a signed-in user without a profile gets 403 until they `PUT /api/users/me`.

## Adoptions (WI-9, Aiden)

| Method | Route | Auth | Request | Success | Errors |
|---|---|---|---|---|---|
| POST | `/api/adoptions` | `adoptions.write` | `CreateAdoptionRequest` | 201 `AdoptionResponse` | 400, 401, 403, 404 (pet), 409 (pet not available, or you already have a pending application for it; per WI-9) |
| GET | `/api/adoptions/mine` | `adoptions.read` | — | 200 `AdoptionResponse[]` | 401, 403 |
| GET | `/api/adoptions/{id}` | `adoptions.read` (own) or `adoptions.review` | — | 200 `AdoptionResponse` | 401, 403, 404 |
| GET | `/api/adoptions?status=Pending` | `adoptions.review` | `AdoptionQuery` | 200 `AdoptionResponse[]` | 400, 401, 403 |
| POST | `/api/adoptions/{id}/approve` | `adoptions.review` | — | 204 | 401, 403, 404, 409 (not pending) |
| POST | `/api/adoptions/{id}/reject` | `adoptions.review` | — | 204 | 401, 403, 404, 409 (not pending) |

On approve: the pet becomes unavailable, other pending applications for that pet are rejected, and each applicant gets a notification, all saved in one `SaveChangesAsync`. Applying and rejecting also notify the applicant. Someone else's application reads as 404 for adopters; reviewers (`adoptions.review`) can read any.

```jsonc
// CreateAdoptionRequest: userId comes from the token, not the body
{ "petId": 1 }

// AdoptionResponse
{ "id": 3, "petId": 1, "petName": "Biscuit", "userId": 7,
  "status": "Pending",     // Pending | Approved | Rejected
  "applicationDate": "2026-10-21T15:00:00Z" }

// AdoptionQuery
{ "status": "Pending" }    // optional
```

## Appointments (WI-10, Chloe)

| Method | Route | Auth | Request | Success | Errors |
|---|---|---|---|---|---|
| POST | `/api/appointments` | `appointments.write` | `CreateAppointmentRequest` | 201 `AppointmentResponse` | 400, 401, 403, 404 (pet/shelter), 409 (date not in the future or slot taken, per WI-10) |
| GET | `/api/appointments/mine` | `appointments.read` | — | 200 `AppointmentResponse[]` | 401, 403 |
| GET | `/api/appointments/{id}` | `appointments.read` (own) | — | 200 `AppointmentResponse` | 401, 403, 404 |
| POST | `/api/appointments/{id}/cancel` | `appointments.write` (own) | — | 204 | 401, 403, 404, 409 (already cancelled/completed) |

```jsonc
// CreateAppointmentRequest
{ "petId": 1, "shelterId": 1,
  "appointmentDate": "2026-11-03T14:00:00Z" }   // must be in the future (AppointmentRules.EnsureCanSchedule)

// AppointmentResponse
{ "id": 4, "petId": 1, "petName": "Biscuit", "shelterId": 1, "userId": 7,
  "appointmentDate": "2026-11-03T14:00:00Z",
  "status": "Scheduled" }  // Scheduled | Completed | Cancelled
```

## Notifications (WI-11, Chloe)

Notifications are created **inside** the API by `IAdoptionService` and `IAppointmentService` (through `INotificationService`). There is no public create endpoint.

| Method | Route | Auth | Request | Success | Errors |
|---|---|---|---|---|---|
| GET | `/api/notifications?unreadOnly=true` | `notifications.read` | `NotificationQuery` | 200 `NotificationResponse[]` (own, newest first) | 401, 403 |
| POST | `/api/notifications/{id}/read` | `notifications.read` (own) | — | 204 (calling it twice is fine) | 401, 403, 404 |

```jsonc
// NotificationResponse
{ "id": 9, "message": "Your application for Biscuit was approved.",
  "date": "2026-10-22T09:00:00Z", "isRead": false }
```

## Public vs protected summary

| Public (no token) | Everything else |
|---|---|
| `GET /api/pets`, `GET /api/pets/{id}`, `GET /api/shelters`, `GET /api/shelters/{id}`, `GET /api/shelters/{id}/pets` | Needs a bearer token with the scope listed in its table |

The auth integration tests (WI-3) must cover at least one public endpoint and one protected endpoint per scope.

## Sign-off

Comment on #12 with "approved" or the changes you want, and answer the open decisions.

| Member | Signed off |
|---|---|
| Aiden | ☐ |
| Darcy | ☐ |
| Chloe | ☐ |
