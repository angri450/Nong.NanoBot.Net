# Nong.NanoBot.Net Project State

Last updated: 2026-06-13

This file is the current truth source for agents. Read it before `AGENTS.md`, `CLAUDE.md`, `agent.md`, `README.md`, `DEVELOPMENT_PLAN.zh-CN.md`, or any file under `log/`.

## Current Work

Active plan/handoff:

- None.

The 2026-06-13 NanoBot application-layer plan is complete through Phase 10. There is no active construction plan. If new work is needed, the planner window must create or update a plan under `log/plans/`, update `log/plans/index.md`, and then update this section before a builder window starts.

Do not treat `DEVELOPMENT_PLAN.zh-CN.md` or old `log/plans/*` files as active unless this file links to them.

## Current Role

Nong.NanoBot.Net is the .NET 8 local-first agent runtime for Nong.

It owns runtime orchestration, provider routing, memory, MCP, bounded tool execution, Nong bridge, plugin installation, WebUI status surfaces, and future distribution work.

External GUI/runtime repositories are references unless the user explicitly changes the main implementation target.

## Current Baseline

Latest observed local commit:

- `787a7cb feat: Phase 10 — end-to-end verification spec, 10/10 phases complete, application layer done`

Current application-layer status:

- Phase 1-3: Nong allowlist and command/tool registration complete.
- Phase 4-6: skill routing, context management, and confirmation hook complete.
- Phase 7: plugin install/list tooling complete.
- Phase 8-9: WebUI Nong status panel and model/status improvements complete.
- Phase 10: end-to-end verification spec complete.

Last recorded code-side gate from `log/changelog/2026-06-13-phase-10.md`:

- `dotnet build Nanobot.slnx`: 0 errors
- `dotnet test`: 102 passed

Real provider-backed end-to-end LLM verification remains environment-dependent and should be run only when credentials and model access are available.

## Nong Integration Contract

NanoBot discovers Nong tools from `nong commands --format openai-tools`.

Current sibling workspace state says `Nong.Cli.Net` exposes 126 commands/tool schemas. Older NanoBot logs use `125+` as a threshold. Verify live command count before changing Nong bridge behavior.

Nong bridge must keep:

- argument-array execution;
- workspace boundaries;
- root command allowlist;
- timeouts;
- output caps;
- structured errors;
- confirmation gates for install, token, and write actions.

## Planning Workflow

Development plans live in `log/plans/`.

Only the plan linked above is active for a builder window. Older plans remain as history and must not be scanned to infer current work.

Two-window workflow:

- Planner window: reads history as needed, writes or updates `log/plans/YYYY-MM-DD-topic.md`, updates `log/plans/index.md`, then updates this file's active plan pointer.
- Builder window: reads this file and the active plan only, then implements and verifies.

Detailed policy:

- `docs/wiki/planning-workflow.md`
- `log/plans/README.md`

## Current Architecture

Main components:

```text
Nanobot.Core
  agent loop, providers, tools, memory, MCP, Nong bridge, plugins

Nanobot.CLI
  CLI entry points and local operation

Nanobot.Web
  WebUI workbench, runtime status, Nong status, model/plugin surfaces

Nanobot.Tests
  unit and integration-oriented verification
```

WebUI remains browser-first for now. Do not introduce Electron or WebView2 desktop shells unless the project direction is explicitly changed.

## Current Risks

- Real LLM end-to-end tests require local credentials and model access.
- Older docs mention earlier P6/P7/P8 plans; check this file before treating them as active.
- Nong command count can drift as `Nong.Cli.Net` evolves.
- Do not commit API keys, OAuth tokens, local auth files, or generated publish artifacts.

## Information Sources

Use this order:

1. `PROJECT_STATE.md` for current truth.
2. `AGENTS.md`, `CLAUDE.md`, and `agent.md` for agent behavior.
3. The active plan linked above, if any.
4. `docs/wiki/` for stable project knowledge.
5. `log/` only as historical evidence.
6. Legacy root `changelog/` only when explicitly researching older history.

Never bulk-read `log/` to decide current work.

## Verification Baseline

For meaningful code changes:

```powershell
dotnet build Nanobot.slnx
dotnet test
```

For WebUI changes, also check desktop and narrow layouts when feasible.
