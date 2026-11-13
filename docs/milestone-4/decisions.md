# Milestone 4 Decisions (WI-1)

**Status: proposed, waiting for sign-off on the WI-1 issue.** Darcy and Chloe approve by commenting on the issue. After that, a change to any decision goes through a PR that updates this file, and the owners of the affected work items get tagged.

These are decided before anyone starts coding, as the Milestone 4 spec asks, so the identity provider setup (WI-2, WI-3), the picture backend (WI-4, WI-5), the picture screen (WI-6) and the tests (WI-7, WI-8, WI-10) can be built at the same time without different assumptions. The picture endpoints themselves are in [api-contracts.md](api-contracts.md).

## Summary

| # | Decision | Choice | Affects |
|---|---|---|---|
| D1 | Identity provider | Keycloak 26 in Docker, realm `animal` | WI-2, WI-3, WI-10 |
| D2 | Token contents | Audience `animal-api`, scopes in `scope`, a flat `roles` claim | WI-2 |
| D3 | Staff-only operations | Need the scope **and** the `staff` role | WI-2 |
| D4 | Where the client keeps tokens | Server side only, the `/auth/access-token` route is removed | WI-3, WI-6 |
| D5 | Sign-out | Keep `POST /auth/logout` (cookie and Keycloak session) | WI-3, WI-10 |
| D6 | Picture formats and limits | PNG and JPEG, 2 MB, 2048 × 2048 pixels | WI-4, WI-5, WI-6 |
| D7 | Checking and cleaning images | Check the file signature, then decode and re-encode with SkiaSharp | WI-4, WI-5 |
| D8 | Picture storage | `IProfilePictureStore` with a `ProfilePictures` table in the same database | WI-4, WI-5, WI-8 |
| D9 | Picture access | Only through `/api/users/me/picture`, no public or temporary links | WI-5, WI-6 |
| D10 | Profile response | `UserResponse` gets `hasPicture` | WI-5, WI-6, WI-7 |
| D11 | Errors from failing dependencies | Generic 500 ProblemDetails, nothing saved | WI-5, WI-8 |
| D12 | Secret store | `dotnet user-secrets` locally, environment variables elsewhere, a gitignored `.env` for Docker | WI-2, WI-9 |
| D13 | Test doubles | Locally signed tokens for the provider, in-memory and failing stores for storage | WI-7, WI-8, WI-10 |
| D14 | Contract tests | The integration tests call the real API and read the JSON into the client's own DTOs | WI-7 |

## D1: Identity provider

**Choice:** Keycloak 26 (`quay.io/keycloak/keycloak:26.0`), run with Docker in dev mode on `http://localhost:8080`. One realm called `animal`.

| Setting | Value |
|---|---|
| Authority (API and client) | `http://localhost:8080/realms/animal` |
| Client | `animal-client`, confidential, standard flow (authorization code) with PKCE `S256` |
| Redirect URI | `https://localhost:7009/signin-oidc` and `http://localhost:5012/signin-oidc` |
| Post-logout redirect URI | `https://localhost:7009/signout-callback-oidc` and `http://localhost:5012/signout-callback-oidc` |
| Test users | `adopter1`, `adopter2` (no role) and `staff1` (role `staff`), fake data only |

**Why:** it's free, runs on our own machines without an account, and the API needs no code change because it already reads the authority from configuration. Auth0 was the other option. It needs nothing installed, but the client would need extra code to ask for the API audience, and the whole team would share one online tenant.

**Cost:** everyone needs Docker Desktop. The realm is exported to `keycloak/animal-realm.json` (WI-2) so everyone imports the same setup instead of clicking through it. The export must not contain real passwords or the client secret.

**Note:** HTTP is only allowed because `RequireHttpsMetadata` is already off in Development. Anywhere else, the authority must be HTTPS.

## D2: Token contents

The API already checks signature, issuer, audience and expiry (Milestone 3, `AuthTests`). For Keycloak's tokens to pass, WI-2 sets up:

- an **audience mapper** on `animal-client` that adds `animal-api` to `aud` (Keycloak only puts `account` there by default)
- the 10 scopes (`pets.write`, `users.read` and so on) as **optional client scopes** on `animal-client`, so they show up in the `scope` claim that `ScopeClaims.HasScope` already reads
- the **basic** client scope kept on the client, because it is what puts `sub` in the access token in Keycloak 25 and later
- a **realm roles mapper** that writes the roles as a flat `roles` claim (Keycloak's default is the nested `realm_access.roles`, which our policies can't read with `MapInboundClaims = false`)

`API Audience` setting: `animal-api`.

## D3: Staff-only operations need a role, not only a scope

**Problem found while planning:** in Keycloak (and in Auth0) an optional scope is given to anyone who asks for it. Our client already asks for every scope, including `adoptions.review`. So right now any signed-in adopter would get a token that lets them approve applications or delete pets. In Milestone 3 this didn't show, because the tests make their own tokens.

**Choice:** the policies for staff operations require the scope **and** the realm role `staff` in the `roles` claim:

| Policy | Requires |
|---|---|
| `pets.write` | scope `pets.write` + role `staff` |
| `shelters.write` | scope `shelters.write` + role `staff` |
| `adoptions.review` | scope `adoptions.review` + role `staff` |
| everything else | the scope only (users act on their own data) |

The role comes from the token the API validated, never from the client. WI-2 adds this in `AuthorizationPolicies.Configure` with tests: staff scope without the role → 403, with the role → allowed. `auth.md` gets the updated table (WI-11).

## D4: The client keeps tokens on the server

**Problem:** `GET /auth/access-token` returns the access token to browser JavaScript (`wwwroot/auth.js`). The spec says to keep tokens out of client-side code, and any script running on the page could read it.

**Choice:** remove the route and `auth.js`. The tokens stay in the encrypted auth cookie (`SaveTokens = true`), and `AccessTokenHandler` reads them on the server with `HttpContext.GetTokenAsync("access_token")`. Blazor Server runs on the server, so it doesn't need the token in the browser. WI-3 adds a test that `/auth/access-token` returns 404.

**Changed in WI-10:** the session is now kept on the server too (`InMemoryTicketStore`, the cookie's `SessionStore`). The cookie only holds a random session key. **Why:** with the whole session in the cookie, signing out only made the browser delete it, so a cookie copied before sign-out kept working until it expired. A test showed this (`SignOut_CopiedCookieFromBeforeSignOut_NoLongerWorks`). Now sign-out deletes the session on the server and any copy stops working. The tokens also never go to the browser at all, not even encrypted. **Cost:** sessions live in the client's memory, so restarting the client signs everyone out, and running several client servers would need a shared cache (for example Redis) instead.

## D5: Sign-out

**Choice:** keep the existing `POST /auth/logout`. It signs out of the cookie and of Keycloak (`end_session` with the id token), then returns to `/`. Signing out only ends that device's session. Other devices keep their own session until they sign out or it expires.

| Lifetime | Value |
|---|---|
| Access token | 30 minutes (`accessTokenLifespan` in the realm file) |
| Client cookie | 30 minutes, **not** sliding, `HttpOnly`, `SameSite=Lax`, HTTPS-only outside Development. Rejected as soon as the access token inside it has expired |
| Keycloak SSO session | 30 minutes idle, 10 hours max (defaults) |

Refreshing an expired access token is out of scope. When it expires, the cookie is rejected and the user signs in again (silently, without a password, while the Keycloak session is still active). This goes in the threat model as a known limit.

**Changed in WI-3:** the access token was 5 minutes. Without refresh, that signed users out every 5 minutes. It's 30 minutes now, which is safe enough because the token no longer leaves the server (D4), and the cookie can't outlive it.

## D6: Picture formats and limits

| Rule | Value | Status code when broken |
|---|---|---|
| Formats | PNG and JPEG only | 400 |
| File size | 1 byte to 2 MB (2,097,152 bytes) | 400 |
| Whole request size | 3 MB (`[RequestSizeLimit]`, leaves room for the multipart wrapping) | 413, cut off before the body is read |
| Dimensions | at most 2048 × 2048 pixels | 400 |
| Content type sent by the client | must match the real format (`image/png` or `image/jpeg`) | 400 |

The client (WI-6) checks the same limits to give quick feedback, but only the API's checks count.

## D7: Checking and cleaning images

1. **Signature check (Domain, WI-4):** `ProfilePictureRules` reads the first bytes. PNG starts with `89 50 4E 47 0D 0A 1A 0A`, JPEG with `FF D8 FF`. Anything else is rejected, whatever its name or content type says.
2. **Decode and re-encode (API, WI-5):** the image is decoded with SkiaSharp (MIT licence). If it can't be decoded it's malformed → 400. Then it's encoded again in the same format (JPEG quality 90), and only the new bytes are saved.

**Why re-encode:** it drops anything hidden after the image data and removes metadata such as the GPS location in phone photos. That's a privacy fix as well as a security one.

**Package note:** SkiaSharp needs `SkiaSharp.NativeAssets.Linux.NoDependencies` so it runs in CI on Ubuntu. SkiaSharp is only referenced by `Animal.API`, never by `Animal.Domain`, so Domain keeps no references.

## D8: Picture storage

**Choice:** a `ProfilePictures` table in the same SQL Server database, behind an interface:

```csharp
public interface IProfilePictureStore
{
    Task<ProfilePicture?> GetAsync(int userId, CancellationToken cancellationToken);
    Task SaveAsync(ProfilePicture picture, CancellationToken cancellationToken);   // insert or replace, saved by IUnitOfWork
    Task DeleteAsync(int userId, CancellationToken cancellationToken);
}
```

| Column | Type | Notes |
|---|---|---|
| `UserId` | int | primary key and foreign key to `Users.Id`, cascade delete |
| `ContentType` | nvarchar(20) | `image/png` or `image/jpeg` |
| `Data` | varbinary(max) | the re-encoded image |
| `UpdatedAt` | datetime2 | UTC, from `TimeProvider` |

One row per user, so replacing a picture overwrites the old one. Added with a new migration (`AddProfilePictures`), never by editing old ones.

**Why the database:** it's already shared and persistent, so Device B gets what Device A saved. The picture and the profile are saved by the same `IUnitOfWork.SaveChangesAsync`, so they can't end up half-saved. Object storage (Azure Blob) was the other option. It's how big sites do it, but it would add a second system, a cost and a secret, and the spec doesn't need it. Because services only use the interface, a blob store could replace the table later without touching the API.

## D9: Picture access

The picture is private. It's only returned by `GET /api/users/me/picture` to the signed-in owner with `users.read`. There is no route with a user id, no public URL and no temporary link, so nothing about expiring links needs to be documented. The response has `Cache-Control: private, no-store` so a shared computer or a proxy doesn't keep a copy.

The browser can't send our bearer token by itself, so the client (WI-6) shows the picture through its own server: a client route such as `/profile/picture` uses the signed-in user's token on the server side and streams the bytes. No token ever goes into a URL, an `<img>` tag, profile data or a log.

## D10: Profile response

`UserResponse` gets one new field, `hasPicture` (bool). There's no image URL in the profile. The client's copy of `UserResponse` changes in WI-6, and WI-7's contract test checks they still match.

## D11: Errors from failing dependencies

If the database or the store fails, the API returns the normal ProblemDetails **500** from the default exception handler ("An error occurred while processing your request." and a `traceId`). There's no exception message, SQL, connection string or file path in the body. Nothing is saved, because the one save didn't happen. The full error is logged on the server without the image bytes, tokens or personal data. WI-8 tests all of this.

## D12: Secret store

| Value | Secret? | Where it lives |
|---|---|---|
| Authority, audience, client id, API base URL | no | `appsettings.Development.json` |
| Client secret for `animal-client` | **yes** | `dotnet user-secrets` in `Animal.Client` locally, an environment variable (`Authentication__ClientSecret`) elsewhere |
| Database connection string with a password | **yes** | user-secrets or `ConnectionStrings__AnimalDb`. LocalDB uses Windows sign-in, so dev has no password |
| Keycloak admin password | **yes** (dev only) | `.env` next to the compose file, listed in `.gitignore`. A `.env.example` with a fake value is committed |
| Test signing key | no | made fresh in memory for each test run (`TestTokenFactory`) |

CI needs no secrets, because the tests don't use Keycloak or a real database. WI-9 turns this into the full secrets and configuration guide.

## D13: Test doubles

- **Identity provider:** tests keep using tokens signed with a local test key (`AnimalApiFactory.CreateToken`). Each "device" in the cross-device test gets its own token with a different `jti` and issue time for the same `sub`. The real Keycloak is only used for the manual check.
- **Storage:** the API tests use the real store on SQLite (this keeps coverage at the API boundary). WI-8 swaps in a store that throws, to test failures. Unit tests use an in-memory fake.

## D14: Contract tests

`Animal.IntegrationTests` gets a reference to `Animal.Client`. Each contract test calls the real API through `AnimalApiFactory` using the client's own API class, so the client's DTOs and requests are what's being tested. Failure cases (500, 503, timeout, empty body, invalid JSON) use a fake `HttpMessageHandler`. The client project itself still has no project references, so `LayerDependencyTests` keeps passing.

## Sign-off

| Member | Approved on the WI-1 issue |
|---|---|
| Aiden | ☐ |
| Darcy | ☐ |
| Chloe | ☐ |
