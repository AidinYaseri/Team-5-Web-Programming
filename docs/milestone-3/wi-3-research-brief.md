# WI-3 (#14) Auth: research brief for the Copilot agents

Use this to prime Copilot in the **Research** step of R-PIV. Copilot writes the plan; you check it with the Gate 1 checklist at the end.

Prepared with Claude Code as research input. Say so in the PR and in the reflection, and keep Copilot's own plan as the plan of record.

---

## 1. Goal and acceptance criteria (from #14)

Make `Animal.API` a resource server: validate bearer tokens from an OAuth2/OIDC provider and enforce scope policies per endpoint.

- [ ] JWT bearer authentication validates signature, issuer, audience and lifetime, with authority and audience read from configuration
- [ ] `UseAuthentication()` runs before `UseAuthorization()`
- [ ] One named policy per scope in the contract sheet, and every endpoint is either policy-protected or explicitly `[AllowAnonymous]`
- [ ] The current user is read from token claims only (`ICurrentUser`)
- [ ] Tests prove: no token → 401; invalid, expired, wrong-issuer and wrong-audience tokens → 401; valid token missing the scope → 403; right scope → success while a different scope fails; public endpoints work without a token
- [ ] No secrets committed (user-secrets or environment variables only)
- [ ] `docs/milestone-3/auth.md` documents the flow, the roles, the validation settings and the endpoint → policy table

## 2. Decision needed before the plan: identity provider

The API code is the same for every provider. Only these values change:

| | Microsoft Entra ID | Auth0 | Keycloak (Docker) |
|---|---|---|---|
| `Authentication:Authority` | `https://login.microsoftonline.com/<tenant-id>/v2.0` | `https://<tenant>.auth0.com/` | `http://localhost:8080/realms/<realm>` |
| `Authentication:Audience` | `api://<api-client-id>` (or the client id) | the API Identifier you create | needs an audience mapper in the client scope |
| Scope claim in the token | `scp` | `scope` | `scope` |
| Defining scopes | App registration → *Expose an API* | APIs → *Permissions* | Client scopes |

Authority and audience are **not secrets**, so they can go in `appsettings.Development.json`. A client secret (only Chloe's Blazor client has one) goes in `dotnet user-secrets`, never in a file.

## 3. Current code (branch `feature/milestone-3-layered-service-development`)

- `src/Animal.API/Program.cs`: no authentication yet. Pipeline is `UseExceptionHandler` → `UseHttpsRedirection` → `UseAuthorization` → `MapControllers`. `UseAuthentication` is missing.
- `src/Animal.API/Controllers/PetsController.cs`: 5 endpoints, all anonymous today. GET list and GET by id must stay public; POST, PUT and DELETE need `pets.write`.
- `src/Animal.API/ErrorHandling/DomainExceptionHandler.cs`: maps domain errors to ProblemDetails. 401/403 come from the auth middleware, not from here.
- `tests/Animal.IntegrationTests/Api/AnimalApiFactory.cs`: runs the real `Program.cs` with SQLite swapped in. This is where test token validation goes.
- `tests/Animal.IntegrationTests/Api/PetsEndpointTests.cs`: create, update and delete tests send no token today.
- `docs/milestone-3/api-contracts.md`: the scope list and the endpoint → scope table (source of truth).
- Agents: `.github/agents/api-service.agent.md` (edits `src/*.Api/`), `testing.agent.md` (edits `tests/`), `code-review.agent.md` (read-only).

## 4. Constraints for the plan

- Use `Microsoft.AspNetCore.Authentication.JwtBearer` **10.0.12** (same version as the other ASP.NET packages). Never decode or check a JWT by hand.
- Auth code lives in `Animal.API` only. Domain and Data stay untouched.
- Set `MapInboundClaims = false` so `sub` and `scope` keep their real names instead of long `http://schemas...` names.
- **Deny by default:** set a fallback policy that requires an authenticated user, then mark the public GETs `[AllowAnonymous]` on purpose. A new endpoint that someone forgets to annotate is then protected, not open.
- Define a policy for **every** scope in the contract sheet now, even for slices not built yet, so Darcy, Chloe and you reuse the same names. Keep the names in one constants class, not as strings scattered in attributes.
- `ICurrentUser` reads `sub` from `HttpContext.User`. Nothing takes a user id from the body, a header or the route.
- Tests must not call a real provider. They sign their own tokens with a key generated at test time, so CI needs no secrets.

## 5. Verified pitfalls (tested on .NET 10 in a throwaway copy)

These came up when the approach was actually run. Give them to Copilot, because its first attempt will likely hit at least one.

1. **Overriding `Configuration` alone doesn't work in tests.** When `Program.cs` sets an `Authority`, the handler builds a `ConfigurationManager` that tries to download the real provider's keys. Setting only `options.Configuration` in the test gives 401 `"The signature key was not found"`. What works, in a `PostConfigure<JwtBearerOptions>` inside `ConfigureTestServices`:
   ```csharp
   var config = new OpenIdConnectConfiguration { Issuer = TestIssuer };
   config.SigningKeys.Add(TestKey);
   options.Configuration = config;
   options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(config); // namespace Microsoft.IdentityModel.Protocols
   options.TokenValidationParameters.ValidIssuer = TestIssuer;
   options.TokenValidationParameters.ValidAudience = TestAudience;
   ```
   The integration test project also needs a `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.12 reference for `JwtBearerOptions`.
2. **Scopes come as one space-separated string**, for example `"scope": "pets.read pets.write"`. `RequireClaim("scope", "pets.write")` fails on that. The policy has to split the value on spaces and check both `scope` and `scp` (Entra uses `scp`). Test it with a token that has several scopes.
3. **Default clock skew is 5 minutes.** A token that expired 2 minutes ago is still accepted. The expired-token test must use an expiry at least 10 minutes in the past, or the test must set `ClockSkew` lower.
4. **Auth runs before model validation.** Once POST/PUT/DELETE are protected, the existing tests for those endpoints get 401 instead of 201/204/400. In the trial, 3 existing Pets tests failed this way. The plan must update them to send a valid `pets.write` token, for example through a helper on `AnimalApiFactory` that returns an authenticated `HttpClient`.

## 6. Endpoint → policy map (Pets now, the rest when each slice is built)

| Endpoint | Policy |
|---|---|
| `GET /api/pets`, `GET /api/pets/{id}` | `[AllowAnonymous]` |
| `POST /api/pets`, `PUT /api/pets/{id}`, `DELETE /api/pets/{id}` | `pets.write` |
| Shelters, Users, Adoptions, Appointments, Notifications | as listed in `api-contracts.md`, applied by each slice owner |

## 7. Tests the plan must include

In `tests/Animal.IntegrationTests/Api/` (for example `AuthTests.cs`), against a protected Pets endpoint:

| Case | Expected |
|---|---|
| No `Authorization` header | 401 |
| Token signed with a different key | 401 |
| Expired token (10+ minutes ago) | 401 |
| Wrong issuer | 401 |
| Wrong audience | 401 |
| Valid token, no `pets.write` (e.g. only `shelters.write`) | 403 |
| Valid token with `pets.write` among other scopes | 201 |
| `GET /api/pets` with no token | 200 |

Plus unit tests in `tests/Animal.UnitTests` for the scope check (one scope, several scopes, `scp` vs `scope`, missing claim) and for `ICurrentUser` (returns `sub`, and fails clearly when there is none). Every existing test must still pass.

## 8. What `auth.md` needs (the milestone grades this)

- **Roles in the flow:** client = `Animal.Client` (Blazor), resource owner = the signed-in user, authorization server = the chosen provider, resource server = `Animal.API`.
- **Sign-in flow:** authorization code + PKCE. The client gets an access token and sends it as `Authorization: Bearer`. The API never sees a password.
- **Validation settings:** authority, audience, issuer, signature, lifetime and clock skew. Give the config keys, not secret values.
- **Endpoint → policy table** (from section 6) and the scope list.
- **Where secrets live:** user-secrets locally, environment variables in deployment, none in git.
- **Test results:** the test names and a pasted `dotnet test` summary.

## 9. Coordinate with Chloe (WI-4)

Her Blazor client must request the same audience and scopes. Blazor Server runs on the server, so it uses the OpenID Connect handler with authorization code + PKCE (PKCE is on by default). It keeps its client secret in user-secrets and attaches the access token to `PetsApiClient` calls. Agree on the provider and the scope names with her before either of you builds.

---

## 10. Prompts for each R-PIV stage

**Research** (Copilot chat, Agent mode, default agent; attach this file, `api-contracts.md`, `Program.cs`, `PetsController.cs`, `AnimalApiFactory.cs`):
> Read the attached files for issue #14. Summarize the architectural constraints, the files that will change, the dependencies on other work items, and anything in the brief that conflicts with the current code. Don't change any files.

**Plan:**
> Write a file-by-file implementation plan for #14, in the order: packages and configuration, `Program.cs`, policies, `ICurrentUser`, controller attributes, test helpers, auth tests, update existing tests, `auth.md`. For each step, say which agent does it (api-service or testing) and how we'll know it worked. Cover every acceptance criterion and all four pitfalls in section 5. Don't write code yet.

Review the plan with the Gate 1 checklist below. Correct it in the chat, then save the final plan (e.g. `docs/milestone-3/wi-3-plan.md`) as PR evidence.

**Implement:** run the `api-service` agent for the `src/Animal.API` steps, then the `testing` agent for the `tests/` steps, one plan step at a time. Commit each step with `#14: ...`.

**Validate:**
> Run `dotnet build` and `dotnet test` for the whole solution. Report warnings and failures. Don't change anything.

**Code review** (`code-review` agent, on the PR diff):
> Review the diff for #14 against the Human Gate Checklist in the milestone: token validation settings, middleware order, deny-by-default, least-privilege policy per endpoint, user identity from claims only, auth test coverage, and secrets. Group findings by severity.

---

## Gate 1 checklist (review Copilot's plan before any code)

- [ ] Uses `AddJwtBearer` with authority and audience from configuration, no hand-written token parsing
- [ ] Only `Animal.API` and `tests/` change, plus `auth.md`
- [ ] `UseAuthentication()` before `UseAuthorization()`
- [ ] Fallback policy requires an authenticated user, and the public GETs are explicitly `[AllowAnonymous]`
- [ ] One policy per scope from the contract sheet, names kept in one place
- [ ] Scope check splits space-separated values and handles `scp` and `scope`
- [ ] `MapInboundClaims = false`, and `ICurrentUser` reads `sub` only
- [ ] Test token setup uses `StaticConfigurationManager`, not just `Configuration`
- [ ] Expired-token test is outside the 5-minute clock skew
- [ ] Existing Pets create/update/delete tests are updated to send a token
- [ ] Every row in section 7 has a test
- [ ] No secret values anywhere in the plan
- [ ] `auth.md` covers everything in section 8

## Gate 2 checklist (after implementation)

- [ ] `dotnet build`: 0 warnings. `dotnet test`: all pass, including the new auth tests.
- [ ] Read the diff line by line. Policies are on the right endpoints, and nothing user-specific comes from the request body.
- [ ] Temporarily remove `[Authorize(Policy = ...)]` from one endpoint and confirm a test fails, so the tests really check something. Then put it back.
- [ ] `git diff --cached` contains no keys, secrets or real tenant credentials.
