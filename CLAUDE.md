# CLAUDE.md

This repository is **Nong.NanoBot.Net**, the .NET 8 / C# 12 implementation of the personal-agent runtime for local automation, tools, memory, MCP, chat gateways, and multi-provider LLM routing. The upstream Python `nanobot` repository is a reference, not the code being edited here.

## Start Here

Read `PROJECT_STATE.md` before this file. It is the current truth source for active plans, current baseline, and known drift.

Do not bulk-read `log/` to decide current work. `log/` is historical archive. Only the plan linked from `PROJECT_STATE.md` is active for a builder window.

## Current Baseline

- Latest observed local commit: `787a7cb feat: Phase 10 — end-to-end verification spec, 10/10 phases complete, application layer done`
- Core status: mature local personal-agent runtime with Nong application layer complete through Phase 10
- Last recorded verification:
  - `dotnet build Nanobot.slnx`: 0 errors
  - `dotnet test`: 102 passed

## Application Layer Status (2026-06-13)

The plan in `log/plans/2026-06-13-nanobot-application-layer-plan.md` is historical/completed unless `PROJECT_STATE.md` links it again.

Current completed line:

- Phase 1-3: Nong command auto-discovery and AgentLoop tool registration
- Phase 4-6: skill routing, context management, confirmation hook
- Phase 7: PluginManager and plugin install/list tools
- Phase 8-9: WebUI Nong status panel and external tool/OCR status rendering
- Phase 10: end-to-end verification spec

## P6 Scope Completed

- Memory write path, `remember` tool, `history.jsonl`, and Dream consolidation
- MCP stdio, streamable HTTP, and SSE transports
- Multi-channel gateway baseline: Telegram, Slack, Discord, Feishu
- Anthropic and Azure OpenAI streaming
- Heartbeat gateway wiring through `HEARTBEAT.md`
- StockTool moved away from Google Finance HTML/CSS scraping to CSV quote parsing
- README, Chinese README, and P6 changelog updated

## Local Artifacts

`publish/` is a local build/release output directory. Keep it on disk if needed, but do not commit it. It is intentionally ignored by `.gitignore`.

## Working Notes

- Prefer repo-native .NET patterns and focused tests.
- Keep README and `README.zh-CN.md` synchronized when user-facing status changes.
- Keep current process records under `log/changelog/` for phase-level work.
- Do not edit or depend on the upstream Python `nanobot` repo unless explicitly asked.
