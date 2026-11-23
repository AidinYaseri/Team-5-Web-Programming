Closes #37

## Summary of changes
- Added authenticated profile picture endpoints for current user:
  - `PUT /api/users/me/picture`
  - `GET /api/users/me/picture`
  - `DELETE /api/users/me/picture`
- Enforced WI-4 profile picture rules before decode via `ProfilePictureRules.EnsureValid(contentType, bytes)`.
- Added metadata-first dimension validation using `SKCodec`; rejects images with width or height > 2048.
- Kept decode + re-encode pipeline for normalized storage (PNG/JPEG only).
- Added request and file limit behavior aligned to contract:
  - >2MB file -> 400
  - >3MB request -> 413
- Ensured rejected uploads do not modify existing stored picture.
- Added `hasPicture` to `GET /api/users/me` response.
- Added lightweight picture existence query in persistence layer to avoid loading BLOB bytes for `hasPicture`.
- Replaced ImageSharp with `SkiaSharp` to avoid paid licensing requirements in this school project.

## Test results
- `dotnet test Team-5-Web-Programming.slnx --configuration Release --filter "FullyQualifiedName~UsersEndpointTests"` -> Passed
- `dotnet test Team-5-Web-Programming.slnx --configuration Release` -> Passed
- `dotnet build Team-5-Web-Programming.slnx --configuration Release` -> Passed

## Checklist
- [x] GIF upload rejected (400)
- [x] JPEG sent as `image/png` rejected (400)
- [x] Oversized dimensions (>2048x2048) rejected (400)
- [x] File over 2MB rejected (400)
- [x] Request over 3MB rejected (413)
- [x] Rejected uploads keep previous saved picture unchanged
- [x] `GET /api/users/me` includes `hasPicture`
- [x] Auth behavior covered (`401` no token, `403` wrong scope)

## AI usage disclosure
- GitHub Copilot was used to help draft and implement code/test updates.
- All generated changes were manually reviewed, adjusted, and validated with build/tests before completion.
