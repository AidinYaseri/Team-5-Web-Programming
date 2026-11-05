# Authentication and authorization

## Components and roles

| Role | Component | Responsibility |
|---|---|---|
| Client | `Animal.Client` | Starts sign-in and sends the access token to the API. |
| Resource owner | The signed-in user | Owns the identity and permissions represented by the access token. |
| Authorization server | The identity provider | Authenticates the user and issues tokens. The provider has not been chosen yet; the API currently uses placeholder configuration values. |
| Resource server | `Animal.API` | Validates bearer access tokens and authorizes requests against the configured policies. |

The API is configured with these non-secret settings:

```json
"Authentication": {
  "Authority": "https://placeholder.invalid/",
  "Audience": "api://animal-api"
}
```

They are present in both `src/Animal.API/appsettings.json` and
`src/Animal.API/appsettings.Development.json` as placeholders until the
identity provider decision is finalized.

## Authorization code + PKCE flow

The client flow is authorization code with Proof Key for Code Exchange (PKCE),
implemented in `src/Animal.Client/Program.cs` with the ASP.NET Core OpenID Connect
handler (`ResponseType = code`, `UsePkce = true`, `SaveTokens = true`). Every typed
API client attaches the access token through `AccessTokenHandler`:

1. `Animal.Client` creates a random code verifier and its corresponding code
   challenge.
2. The client redirects the resource owner to the authorization server,
   including the client id, redirect URI, and PKCE challenge.
3. The resource owner signs in and grants the requested permissions at the
   authorization server.
4. The authorization server redirects the client back with a short-lived
   authorization code.
5. The client sends that code and the original verifier to the authorization
   server's token endpoint.
6. The authorization server verifies the verifier and returns an access token.
7. For a protected API request, the client sends the token in the HTTP
   `Authorization` header:

   ```http
   Authorization: Bearer <access-token>
   ```

8. `Animal.API`'s JWT bearer handler validates the token before authorization
   policies evaluate the request.

The identity provider and final provider-specific client settings are not
chosen yet, so sign-in stays off until `Authentication:Authority` and
`Authentication:ClientId` are set for `Animal.Client`. The client secret is read
from configuration on the server only (user-secrets locally) and is never sent to
the browser. No provider credentials or client secrets are included in this
repository.

Known follow-up: the browser fetches the access token from the client's own
`/auth/access-token` endpoint (`wwwroot/auth.js`) before `AccessTokenHandler` uses
it, so the token is readable by page JavaScript. Keeping it server-side only
(Blazor Server can read the saved token during the request) would reduce exposure.

## API token validation

`Program.cs` registers the JWT bearer handler with the default bearer scheme.
The handler reads:

- `Authentication:Authority` for the token issuer authority and metadata
  source.
- `Authentication:Audience` for the intended API audience.

The API enables validation of:

- the issuer;
- the token signature and signing key;
- the audience; and
- the token lifetime.

`MapInboundClaims = false` preserves the token's claim names, including the
literal `sub`, `scope`, and `scp` names used by this application.

The code does not assign `TokenValidationParameters.ClockSkew`; it retains the
JWT bearer framework default of five minutes. The authentication tests use a
token more than ten minutes expired so it remains outside that default
tolerance.

`ScopeClaims.HasScope` checks both `scope` and `scp` claims. Each claim value
is split on whitespace, and scope matching is exact and case-sensitive. For
example, `pets.write` matches the `pets.write` policy, but `pets.write.all`
and `PETS.WRITE` do not.

## Deny-by-default authorization

The fallback authorization policy in `Program.cs` calls
`RequireAuthenticatedUser()`. This protects endpoints by default, including
new controller endpoints unless they explicitly opt out.

Every named scope policy also requires an authenticated user before evaluating
its scope assertion. The authentication middleware runs before authorization:

```text
UseAuthentication()
UseAuthorization()
```

The development-only OpenAPI endpoint is explicitly public through
`MapOpenApi().AllowAnonymous()`. The public Pets GET actions are explicitly
marked with `[AllowAnonymous]`.

## Scopes and policies

The policy names are defined in
`src/Animal.API/Authorization/AuthorizationPolicies.cs` and are the exact
scope strings expected in the token.

| Scope / policy | Purpose |
|---|---|
| `pets.write` | Write operations on pets |
| `shelters.write` | Write operations on shelters |
| `users.read` | Read user data |
| `users.write` | Write user data |
| `adoptions.read` | Read adoption applications |
| `adoptions.write` | Submit or modify adoption applications |
| `adoptions.review` | Review adoption applications |
| `appointments.read` | Read appointments |
| `appointments.write` | Create or modify appointments |
| `notifications.read` | Read and update notification state |

## Endpoint-to-policy map currently in the code

Every controller action has an explicit attribute; anything else falls back to "signed-in user required"
(`ControllerEndpoints_HaveExplicitAuthorizationMetadata` checks this).

| Method | Endpoint | Authorization |
|---|---|---|
| `GET` | `/api/pets`, `/api/pets/{id}` | Public: `[AllowAnonymous]` |
| `POST`, `PUT`, `DELETE` | `/api/pets`, `/api/pets/{id}` | `pets.write` |
| `GET` | `/api/shelters`, `/api/shelters/{id}`, `/api/shelters/{id}/pets` | Public: `[AllowAnonymous]` |
| `POST`, `PUT`, `DELETE` | `/api/shelters`, `/api/shelters/{id}` | `shelters.write` |
| `GET` | `/api/users/me` | `users.read` |
| `PUT` | `/api/users/me` | `users.write` |
| `GET` | `/api/appointments` | `appointments.read` + mapped local user |
| `POST` | `/api/appointments`, `/api/appointments/{id}/cancel` | `appointments.write` + mapped local user |
| `GET` | `/api/notifications` | `notifications.read` + mapped local user |
| `POST` | `/api/notifications/{id}/read` | `notifications.read` + mapped local user |
| `POST` | `/api/adoptions` | `adoptions.write` + mapped local user |
| `GET` | `/api/adoptions/mine` | `adoptions.read` + mapped local user |
| `GET` | `/api/adoptions/{id}` | `adoptions.read-or-review`: `adoptions.review` (any application), or `adoptions.read` + mapped user (own only; others read as 404) |
| `GET` | `/api/adoptions?status=` | `adoptions.review` |
| `POST` | `/api/adoptions/{id}/approve`, `/api/adoptions/{id}/reject` | `adoptions.review` |
| `GET` | `/openapi/v1.json` (Development only) | Public: `AllowAnonymous()` |

The client requests `adoptions.review` along with the other scopes so the review page can work for staff. The identity
provider must only grant it to shelter staff; the API checks it on every reviewer endpoint.

## Current-user identity

`ICurrentUser` is the only way services learn who is calling:

- `Subject` is the token's `sub` claim. `UsersController` uses it to find or create the caller's own profile
  (`User.ExternalId`).
- `UserId` is the local `Users.Id`. `SubjectToUserIdClaimsTransformation` looks the `sub` up in `Users.ExternalId` on
  each authenticated request and adds it as a `local_user_id` claim. Any `local_user_id` claim already in the token is
  removed first, so a token can't choose its own user. Appointments and notifications use `UserId`.

Nothing is read from a request body, header or route. A signed-in user without a profile gets 403 on the appointments
and notifications policies (they require a mapped user) until they create one with `PUT /api/users/me`.
`NotificationsEndpointTests` covers this, including two tests that send a forged `local_user_id` claim.

## Secret handling

The authority and audience placeholders are not secrets. Provider credentials,
client secrets, signing material, passwords, and tokens must not be committed
to Git. Local secret configuration belongs in .NET user-secrets; deployment
secrets belong in environment variables or a managed secret store.

## Authentication and authorization tests

### Integration tests: `AuthTests`

The following tests run against the real `Program.cs` through
`AnimalApiFactory`:

- `ProtectedWrite_WithoutAuthorizationHeader_ReturnsUnauthorized`
- `ProtectedWrite_WithMalformedBearerToken_ReturnsUnauthorized`
- `ProtectedWrite_WithWrongSigningKey_ReturnsUnauthorized`
- `ProtectedWrite_WithExpiredTokenOutsideClockSkew_ReturnsUnauthorized`
- `ProtectedWrite_WithWrongIssuer_ReturnsUnauthorized`
- `ProtectedWrite_WithWrongAudience_ReturnsUnauthorized`
- `ProtectedWrite_WithValidTokenMissingScope_ReturnsForbidden`
- `ProtectedWrite_WithPetsWriteAndAnotherScope_ReturnsSuccess`
- `PublicGet_WithoutAuthorizationHeader_ReturnsSuccess`
- `AuthorizationOptions_FallbackPolicy_RequiresAuthenticatedUser`
- `ControllerEndpoints_HaveExplicitAuthorizationMetadata`
- `OpenApi_WithoutAuthorizationHeader_ReturnsSuccess`

### Unit tests: `ScopeAuthorizationTests`

- `HasScope_ScopeClaimWithMultipleValues_ReturnsTrueForRequiredScope`
- `HasScope_ScpClaim_ReturnsTrueForRequiredScope`
- `HasScope_MissingClaim_ReturnsFalse`
- `HasScope_DifferentScope_ReturnsFalse`
- `HasScope_ScopeWithExtraSegment_ReturnsFalse`
- `HasScope_DifferentCase_ReturnsFalse`

### Unit tests: `CurrentUserTests`

- `Subject_WithSubClaim_ReturnsSubject`
- `Subject_WithoutSubClaim_ThrowsClearException`

## Test result

The whole solution test run completed with:

```text
216 passed
112 unit tests
104 integration tests
0 failed
```

(After merging WI-4, WI-6, WI-7, WI-10 and WI-11 into the milestone branch. The WI-3 PR itself had 124.)
