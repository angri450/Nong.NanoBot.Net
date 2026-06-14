# Nong.NanoBot.Net Project State

Last updated: 2026-06-13

This file is the current truth source for agents. Read it before `AGENTS.md`, `CLAUDE.md`, `agent.md`, `README.md`, `DEVELOPMENT_PLAN.zh-CN.md`, or any file under `log/`.

## Current Work

Active plan/handoff:

- `log/plans/2026-06-13-nanobot-stabilization-hardening-plan.md`

The 2026-06-13 NanoBot application-layer plan is complete through Phase 10. Current construction has moved to stabilization and usability hardening.

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

Last recorded code-side gate from `log/changelog/2026-06-13-stabilization-hardening.md`:

- `dotnet build Nanobot.slnx`: 0 warnings, 0 errors
- `dotnet test`: 130 passed
- `dotnet run --project Nanobot.Web --urls http://127.0.0.1:8800` smoke:
  - `/api/runtime/status`: 200
  - `/api/system/status`: 200
  - `/api/settings/model`: 200
  - `/api/sessions`: 200
  - `nong.commandCount`: 126
  - active provider: `siliconflow`
  - active model: `nex-agi/Nex-N2-Pro`
  - desktop and narrow browser smoke: runtime pill `就绪`, provider/model selects populated, send enabled, no console/runtime exceptions
  - normal `/api/agent/stream`: `session -> delta* -> complete`, persisted assistant content saved to WebUI session
  - aborted `/api/agent/stream` (`curl --max-time 1`): no orphan empty session, assistant-side `[已停止]` marker persisted
  - `/api/events` replay with `Last-Event-ID: 2`: returns `id: 3`, `id: 4`; sequence continues across restart and replay uses monotonically increasing SSE ids

Real provider-backed end-to-end LLM verification remains environment-dependent and should be run only when credentials and model access are available.

Latest stabilization additions also cover:

- Nong.Toolkit.Net marketplace plugin install shape (`nong-toolkit` full bundle + single-skill installs);
- shared skill-reference loading such as `../references/shared/nong-cli-preflight.md`;
- first-run workspace scaffold creation via onboarding and memory bootstrap.
- WebUI model settings and onboarding defaults now align on SiliconFlow-first local setup, while keeping DMX preset support.
- WebUI streaming turns now persist assistant reasoning/content across reloads and keep durable assistant-side stop/error messages when a run is interrupted or fails.
- WebUI runtime-event replay now persists enough metadata to restore tool/runtime timeline by sequence, and SSE replay ids stay aligned with `Last-Event-ID` semantics across restarts.
- WebUI timeline rendering now de-duplicates runtime events by sequence, scopes the visible event list to the active session, and avoids foreign-session tool notices leaking into the current chat pane.
- Frontend startup contract tests now guard `elements.*` wiring, `getElementById(...)` coverage, duplicate HTML ids, and definitions that appear after `/app.js` loads, so missing or late DOM bindings can fail CI instead of leaving the page stuck half-booted.

## Nong Integration Contract

NanoBot discovers Nong tools from `nong commands --format openai-tools`.

Current sibling workspace state says `Nong.Cli.Net` exposes 126 commands/tool schemas. Older NanoBot logs use `125+` as a threshold. Latest local status smoke on 2026-06-13 also returned `126`. Verify live command count before changing Nong bridge behavior.

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
