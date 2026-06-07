# 2026-06-07 Apache-2.0 License Unification

## Goal

Unify NanoBot.net with the Nong CLI and GroundPA licensing baseline.

## Changes

- Replaced the repository `LICENSE` file with the Apache License 2.0 standard text.
- Updated README and Chinese README license badges and license sections from MIT to Apache-2.0.
- Added `PackageLicenseExpression` metadata to the CLI and Core projects.

## Verification

```text
dotnet build
Result: passed, 0 warnings, 0 errors

dotnet test
Result: passed, 77 passed, 0 failed, 0 skipped

rg -n "MIT" README.md README.zh-CN.md LICENSE Nanobot.CLI Nanobot.Core Nanobot.Tests
Result: no matches
```
