# API Contract Sheet (Milestone 4)

**Status: proposed, waiting for sign-off on the WI-1 issue.** This sheet only lists what Milestone 4 adds or changes. Everything else (conventions, error body shapes, the other endpoints) stays as in [the Milestone 3 sheet](../milestone-3/api-contracts.md). The reasons behind each choice are in [decisions.md](decisions.md).

## What changes

| Change | Where | Work item |
|---|---|---|
| New picture endpoints under `/api/users/me/picture` | API | WI-5 |
| `UserResponse` gets `hasPicture` | API and the client's copy | WI-5, WI-6 |
| `pets.write`, `shelters.write` and `adoptions.review` also need the `staff` role | API policies | WI-2 |
| The client's `GET /auth/access-token` route is removed | Client | WI-3 |

## Profile picture (WI-4, WI-5 Darcy · WI-6 Chloe)

The user always comes from the token's `sub`. There is no user id in any of these routes, so nobody can reach another user's picture by changing an id.

| Method | Route | Auth | Request | Success | Errors |
|---|---|---|---|---|---|
| PUT | `/api/users/me/picture` | `users.write` | `multipart/form-data`, one file in the field `file` | 204 | 400 (rules), 401, 403, 404 (no profile yet), 413 (request over 3 MB) |
| GET | `/api/users/me/picture` | `users.read` | none | 200, the image bytes | 401, 403, 404 (no profile or no picture) |
| DELETE | `/api/users/me/picture` | `users.write` | none | 204 (also when there was no picture) | 401, 403, 404 (no profile yet) |

### Upload rules (`PUT`)

| Check | Result when broken |
|---|---|
| The `file` field is there and not empty | 400 |
| File is at most 2 MB (2,097,152 bytes) | 400 |
| The whole request is at most 3 MB | 413, before the body is read |
| The first bytes are a PNG or JPEG signature | 400 |
| The part's content type (`image/png` or `image/jpeg`) matches the real format | 400 |
| The image decodes | 400 (malformed) |
| At most 2048 × 2048 pixels | 400 |

If any check fails, nothing is saved and the old picture stays. On success the image is re-encoded (metadata removed) and saved together with the profile in one save.

400 errors use the Milestone 3 domain-rule shape:

```jsonc
{ "title": "One or more business rules were broken.", "status": 400,
  "errors": ["The file must be a PNG or JPEG image."] }
```

| Rule broken | Message |
|---|---|
| No file or empty file | `Choose an image to upload.` |
| Over 2 MB | `The image must be 2 MB or smaller.` |
| Not PNG or JPEG | `The file must be a PNG or JPEG image.` |
| Content type doesn't match | `The file type doesn't match its content.` |
| Can't be decoded | `The image is damaged or not a real image.` |
| Too many pixels | `The image must be 2048 × 2048 pixels or smaller.` |

### Download response (`GET`)

```http
HTTP/1.1 200 OK
Content-Type: image/png
Cache-Control: private, no-store
X-Content-Type-Options: nosniff

<image bytes>
```

`Content-Type` is the stored format (`image/png` or `image/jpeg`), never what the uploader claimed.

### Failures

If the database or the store fails during any of these calls, the API returns the default ProblemDetails **500** ("An error occurred while processing your request." and a `traceId`) with no exception message, SQL, connection string or file path, and nothing is saved (WI-8).

## Users (changed)

`GET /api/users/me` keeps its route and status codes. The response gets one field:

```jsonc
// UserResponse
{ "id": 7, "name": "Sam Example", "email": "sam@example.com", "phoneNumber": "514-555-0199",
  "hasPicture": true }
```

There's no image URL in the profile, and never a token.

## Authorization (changed)

| Policy | Before (Milestone 3) | Now |
|---|---|---|
| `pets.write` | scope | scope + role `staff` |
| `shelters.write` | scope | scope + role `staff` |
| `adoptions.review` | scope | scope + role `staff` |
| every other policy | scope (plus a local profile where it needed one) | no change |

A signed-in user without the role gets **403**, even if their token has the scope. The role is read from the `roles` claim of the validated access token (decision D2 and D3).

## Client routes (changed)

| Route | Before | Now |
|---|---|---|
| `GET /auth/login` | starts sign-in | no change |
| `POST /auth/logout` | signs out of the cookie and Keycloak | no change |
| `GET /auth/access-token` | gave the access token to browser JavaScript | **removed**, returns 404 |
| `GET /profile/picture` | none | new: the client's server fetches the signed-in user's picture with their token and streams it (WI-6) |

## Sign-off

| Member | Approved on the WI-1 issue |
|---|---|
| Aiden | ☐ |
| Darcy | ☐ |
| Chloe | ☐ |
