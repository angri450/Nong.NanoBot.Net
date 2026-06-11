# 2026-06-07 .NET 8 Retarget And Independent Positioning

## Goal

Align Nong.NanoBot.Net with the Nong CLI baseline and reposition the repository after leaving the GitHub fork network.

## Changes

- Retargeted Nong.NanoBot.Net projects from `net10.0` to `net8.0`.
- Updated README and Chinese README from .NET 10 / C# 14 to .NET 8 / C# 12.
- Rewrote the README positioning from rebuild/fork wording to independent .NET personal-agent runtime wording.
- Kept Chinese README as the detailed Chinese entry while the repository description moves to English.
- Updated `CLAUDE.md` to reflect the .NET 8 baseline.

## Verification

```text
dotnet test
Result: passed, 77 passed, 0 failed, 0 skipped, target net8.0

dotnet build
Result: passed, 0 warnings, 0 errors, target net8.0
```

## Note

The first `dotnet build` was run in parallel with `dotnet test` and failed because the test process held `Nanobot.Tests.deps.json`. A sequential rerun passed. This was a file-lock race, not a .NET 8 compatibility failure.
