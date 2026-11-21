# Cross-Device Manual Check (WI-10)

**Status: draft.** The profile part can be checked now. The picture steps (marked *after WI-5/WI-6*) are added once the picture endpoints (#37) and the picture page (#38) are merged.

This is the manual version of the spec's cross-device journey, with the real Keycloak. The same journey is automated at the API level in `CrossDeviceProfileTests` and at the client level in `ClientSignOutTests`, which run in CI without Keycloak.

**What it proves:** the profile follows the **account**, not a copied session. Each "device" signs in by itself, and nothing (no token, cookie or link) is moved from one to the other.

## Setup

1. Keycloak, the API and the client are running (`identity-provider-setup.md`, steps 1 to 5).
2. Use two separate browsers, or one normal window and one private window. They don't share cookies, so they act like two devices.
   - **Device A:** the normal window
   - **Device B:** the private window
3. Test users: `adopter1` (the owner) and `adopter2` (the other user).

## Steps

| # | Device | Do | Expected | Screenshot |
|---|---|---|---|---|
| 1 | A | Sign in as `adopter1`, open **My profile**, save a name (for example "Ada on laptop") | Saved | `01-a-profile-saved.png` |
| 2 | B | Sign in as `adopter1` (type the password, don't copy anything from A), open **My profile** | Shows "Ada on laptop" | `02-b-sees-profile.png` |
| 3 | A | Change the name to "Ada updated" and save | Saved | |
| 4 | B | Refresh **My profile** | Shows "Ada updated" | `04-b-sees-update.png` |
| 5 | A | *After WI-5/WI-6:* upload a PNG profile picture | Picture shows on A | `05-a-picture.png` |
| 6 | B | *After WI-5/WI-6:* refresh **My profile** | Same picture shows on B | `06-b-sees-picture.png` |
| 7 | B | Sign out, sign in as `adopter2`, open **My profile** | `adopter2`'s own profile (or "no profile yet"), never Ada's name or picture | `07-other-user.png` |
| 8 | B | Open `http://localhost:5015/api/users/1` (Ada's id) | 404 or 401. There's no route that takes a user id | |
| 9 | A | Click **Sign out** | Back on the home page, signed out | |
| 10 | A | Open **My profile** | "Sign in to see your profile" | `10-a-signed-out.png` |
| 11 | B | (Sign back in as `adopter1` if needed) refresh **My profile** | Still works: signing out on A didn't touch B | |
| 12 | A | Click **Sign in** | Keycloak asks for the password again | |

Save the screenshots in `docs/milestone-4/screenshots/cross-device/` and link them in the milestone PR (#44).

## Things to look for along the way

- No token appears in any URL, page or link (check the address bar and View Source).
- In the browser's dev tools (Application → Cookies), the `.AspNetCore.Cookies` cookie is `HttpOnly` and short (a session key, not a token).

## Results

| Date | Who | Steps passed | Notes |
|---|---|---|---|
| | | | |
