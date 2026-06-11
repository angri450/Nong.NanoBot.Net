# 2026-06-07 P7 Agent Layer Nong Bridge Result

## Goal

Make Nong.NanoBot.Net the current Nong.Toolkit.Net agent-runtime main line by adding a controlled bridge from the agent tool layer to the deterministic Nong CLI.

## Changes

- Added `run_nong` as a built-in NanoBot tool.
- Added `tools.nong` configuration for command path, JSON mode, timeout, output cap, and root-command allowlist.
- Registered `run_nong` from the CLI agent setup path when enabled.
- Updated `onboard` default config with Nong bridge settings.
- Updated English and Chinese README status, config, architecture, safety, and verification counts.
- Added tests for argument-array execution, `--json` handling, allowlist rejection, empty allowlist rejection, workspace boundary enforcement, and config binding.

## Problems Encountered

1. Problem:
   `AllowedRoots` initially had defaults directly on the config model.
   Evidence:
   Microsoft configuration binding can append indexed config values to an existing list, which would make custom allowlists unexpectedly include defaults.
   Cause:
   Defaults belonged in the runtime tool, not in the bindable config object.
   Resolution:
   Changed `NongToolSettings.AllowedRoots` to nullable and moved the default published-root allowlist into `NongTool`. Added `AppConfig_BindsCustomNongAllowedRootsWithoutDefaultAppend`.

2. Problem:
   `List<string>?` and `string[]` could not be combined with `??`.
   Evidence:
   `dotnet build` failed with CS0019.
   Cause:
   Different compile-time collection types.
   Resolution:
   Typed the default allowlist as `IReadOnlyList<string>`.

3. Problem:
   Explicit empty allowlist originally implied all roots allowed.
   Evidence:
   Code path treated count zero as open access.
   Cause:
   That behavior was inherited from a permissive allowlist pattern but was wrong for a high-risk CLI bridge.
   Resolution:
   Empty allowlist now rejects all roots. Added `NongTool_RejectsAllRootsWhenAllowlistIsExplicitlyEmpty`.

## Verification

```text
dotnet build
Result: passed, 0 warnings, 0 errors

dotnet test
Result: passed, 77 passed, 0 failed, 0 skipped

rg -n "TODO|stub|NotImplementedException" Nanobot.Core Nanobot.CLI Nanobot.Tests
Result: no matches
```

## Runtime Direction

Nong.NanoBot.Net is the current main line for Nong.Toolkit.Net agent-runtime development because it already has the strongest local baseline: agent loop, providers, streaming, tools, memory, Dream, MCP, gateway, channels, heartbeat, and tests.

soloncode.net, GenericAgent.net, PilotDeck.net, and other runtime candidates remain comparison lines. They should be audited against the same matrix before being promoted over Nong.NanoBot.Net.

## Remaining Risks

- The tests verify the NanoBot-side adapter boundary with a fake Nong runner, not real Nong command semantics.
- A later integration gate should run `run_nong` against a clean published Nong CLI.
- Default allowlist intentionally excludes unverified or unpublished roots.

## Next Steps

- Add an agent-runtime comparison matrix for Nong.NanoBot.Net, soloncode.net, GenericAgent.net, and PilotDeck.net.
- Add real `nong commands --json` integration smoke once Nong working tree is clean or a published binary is available.
