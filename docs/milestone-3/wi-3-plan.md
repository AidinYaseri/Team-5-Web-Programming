# WI-3 / Issue #14: Authentication and scope-policy implementation plan

## Research summary

Issue #14 changes `Animal.API` from anonymous endpoints into an OAuth2/OIDC
resource server. The API must validate bearer tokens through the ASP.NET Core
JWT bearer handler and authorize operations by scope. Controllers remain thin;
authentication and authorization belong in the API composition root and
policies, while tests exercise the real `Program.cs` through
`AnimalApiFactory`.

The current code has no authentication registration, no `UseAuthentication()`,
and no authorization attributes. `PetsController` has five endpoints: both
GET endpoints are intentionally public, while POST, PUT, and DELETE require
`pets.write`. Existing integration tests call the protected write endpoints
without a token and will therefore need authenticated test clients.

### Dependencies and conflicts

- The provider decision and final scope naming are still open in
  `api-contracts.md`. Implementation is not blocked on that decision: use
  clearly marked placeholder values for `Authentication:Authority` and
  `Authentication:Audience` in development configuration, and keep the
  provider-specific final values for `auth.md` after the decision. Before the
  client is wired, coordinate the final provider, authority, audience, and
  scopes with WI-4 (Chloe's Blazor client).
- The contract sheet is draft v2, but it is the source of the scope list for
  this work. If #12 changes the scopes, update the constants, policies, tests,
  and `auth.md` together before implementation.
- The brief's statement that only `Animal.API`, `tests/`, and `auth.md` change
  is compatible with the repository. Domain and Data remain untouched.
- No provider, client secret, signing key, or tenant credential belongs in the
  plan, source, appsettings files, or test output. Test signing keys are
  generated in memory at test time.

## File-by-file implementation order

### 1. Packages and configuration

- **Agent:** `api-service` for API project files; `testing` for the integration
  test project reference.
- **Files:** `src/Animal.API/Animal.API.csproj`,
  `src/Animal.API/appsettings.json`,
  `src/Animal.API/appsettings.Development.json`, and
  `tests/Animal.IntegrationTests/Animal.IntegrationTests.csproj`.
- Add `Microsoft.AspNetCore.Authentication.JwtBearer` version `10.0.12` to
  the API. Add the same package to integration tests because the test host
  configures `JwtBearerOptions`.
- Add non-secret `Authentication:Authority` and
  `Authentication:Audience` configuration keys. Use clearly marked
  provider-neutral placeholder values in `appsettings.Development.json` so
  implementation and tests can proceed; replace them with the agreed
  provider's non-secret values once #12 is resolved. Secrets, if any, remain
  in user-secrets or deployment environment configuration and are never
  committed.
- **Proof:** package restore/build resolves the JWT handler, configuration
  binding is present, and no credentials or signing material appears in the
  diff.

### 2. `Program.cs`: authentication, policies, and middleware

- **Agent:** `api-service`.
- **File:** `src/Animal.API/Program.cs`.
- Register `AddAuthentication` with the JWT bearer default scheme and
  `AddJwtBearer` using authority and audience from configuration. Enable
  signature, issuer, audience, and lifetime validation, set
  `MapInboundClaims = false`, and retain the framework's normal clock-skew
  behavior unless the tests deliberately configure a smaller value.
- Add a fallback authorization policy requiring an authenticated user so a
  newly added endpoint is protected by default.
- Add `UseAuthentication()` before `UseAuthorization()` and before controller
  mapping. Because the fallback policy also applies to endpoint mappings,
  call `.AllowAnonymous()` on `MapOpenApi()` so the development-only
  `/openapi/v1.json` endpoint remains public.
- **Proof:** the middleware ordering and fallback policy are visible in the
  composition root; unauthenticated access to a protected endpoint returns
  401, OpenAPI remains reachable without a token in Development, and
  authentication cannot be bypassed by model-validation behavior.

### 3. Named scope policies and shared policy names

- **Agent:** `api-service`.
- **Files:** `src/Animal.API/Authorization/AuthorizationPolicies.cs`,
  `src/Animal.API/Authorization/ScopeClaims.cs` (or equivalent),
  `src/Animal.API/Program.cs`, and the handler/requirement files if that
  implementation style is selected.
- Define one named policy for every scope in the contract sheet:
  `pets.write`, `shelters.write`, `users.read`, `users.write`,
  `adoptions.read`, `adoptions.write`, `adoptions.review`,
  `appointments.read`, `appointments.write`, and
  `notifications.read`. Keep names in one constants class and reuse those
  constants from registration and controller attributes.
- Implement the scope check in a dedicated, directly testable class rather
  than a lambda embedded in `Program.cs`. Use either a `ScopeRequirement`
  with an `AuthorizationHandler`, or a static
  `ScopeClaims.HasScope(ClaimsPrincipal, string)` helper. It must check both
  `scope` and `scp`, split space-separated values, accept the required scope
  among multiple scopes, and reject a missing scope. Do not rely on
  `RequireClaim` with one unsplit value.
- **Proof:** unit tests cover one scope, multiple scopes, `scope`, `scp`, and a
  missing claim by calling the dedicated class directly; the policy
  registrations compile without duplicated literals.

### 4. `ICurrentUser`

- **Agent:** `api-service`.
- **Files:** `src/Animal.API/Services/ICurrentUser.cs` and its implementation
  (for example `HttpCurrentUser.cs`), plus `src/Animal.API/Program.cs`.
- Register a scoped `ICurrentUser` implementation that reads only the `sub`
  claim from `HttpContext.User`. It must not accept a user id from a request
  body, header, or route, and must fail clearly when `sub` is absent rather
  than returning a fabricated identity.
- Register `AddHttpContextAccessor()` before registering the current-user
  service so the implementation can access the current request context.
- **Proof:** unit tests verify the returned subject and the missing-subject
  failure; the service is resolved through DI and has no Domain or Data
  dependency.

### 5. Controller attributes

- **Agent:** `api-service`.
- **File:** `src/Animal.API/Controllers/PetsController.cs`.
- Mark `GET /api/pets` and `GET /api/pets/{id}` explicitly with
  `[AllowAnonymous]`.
- Mark POST, PUT, and DELETE with `[Authorize(Policy = ...)]` using the shared
  `pets.write` policy name. Do not add business logic or bind an entity/user
  identity from the request.
- **Proof:** the endpoint-to-policy map matches the contract, public GETs work
  with no token, and protected writes distinguish 401 from 403.

### 6. Test token helper and host override

- **Agent:** `testing`.
- **Files:** `tests/Animal.IntegrationTests/Api/AnimalApiFactory.cs` and a
  focused helper such as `tests/Animal.IntegrationTests/Api/TestTokenFactory.cs`.
- Generate an ephemeral signing key in memory and provide helpers for tokens
  with configurable issuer, audience, expiry, `sub`, and `scope`/`scp` claims.
  Include a helper for an authenticated client carrying a valid
  `pets.write` token.
- In `ConfigureTestServices`, use `PostConfigure<JwtBearerOptions>` to set a
  matching `OpenIdConnectConfiguration` with the test issuer and signing key,
  and set `ConfigurationManager` to
  `StaticConfigurationManager<OpenIdConnectConfiguration>`. Also set valid
  issuer and audience validation values. Do not only set `options.Configuration`;
  that would allow the handler to attempt real provider metadata/key
  discovery.
- **Proof:** integration tests run without network access or provider
  credentials, and valid locally signed tokens are accepted by the real
  application pipeline.

### 7. Authentication and authorization tests

- **Agent:** `testing`.
- **Files:** `tests/Animal.IntegrationTests/Api/AuthTests.cs` and
  `tests/Animal.UnitTests/Api/ScopeAuthorizationTests.cs`,
  `tests/Animal.UnitTests/Api/CurrentUserTests.cs` (names may follow existing
  test conventions).
- Against a protected Pets write endpoint, test every row in the brief:
  no header -> 401; malformed token (`Bearer not-a-jwt`) -> 401; wrong
  signing key -> 401; expired token -> 401; wrong issuer -> 401; wrong
  audience -> 401; valid token without `pets.write` -> 403; valid token with
  `pets.write` plus another scope -> success; and public GET without a token
  -> 200.
- Make the expired token at least ten minutes old, or otherwise outside the
  default five-minute clock skew.
- Add unit coverage for scope parsing and `ICurrentUser`, including `scope`
  versus `scp`, multiple scopes, missing claims, valid `sub`, and missing
  `sub`.
- **Proof:** all listed status-code assertions pass against the actual host,
  and unit tests fail if scope parsing or claim handling regresses.

### 8. Update existing Pets integration tests

- **Agent:** `testing`.
- **File:** `tests/Animal.IntegrationTests/Api/PetsEndpointTests.cs` (and any
  directly affected API startup test).
- Use the factory's authenticated client for every create, update, and delete
  request, including validation, missing-resource, and conflict cases. Keep
  public GET requests on the unauthenticated client so public behavior remains
  explicitly covered.
- Ensure the client/token setup does not change request bodies or introduce
  user identity fields.
- **Proof:** all pre-existing Pets behavior tests retain their original
  success/error assertions instead of failing early with 401, while public
  GET tests continue to work without credentials.
- **Client coordination:** until WI-4 adds sign-in and attaches bearer
  tokens, the Blazor client's create, edit, and delete calls are expected to
  receive 401 responses. This is an intentional integration boundary, not a
  reason to weaken the API policies.

### 9. Authentication documentation

- **Agent:** default agent, with `testing` supplying final test names/results.
- **File:** `docs/milestone-3/auth.md`.
- Document the roles (Blazor client, signed-in resource owner, authorization
  server, and API resource server), authorization-code + PKCE flow, bearer
  token transport, provider-dependent authority and audience configuration,
  issuer/signature/audience/lifetime validation, clock skew, all scope names,
  the complete endpoint-to-policy table, and the public/protected boundary.
- Document that local secrets use user-secrets and deployment secrets use
  environment variables or a secret store, with no secrets in git. Include
  the final authentication test names and the pasted whole-solution
  `dotnet test` summary after validation.
- **Proof:** every section-8 documentation requirement is present and the
  table matches the policy constants and controller attributes.

## Gate 1 completion checklist

- [ ] Placeholder authority/audience values allow implementation to proceed;
  final provider values and scope decision from #12 are recorded in
  `auth.md` and coordinated with WI-4.
- [ ] JWT bearer uses configuration-driven authority/audience; no hand-written
  token parsing.
- [ ] Changes are limited to `src/Animal.API`, `tests/`, and
  `docs/milestone-3/auth.md`.
- [ ] Authentication precedes authorization; fallback authorization is
  authenticated-by-default; public GETs use explicit `[AllowAnonymous]`; and
  `MapOpenApi().AllowAnonymous()` keeps development OpenAPI public.
- [ ] Every contract scope has one shared named policy.
- [ ] Scope authorization is implemented in a directly testable class, not an
  inline `Program.cs` lambda.
- [ ] `scope`/`scp` values are split on spaces.
- [ ] Inbound claim mapping is disabled and `ICurrentUser` reads `sub` only.
- [ ] `AddHttpContextAccessor()` is registered for `ICurrentUser`.
- [ ] Test validation uses `StaticConfigurationManager`.
- [ ] Malformed-token and expiry testing are covered, with expiry outside the
  five-minute clock skew.
- [ ] Existing write tests send a valid token.
- [ ] Every row in the brief's test table has an automated test.
- [ ] No secret values appear in this plan or the intended diff.
- [ ] `auth.md` covers the complete section-8 checklist.
  
  ## Gate 1 review (Aiden, 2026-11-04)

Reviewed Copilot's first plan against the Gate 1 checklist and the code. Approved after these corrections:

1. The fallback policy would have made `/openapi/v1.json` return 401. Added `MapOpenApi().AllowAnonymous()`.
2. Step 9 gave `auth.md` to the api-service agent, which may only edit `src/*.Api/`. Moved to the default agent.
3. The scope check was planned as a lambda in `Program.cs`, which unit tests can't reach. Moved to a dedicated class, and added `AddHttpContextAccessor()` for `ICurrentUser`.
4. The plan blocked all implementation on the provider choice. Changed to placeholder config values; only `auth.md` waits.
5. Added a malformed-token (`Bearer not-a-jwt`) → 401 test.
6. Noted that the Blazor client's create/edit/delete calls return 401 until WI-4 adds sign-in.
7. The first plan claimed the brief referenced `tests/Animal.API.Tests`. It didn't (the brief uses `tests/Animal.IntegrationTests`). Removed the false conflict.
