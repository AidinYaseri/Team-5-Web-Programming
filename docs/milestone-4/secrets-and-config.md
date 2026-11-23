# Secrets and configuration guide

This guide documents settings in the current Milestone 4 client, API, and local Keycloak setup. ASP.NET Core reads `appsettings.json`, then `appsettings.{Environment}.json`, environment variables, and (in Development) .NET user-secrets; later providers override earlier values. For environment variables, replace configuration colons with double underscores, for example `Authentication__Authority`.

## Settings by environment

| Component / setting | Local Development | Production / deployed environment | Secret? |
|---|---|---|---|
| Client `AnimalApi:BaseUrl` | `http://localhost:5015/` in `src/Animal.Client/appsettings.json`; client launch profiles use 5012 / 7009 | Supply HTTPS API base URL as `AnimalApi__BaseUrl` | No |
| Client `Authentication:Authority` | `http://localhost:8080/realms/animal` in `src/Animal.Client/appsettings.Development.json` | HTTPS issuer URL from trusted provider as `Authentication__Authority` | No; trust-sensitive |
| Client `Authentication:ClientId` | `animal-client` in client Development config | Registered OIDC client ID as `Authentication__ClientId` | No |
| Client `Authentication:ClientSecret` | Generated on Keycloak realm import; store in `dotnet user-secrets` for `Animal.Client` | Inject from hosting secret manager as `Authentication__ClientSecret` | **Yes** |
| Client `Authentication:Scopes` | Default scope list in `src/Animal.Client/appsettings.json` | Override with least-privilege `Authentication__Scopes` if required | No; permission-sensitive |
| API `Authentication:Authority` | `http://localhost:8080/realms/animal` in `src/Animal.API/appsettings.Development.json` | Trusted HTTPS issuer as `Authentication__Authority` | No; trust-sensitive |
| API `Authentication:Audience` | `animal-api` in API Development config | API resource audience as `Authentication__Audience` | No; trust-sensitive |
| API `ConnectionStrings:AnimalDb` | LocalDB integrated authentication in API Development config; no password is embedded | Inject production connection string as `ConnectionStrings__AnimalDb` through secret management when it includes credentials | **Usually yes** |
| API `Cors:ClientOrigin` | `http://localhost:5012` in API Development config (set matching HTTPS origin when using the HTTPS client profile) | Exact allowed browser origin as `Cors__ClientOrigin` only if direct browser-to-API calls are used | No; security-sensitive |
| Keycloak `KEYCLOAK_ADMIN_PASSWORD` | Set in gitignored `keycloak/.env`, copied from `.env.example` | Do not use local dev compose for production; configure admin credentials in the production IdP's secret store | **Yes** |
| `ASPNETCORE_ENVIRONMENT` | `Development` in API/client `Properties/launchSettings.json` profiles | Set by deployment host, usually `Production` | No |
| Logging levels, `AllowedHosts` | Checked-in appsettings defaults | Set by appsettings or environment variables (`Logging__LogLevel__...`, `AllowedHosts`) | No |

The API `appsettings.json` files contain placeholder authority/audience values; Development files supply local Keycloak settings. Authority, audience, client ID, scope names, ports, and the LocalDB integrated-auth string are not credentials, but changing them can change trust or access and should be reviewed. Never commit real passwords, API keys, signing keys, OIDC client secrets, or credential-bearing connection strings.

## Local setup

1. Install Docker Desktop and the .NET 10 SDK. In the repository root, create the local Keycloak environment file and replace its sample password with a private local value:

   ```powershell
   Copy-Item keycloak/.env.example keycloak/.env
   # Edit keycloak/.env and set KEYCLOAK_ADMIN_PASSWORD to a unique local password.
   docker compose -f keycloak/docker-compose.yml up -d
   ```

   The `.env` file is gitignored. The realm export has no user passwords or client secret; Keycloak generates the client secret during realm import. Set passwords for the fictional local users using the steps in [identity-provider-setup.md](identity-provider-setup.md).

2. Copy the generated `animal-client` secret from the Keycloak admin console and store it outside the repository. The client project already defines a `UserSecretsId`.

   ```powershell
   dotnet user-secrets set "Authentication:ClientSecret" "<generated-secret>" --project src/Animal.Client
   dotnet user-secrets list --project src/Animal.Client
   ```

   User-secrets are scoped to the client project and stored in the developer profile. They are not encrypted and are for local development only. Do not place the secret in JSON, source code, screenshots, issue comments, or commits. If the client secret is regenerated, replace the stored value.

3. API Development config uses Windows LocalDB integrated authentication. If you use a different local database, set a project-scoped user-secret rather than editing checked-in settings:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:AnimalDb" "<local-connection-string>" --project src/Animal.API
   ```

4. Keep the checked-in API and client Development authority values aligned with Keycloak. Run `dotnet run --project src/Animal.API` and `dotnet run --project src/Animal.Client` in separate terminals. Launch profiles select Development and local ports. Follow `identity-provider-setup.md` for test-user credentials and the sign-in smoke check.

## Production guidance

Supply secrets from the deployment platform's secret manager and inject them as environment variables. For example, `Authentication__ClientSecret` maps to `Authentication:ClientSecret`, and `ConnectionStrings__AnimalDb` maps to `ConnectionStrings:AnimalDb`. Limit access to OIDC client credentials, database credentials, Keycloak administrator credentials, and future data-protection keys; rotate them after suspected exposure. Keep values out of build logs and deployment artifacts.

Use a trusted HTTPS issuer consistently in the client and API, register exact production redirect and sign-out callback URLs, and set the correct audience and least-privilege scopes. Production requires HTTPS, trusted host names, a production database, protected durable ASP.NET data-protection keys, and a shared protected ticket store if the client scales to multiple instances. The current session store is in-memory and is unsuitable for multiple client instances. Do not deploy the local Keycloak `start-dev` container, HTTP port, in-container database, or its `.env` file.

## Documentation follow-up

`docs/milestone-3/auth.md` predates the in-memory ticket store and describes tokens as residing in the encrypted cookie. Current code stores tickets/tokens server-side; the browser cookie carries a session key. Update that earlier guide as WI-1/Milestone 4 documentation is consolidated.
