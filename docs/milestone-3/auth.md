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

The intended client flow is authorization code with Proof Key for Code
Exchange (PKCE):

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
chosen yet. No provider credentials or client secrets are included in this
repository.

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

The controller metadata currently present in `PetsController` is:

| Method | Endpoint | Authorization |
|---|---|---|
| `GET` | `/api/pets` | Public: `[AllowAnonymous]` |
| `GET` | `/api/pets/{id}` | Public: `[AllowAnonymous]` |
| `POST` | `/api/pets` | `[Authorize(Policy = AuthorizationPolicies.PetsWrite)]` |
| `PUT` | `/api/pets/{id}` | `[Authorize(Policy = AuthorizationPolicies.PetsWrite)]` |
| `DELETE` | `/api/pets/{id}` | `[Authorize(Policy = AuthorizationPolicies.PetsWrite)]` |

The other named policies are registered in `Program.cs`, but no controller
actions using those policies are present in the code reviewed for this
milestone. Their endpoint mappings will be added with their corresponding
controllers.

## Current-user identity

`HttpCurrentUser` reads only the `sub` claim from `HttpContext.User`. It does
not accept an identity from a request body, header, or route. Accessing the
subject without a `sub` claim throws an `InvalidOperationException` with a
message identifying the missing claim.

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
124 passed
63 unit tests
61 integration tests
0 failed
```
