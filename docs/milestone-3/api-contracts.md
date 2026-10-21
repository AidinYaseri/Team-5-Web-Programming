# API Contract Sheet (Milestone 3)

**Status: DRAFT.** The team reviews this before any slice starts (WI-1, task 5). Once it's agreed, backend and frontend can be built at the same time against it. To change a contract after that, update this file in a PR and tell whoever owns the client screen.

## Conventions (all slices)

| Topic | Rule |
|---|---|
| Base URL | `http://localhost:5015` (dev). All routes start with `/api/`. Plural resource names. |
| Format | JSON, camelCase. Enums are sent as strings (`"Pending"`, not `0`). Dates are ISO 8601 UTC (`2026-11-03T14:00:00Z`). |
| DTO names | `XxxResponse` for output, `CreateXxxRequest` / `UpdateXxxRequest` for bodies, `XxxQuery` for query strings. They live in `Animal.API/Contracts`. Entities are never returned. |
| Validation | Data annotations on request DTOs. A failure returns **400** `application/problem+json` with an `errors` map (automatic with `[ApiController]`). |
| Not found | The service throws `EntityNotFoundException` → **404** ProblemDetails. |
| Rule broken | The service throws `BusinessRuleException` → **409** ProblemDetails (e.g. adopting an unavailable pet). |
| Create | **201** with a `Location` header (`CreatedAtAction`) and the created `XxxResponse`. |
| Update | **200** with the updated `XxxResponse`. |
| Delete / actions with no body | **204**. |
| Auth | Bearer access token in `Authorization: Bearer <token>`. No token or a bad token → **401**. Valid token but missing scope → **403**. Scope names below are a proposal until the identity provider is picked (D2). |
| Current user | Comes from the token's `sub` claim only, never from the body or a header. "Mine" routes filter by it. |

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
| POST | `/api/pets` | `pets.write` | `CreatePetRequest` | 201 `PetResponse` | 400, 401, 403, 404 (shelter) |
| PUT | `/api/pets/{id}` | `pets.write` | `UpdatePetRequest` | 200 `PetResponse` | 400, 401, 403, 404 |
| DELETE | `/api/pets/{id}` | `pets.write` | — | 204 | 401, 403, 404, 409 (has an open application) |

Only `GET /api/pets` exists so far. The DTOs below are already in `Animal.API/Contracts`.

```jsonc
// PetSearchQuery: every field optional
{ "species": "dog",        // ≤ 50 chars, exact match, case-insensitive
  "maxAge": 5,             // 0–100, inclusive
  "availableOnly": true }

// PetResponse
{ "id": 1, "name": "Biscuit", "species": "Dog", "breed": "Beagle",
  "age": 3, "isAvailable": true, "shelterId": 1 }

// CreatePetRequest: new pets are always available
{ "name": "Biscuit",       // required, ≤ 50
  "species": "Dog",        // required, ≤ 50
  "breed": "Beagle",       // ≤ 50
  "age": 3,                // 0–100
  "shelterId": 1 }         // ≥ 1, must exist

// UpdatePetRequest: same as create, plus
{ ..., "isAvailable": false }
```

## Shelters (WI-7, Darcy)

| Method | Route | Auth | Request | Success | Errors |
|---|---|---|---|---|---|
| GET | `/api/shelters` | Public | — | 200 `ShelterResponse[]` | — |
| GET | `/api/shelters/{id}` | Public | — | 200 `ShelterResponse` | 404 |
| GET | `/api/shelters/{id}/pets` | Public | — | 200 `PetResponse[]` | 404 |
| POST | `/api/shelters` | `shelters.write` | `CreateShelterRequest` | 201 `ShelterResponse` | 400, 401, 403 |
| PUT | `/api/shelters/{id}` | `shelters.write` | `UpdateShelterRequest` | 200 `ShelterResponse` | 400, 401, 403, 404 |
| DELETE | `/api/shelters/{id}` | `shelters.write` | — | 204 | 401, 403, 404, 409 (still has pets) |

```jsonc
// ShelterResponse
{ "id": 1, "name": "Montreal Paws", "address": "123 Fake St", "phoneNumber": "514-555-0100", "email": "info@example.com" }

// CreateShelterRequest / UpdateShelterRequest
{ "name": "...",           // required, ≤ 100
  "address": "...",        // required, ≤ 200
  "phoneNumber": "...",    // [Phone], ≤ 20
  "email": "..." }         // [EmailAddress], ≤ 100
```

## Users (WI-8, Aiden)

Users are created on their first sign-in (or on the first `PUT /me`). The user is identified by the token's `sub` claim.

| Method | Route | Auth | Request | Success | Errors |
|---|---|---|---|---|---|
| GET | `/api/users/me` | `users.read` | — | 200 `UserResponse` | 401, 403, 404 (no profile yet) |
| PUT | `/api/users/me` | `users.write` | `UpdateUserRequest` | 200 `UserResponse` | 400, 401, 403, 409 (email taken) |

There is no "list all users" endpoint, so nobody can read other users' personal data.

```jsonc
// UserResponse
{ "id": 7, "name": "Sam Example", "email": "sam@example.com", "phoneNumber": "514-555-0199" }

// UpdateUserRequest
{ "name": "...",           // required, ≤ 100
  "email": "...",          // required, [EmailAddress], unique
  "phoneNumber": "..." }   // [Phone], ≤ 20
```

> Open point for WI-8: `User.Id` is an `int`, but `sub` is a string. Add an `ExternalId` (string, unique) column to `User` to link them.

## Adoptions (WI-9, Aiden)

| Method | Route | Auth | Request | Success | Errors |
|---|---|---|---|---|---|
| POST | `/api/adoptions` | `adoptions.write` | `CreateAdoptionRequest` | 201 `AdoptionResponse` | 400, 401, 403, 404 (pet), 409 (pet unavailable / already applied) |
| GET | `/api/adoptions/mine` | `adoptions.read` | — | 200 `AdoptionResponse[]` | 401, 403 |
| GET | `/api/adoptions/{id}` | `adoptions.read` (own) or `adoptions.review` | — | 200 `AdoptionResponse` | 401, 403, 404 |
| GET | `/api/adoptions?status=Pending` | `adoptions.review` | `AdoptionQuery` | 200 `AdoptionResponse[]` | 400, 401, 403 |
| POST | `/api/adoptions/{id}/approve` | `adoptions.review` | — | 204 | 401, 403, 404, 409 (not pending) |
| POST | `/api/adoptions/{id}/reject` | `adoptions.review` | — | 204 | 401, 403, 404, 409 (not pending) |

On approve: the pet becomes unavailable, other pending applications for that pet are rejected, and each applicant gets a notification.

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
| POST | `/api/appointments` | `appointments.write` | `CreateAppointmentRequest` | 201 `AppointmentResponse` | 400, 401, 403, 404 (pet/shelter), 409 (slot taken / in the past) |
| GET | `/api/appointments/mine` | `appointments.read` | — | 200 `AppointmentResponse[]` | 401, 403 |
| GET | `/api/appointments/{id}` | `appointments.read` (own) | — | 200 `AppointmentResponse` | 401, 403, 404 |
| POST | `/api/appointments/{id}/cancel` | `appointments.write` (own) | — | 204 | 401, 403, 404, 409 (already cancelled/completed) |

```jsonc
// CreateAppointmentRequest
{ "petId": 1, "shelterId": 1,
  "appointmentDate": "2026-11-03T14:00:00Z" }   // must be in the future

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
