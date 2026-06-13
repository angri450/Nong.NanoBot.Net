# CLAUDE.md

This repository is **Nong.NanoBot.Net**, the .NET 8 / C# 12 implementation of the personal-agent runtime for local automation, tools, memory, MCP, chat gateways, and multi-provider LLM routing. The upstream Python `nanobot` repository is a reference, not the code being edited here.

## Current Baseline

- Latest local commit: `a4fd99e Complete P6 parity features`
- Core status: mature local personal-agent runtime and internal integration baseline
- Verification:
  - `dotnet test`: 71 passed, 0 failed, 0 skipped
  - `dotnet build`: 0 warnings, 0 errors
  - Source audit: 0 TODO, 0 stub, 0 `NotImplementedException`

## Application Layer Plans (2026-06-13)

See `log/plans/2026-06-13-nanobot-application-layer-plan.md` for the full construction roadmap.

Upstream dependencies are now ready:
- **Nong.Cli.Net 4.1.0**: modular (7 packages), `nong commands --format openai-tools` (125 tools)
- **Nong.Toolkit.Net 4.1.0**: 16 skills synced, modular architecture docs updated

Next: Phase 2 — AgentLoop tool auto-registration + Skill route matching.

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
- Keep changelog entries under `changelog/` for phase-level work.
- Do not edit or depend on the upstream Python `nanobot` repo unless explicitly asked.
