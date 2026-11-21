# Milestone 4 Decisions

## D7 - Profile picture image processing library

**Decision:** Use `SkiaSharp` for profile picture identify/decode/re-encode logic, instead of SixLabors ImageSharp.

**Why:**
- This is a school project and we must avoid paid/commercial-license dependencies.
- ImageSharp 4.x requires a license key/file at build time in this environment.
- SkiaSharp supports metadata-first validation (`SKCodec`) for early dimension checks and reliable PNG/JPEG re-encoding.
- Keeps endpoint behavior aligned with the contract (PNG/JPEG only, 2048x2048 max dimensions, deterministic re-encode).

**Tradeoffs considered:**
- ImageSharp 3.x can build without paid setup but has known vulnerability advisories.
- Moving to SkiaSharp adds a dependency with native runtime assets, but avoids licensing cost and keeps the project non-commercial-friendly.
