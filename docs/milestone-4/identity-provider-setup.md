# Identity Provider Setup (Keycloak, WI-2)

This sets up the local Keycloak that the API and the Blazor client use for sign-in. It takes about 10 minutes the first time. The reasons behind each setting are in [decisions.md](decisions.md) (D1 to D3, D12).

**You need:** Docker Desktop (running), the .NET 10 SDK, and the repo on the `feature/milestone-4-security` branch or later.

## What gets created

The realm file `keycloak/animal-realm.json` is imported automatically. It contains:

| Item | Value |
|---|---|
| Realm | `animal` (`http://localhost:8080/realms/animal`) |
| Client | `animal-client`: confidential, authorization code only, PKCE `S256` required, no password grant |
| Redirect URIs | `https://localhost:7009/signin-oidc`, `http://localhost:5012/signin-oidc` |
| Audience mapper | adds `animal-api` to the access token's `aud` |
| Roles mapper | writes realm roles as a flat `roles` claim |
| Scopes | the 10 API scopes as optional client scopes (`pets.write`, `users.read`, ...) |
| Realm role | `staff` |
| Test users | `adopter1`, `adopter2` (adopters) and `staff1` (role `staff`), fake data, **no passwords** |

There's no client secret and no password in the file. Keycloak generates the secret on import, and you set the test users' passwords yourself (step 3).

## 1. Choose an admin password

```powershell
cd keycloak
copy .env.example .env
```

Open `keycloak/.env` and change `change-me` to your own password. `.env` is gitignored, so it stays on your machine.

## 2. Start Keycloak

From the `keycloak` folder:

```powershell
docker compose up -d
docker compose logs -f keycloak
```

Wait for `Realm 'animal' imported` and `Listening on: http://0.0.0.0:8080`, then press Ctrl+C (Keycloak keeps running). Check it in a browser: `http://localhost:8080/realms/animal` should show some JSON.

The admin console is `http://localhost:8080/admin` (user `admin`, the password from `.env`).

## 3. Set the test users' passwords

In the admin console, switch to the **animal** realm (top-left menu), then for each of `adopter1`, `adopter2` and `staff1`:

**Users** → click the user → **Credentials** → **Set password** → type a password → turn **Temporary** off → **Save**.

Or from PowerShell in the `keycloak` folder (replace the passwords with your own):

```powershell
docker compose exec keycloak /opt/keycloak/bin/kcadm.sh config credentials --server http://localhost:8080 --realm master --user admin --password <your admin password>
docker compose exec keycloak /opt/keycloak/bin/kcadm.sh set-password -r animal --username adopter1 --new-password <password>
docker compose exec keycloak /opt/keycloak/bin/kcadm.sh set-password -r animal --username adopter2 --new-password <password>
docker compose exec keycloak /opt/keycloak/bin/kcadm.sh set-password -r animal --username staff1 --new-password <password>
```

## 4. Give the client its secret

In the admin console: **Clients** → **animal-client** → **Credentials** → copy the **Client secret**. Then, from the repo root:

```powershell
dotnet user-secrets set "Authentication:ClientSecret" "<paste the secret>" --project src/Animal.Client
```

This saves it outside the repo (in your user profile). Never put it in `appsettings*.json`. To check it's there: `dotnet user-secrets list --project src/Animal.Client`.

## 5. Run the app

The API and client development settings already point at Keycloak:

| Project | Setting | Value |
|---|---|---|
| `Animal.API` | `Authentication:Authority` | `http://localhost:8080/realms/animal` |
| `Animal.API` | `Authentication:Audience` | `animal-api` |
| `Animal.Client` | `Authentication:Authority` | `http://localhost:8080/realms/animal` |
| `Animal.Client` | `Authentication:ClientId` | `animal-client` |
| `Animal.Client` | `Authentication:ClientSecret` | user-secrets (step 4) |

Two terminals, from the repo root:

```powershell
dotnet run --project src/Animal.API
dotnet run --project src/Animal.Client
```

Open `http://localhost:5012`, click **Sign in**, and sign in as `adopter1`. You come back to the pets page signed in.

## 6. Check that it works

| Check | Expected |
|---|---|
| Sign in as `adopter1`, open **My profile**, save it | Saved, and it's still there after a refresh |
| As `adopter1`, try to edit a pet or open **Review applications** | Refused (403), because adopters don't have the `staff` role |
| Sign out, sign in as `staff1` | Editing pets and reviewing applications works |
| Click **Sign out** | Back on the home page, signed out. Protected pages ask you to sign in again |

## Day to day

| Task | Command (in `keycloak/`) |
|---|---|
| Stop Keycloak (keeps users and passwords) | `docker compose stop` |
| Start it again | `docker compose start` |
| Reset everything to the realm file | `docker compose down`, then `docker compose up -d`, then redo steps 3 and 4 (new secret) |

`docker compose down` deletes the container and its data, including the passwords and the client secret. Use `stop` and `start` normally.

## Troubleshooting

| Problem | Fix |
|---|---|
| Clicking Sign in shows a 503 "Set Authentication:Authority and Authentication:ClientId" | You're not running in Development, or the client settings are missing. Use `dotnet run` (Development is the default profile) |
| Keycloak says `invalid_client` or "Invalid client credentials" | The user-secret is missing or old. Redo step 4 |
| Keycloak says "Invalid parameter: redirect_uri" | The client isn't on `http://localhost:5012` or `https://localhost:7009`. Use one of those |
| The API answers 401 to a signed-in user | Keycloak isn't running, or the API's `Authority` or `Audience` was changed |
| The API answers 403 to a staff action | The user doesn't have the `staff` role (Users → user → Role mapping) |
| Port 8080 is already used | Stop the other program, or change the left side of `"8080:8080"` and every `localhost:8080` setting |

## Tests

The automated tests don't use Keycloak. They make their own tokens signed with a test key (`TestTokenFactory`), so they run anywhere, including CI, without Docker or secrets. The real Keycloak is only for running the app by hand and for the manual checks (WI-10).

## Verification record (WI-2)

Done on 2026-11-10 with Keycloak 26.0.7 and a fresh import of `keycloak/animal-realm.json`:

| Check | Result |
|---|---|
| Realm import | `Realm 'animal' imported`, client secret generated (32 characters), 3 users, `staff1` has `staff` |
| Password grant on `animal-client` | refused: `unauthorized_client` |
| Sign-in request without PKCE | refused: `Missing parameter: code_challenge_method` |
| Browser sign-in through the Blazor client (`adopter1`, `staff1`) | back on `/pets` signed in. The client uses pushed authorization requests (PAR), so PKCE is sent on the back channel |
| Access token from that sign-in | `iss` = `http://localhost:8080/realms/animal`, `aud` contains `animal-api`, `azp` = `animal-client`, `scope` lists the API scopes, `roles` contains `staff` only for `staff1` |
| `adopter1` token (asked for `pets.write` and `adoptions.review`) on the real API | `POST /api/pets` 403, review queue 403 |
| `staff1` token on the real API | `POST /api/pets` 201, review queue 200 |
| `adopter1` profile on the real API | `GET /me` 404, `PUT /me` 204, `GET /me` 200 |
| No token, garbage token, token with a changed signature | 401 |
| Sign out | auth cookies removed, back on `/` |
