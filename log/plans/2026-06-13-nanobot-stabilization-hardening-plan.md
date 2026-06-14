# NanoBot Stabilization Hardening Plan

Date: 2026-06-13
Status: active

## Objective

Make `Nong.NanoBot.Net` materially more usable as the local-first Nong agent runtime by removing current verification drift, hardening the most visible runtime status paths, and keeping the project documentation aligned with observed evidence.

This is not a rewrite. It continues the existing .NET 8 runtime architecture and focuses on defects that make the project feel unreliable or misleading.

## Current Evidence

- `dotnet build Nanobot.slnx` succeeds but currently emits nullable warnings in:
  - `Nanobot.Core/Tools/Builtin/NongTool.cs`
  - `Nanobot.Web/Program.cs`
- The first `dotnet test` attempt failed because build and test were started concurrently and `Nanobot.Core.dll` was locked by the compiler process. Re-run tests sequentially after code changes.
- `README.md` and `README.zh-CN.md` still claim older verification counts and `0 warnings`, which contradicts the current observed build.
- `PROJECT_STATE.md` has no active construction plan, so this plan is the current handoff once linked there.

## Files To Read First

- `PROJECT_STATE.md`
- `AGENTS.md`
- `CLAUDE.md`
- `agent.md`
- `docs/wiki/architecture.md`
- This plan

Read module files only as needed after that.

## Files Expected To Change

Likely:

- `Nanobot.Core/Tools/Builtin/NongTool.cs`
- `Nanobot.Web/Program.cs`
- `Nanobot.Tests/*`
- `README.md`
- `README.zh-CN.md`
- `log/changelog/2026-06-13-stabilization-hardening.md`
- `log/changelog/index.md`

Plan/index/state files changed by the planner step:

- `log/plans/2026-06-13-nanobot-stabilization-hardening-plan.md`
- `log/plans/index.md`
- `PROJECT_STATE.md`

## Phase 1: Verification Drift Cleanup

1. Fix current nullable warnings without suppressing real null risks.
2. Add focused tests if the warning fix changes parsing or status behavior.
3. Run `dotnet build Nanobot.slnx` sequentially.
4. Run `dotnet test` sequentially.
5. Update README verification claims to match actual evidence.

Acceptance:

- Build completes with `0 warnings, 0 errors`.
- Tests complete without failures.
- README and Chinese README no longer claim stale counts.

## Phase 2: Runtime Status Hardening

1. Review WebUI system status probes for external process handling risks.
2. Avoid hangs, stale process output, and unnecessary repeated probes where practical.
3. Keep Nong CLI status, external dotnet tool status, OCR model status, and toolkit status visible.
4. Add tests for pure parsing helpers if behavior is extracted from process probes.

Acceptance:

- Status APIs remain available when `nong` is missing or returns unexpected JSON.
- The WebUI can still load when provider configuration is incomplete.
- Build/test gates remain green.

## Phase 3: Usability Audit Pass

1. Inspect the next highest-impact user path after status hardening: onboarding/config, plugin bootstrap, Nong bridge, or session chat.
2. Fix only evidence-backed defects in the selected path.
3. Update docs and changelog for user-visible behavior.

Acceptance:

- At least one real project usability defect is fixed with tests or a concrete smoke check.
- No unrelated architecture or UI rewrite is introduced.

## Verification

Required for code changes:

```powershell
dotnet build Nanobot.slnx
dotnet test
```

For WebUI changes when feasible:

```powershell
dotnet run --project Nanobot.Web --urls http://127.0.0.1:8788
```

Then check:

- `/api/runtime/status`
- `/api/system/status`
- One desktop layout and one narrow layout when frontend files change.

## Stop Conditions

- Stop and report if real provider-backed LLM verification is required but credentials/model access are unavailable.
- Stop and report before publishing, pushing, deleting remote state, or creating NuGet/MSI release artifacts unless explicitly requested.
- Stop if a needed change would require replacing the WebUI with Electron/WebView2; that conflicts with current repo direction.

## Non-Goals

- Do not copy payloads from `Nong.Toolkit.Net` or `Nong.Cli.Net` into this runtime by default.
- Do not treat old `log/plans/*` files or `DEVELOPMENT_PLAN.zh-CN.md` as active.
- Do not introduce a new parallel runtime.
- Do not commit API keys, OAuth tokens, local auth files, or generated publish artifacts.
