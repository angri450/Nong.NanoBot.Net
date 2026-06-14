<div align="center">

# Nong.NanoBot.Net

**A typed .NET 8 personal-agent runtime for local automation, chat gateways, tools, memory, MCP, and multi-provider LLM routing.**

[中文说明](README.zh-CN.md) · [Releases](https://github.com/angri450/Nong.NanoBot.Net/releases) · [GitHub](https://github.com/angri450/Nong.NanoBot.Net)

![.NET 8](https://img.shields.io/badge/.NET-8-6d28d9?style=for-the-badge)
![C# 12](https://img.shields.io/badge/C%23-12-2563eb?style=for-the-badge)
![Tests](https://img.shields.io/badge/tests-136%20passed-16a34a?style=for-the-badge)
![Build](https://img.shields.io/badge/build-0%20warnings%20%2F%200%20errors-16a34a?style=for-the-badge)
![License](https://img.shields.io/badge/license-Apache--2.0-374151?style=for-the-badge)

</div>

## What It Is

Nong.NanoBot.Net is an independent .NET 8 personal-agent runtime for local automation, tool execution, memory, chat gateways, MCP, and multi-provider LLM routing. It is designed as a compact but production-shaped foundation for building local-first agents in the C# ecosystem.

The project keeps a simple operating model: local config, local workspace, local memory, direct tool execution, and no mandatory cloud control plane. It was originally informed by lightweight personal-agent systems such as [HKUDS/nanobot](https://github.com/HKUDS/nanobot), but Nong.NanoBot.Net now evolves as its own runtime rather than a line-by-line port.

The current codebase is a mature integration-ready baseline: agent loop, provider routing, streaming, tools, memory write path, Dream consolidation, MCP stdio/HTTP/SSE, cron, heartbeat, WebSocket gateway, and multiple chat-channel adapters are implemented and covered by tests.

It is not a hardened public multi-tenant service yet. Treat it as a strong personal-agent runtime and internal integration base.

## Current Status

| Area | Status | Notes |
| --- | --- | --- |
| Agent loop | Complete | Multi-round tool calls, streaming, hooks, session isolation, runtime events |
| Providers | Complete | OpenAI-compatible, Anthropic, Azure OpenAI, fallback chain |
| Streaming | Complete | OpenAI-compatible, Anthropic SSE, Azure OpenAI SSE |
| Memory | Complete | `MEMORY.md`, `SOUL.md`, `USER.md`, writable memory, `remember` tool, `history.jsonl` |
| Dream | Complete | Periodic history consolidation into durable Markdown memory |
| MCP | Complete | stdio, streamable HTTP, SSE endpoint discovery, `tools/list`, `tools/call` |
| Channels | Complete baseline | Telegram plus Slack, Discord, Feishu HTTP callback / REST adapters |
| Gateway | Complete | CLI, WebSocket gateway with token auth, chat gateway with cron |
| WebUI | P2 usable | Chinese-first browser workbench, streaming chat, persisted sessions, workspace file tree, tool-call details, dark/light themes |
| Plugins | Hardened | Nong.Toolkit.Net marketplace install now maps full bundle or single-skill plugins into `workspace/skills` and keeps shared references available |
| Windows MSI | P5 baseline | Per-user MSI, self-contained CLI/WebUI payload, Start Menu shortcuts, user PATH entry |
| Heartbeat | Complete | `HEARTBEAT.md` active task detection and gateway startup wiring |
| Tools | Complete | Files, shell, Nong CLI bridge, web, weather, stocks via CSV API, GitHub, summarize, memory |
| Safety | Complete baseline | SSRF guard, workspace-bounded shell, structured tool errors |
| CI/release | Complete | Build/test workflows, integration workflow, tag release workflow |

## Quick Start

```bash
# 1. Prerequisite: .NET 8 SDK
git clone https://github.com/angri450/Nong.NanoBot.Net.git
cd Nong.NanoBot.Net

# 2. Create ~/.nanobot/config.json and ~/.nanobot/workspace
dotnet run --project Nanobot.CLI -- onboard

# 3. Add a SiliconFlow API key to ~/.nanobot/secrets.json
#    or export SILICONFLOW_API_KEY

# 4. Start interactive chat
dotnet run --project Nanobot.CLI
```

Workspace layout:

```text
~/.nanobot/
  config.json
  cron.json
  workspace/
    SOUL.md
    USER.md
    HEARTBEAT.md
    memory/
      MEMORY.md
      history.jsonl
      .dream_cursor
    skills/
      my-skill/
        SKILL.md
```

## Commands

| Command | Purpose |
| --- | --- |
| `dotnet run --project Nanobot.CLI` | Interactive chat mode |
| `dotnet run --project Nanobot.CLI -- chat` | Explicit interactive chat mode |
| `dotnet run --project Nanobot.CLI -- agent -m "..."` | Single-turn agent run |
| `dotnet run --project Nanobot.CLI -- gateway` | Start enabled chat channels, cron, Dream, heartbeat |
| `dotnet run --project Nanobot.CLI -- websocket` | Start WebSocket agent gateway |
| `dotnet run --project Nanobot.CLI -- web` | Start the local WebUI and open the default browser |
| `dotnet run --project Nanobot.CLI -- serve` | Start the local WebUI server without opening a browser |
| `dotnet run --project Nanobot.Web` | Start the local browser workbench |
| `dotnet run --project Nanobot.CLI -- onboard` | Create default config, model catalog, secrets, and workspace scaffold |

## WebUI

`Nanobot.Web` is a local browser workbench for using the runtime visually. It exposes runtime status, streaming session chat, persisted WebUI sessions, workspace file browsing and preview, server-sent tool events with detail inspection, and memory preview through an ASP.NET Core backend and a static HTML/CSS/JS frontend.

```bash
dotnet run --project Nanobot.Web --urls http://127.0.0.1:8788
```

The CLI wrapper starts the same WebUI runtime and is the command used by the Windows installer:

```bash
dotnet run --project Nanobot.CLI -- web
dotnet run --project Nanobot.CLI -- serve --urls http://127.0.0.1:8788
```

The normal command requires the .NET 8 SDK and a compatible ASP.NET Core runtime. On a machine without a compatible runtime, run it self-contained:

```bash
dotnet run --project Nanobot.Web --self-contained -r win-x64 --urls http://127.0.0.1:8788
```

The workbench still loads when provider configuration is incomplete. It reports the runtime error in the status panel and enables chat after `~/.nanobot/config.json` or provider environment variables are configured and the WebUI is restarted.

Current WebUI behavior:

- Chinese is the default UI language, with an optional English toggle.
- Dark and light themes are both supported from the header toggle.
- The left sidebar includes a SiliconFlow model settings panel. It saves API keys to local `~/.nanobot/secrets.json`, reloads the runtime, and keeps secrets out of the repository.
- Chat uses the runtime streaming path, renders partial assistant output as it arrives, and persists the latest assistant content/reasoning so reload keeps the same result.
- WebUI sessions are persisted under `~/.nanobot/workspace/.webui/sessions.json`.
- Interrupted or failed streamed turns now leave a durable assistant-side stop/error message in the persisted session instead of a user-only dangling turn.
- Runtime-event replay for the tool timeline now uses durable sequence-based SSE ids, so browser reconnects can resume from `Last-Event-ID` without replay/live id mismatch.
- The tool timeline is now scoped to the active session and deduplicated by runtime sequence, so reconnects and multi-session activity do not mix foreign events into the current chat view.
- Workspace file browsing is restricted to `~/.nanobot/workspace`; internal `.webui` files are hidden.
- Tool calls are shown in a live event timeline with a detail panel for run/session/tool/error/content fields.
- The system status panel degrades gracefully when `nong` is missing or returns unexpected output; the workbench still loads and reports unavailable status instead of failing the page.

## Windows MSI

Nong.NanoBot.Net can be packaged as a Windows x64 MSI without WebView2, Electron, or a resident browser shell. The MSI installs the self-contained CLI and WebUI runtime, creates Start Menu shortcuts, and adds `nanobot.exe` to the current user's PATH. Nong.Toolkit.Net and Nong.Cli.Net are still installed later through the plugin/bootstrap path; they are not bundled into the MSI payload. The current runtime plugin installer understands the Nong.Toolkit.Net marketplace layout, so `nong-toolkit` and individual skills such as `word` install into `~/.nanobot/workspace/skills` with shared references preserved.

Build a local MSI:

```powershell
.\eng\package-msi.ps1 -Version 0.1.0 -Configuration Release -RuntimeIdentifier win-x64
```

The generated package is written to:

```text
artifacts/installer/NanoBot-0.1.0-win-x64.msi
```

After installation:

```powershell
nanobot onboard
nanobot web
nanobot serve --urls http://127.0.0.1:8788
```

## Configuration

Default onboarded profile:

`nanobot onboard` now seeds SiliconFlow as the default local profile. The distributable first-run path does not seed alternate provider presets.

Minimal SiliconFlow config:

You can either fill this from the WebUI model settings panel or edit `~/.nanobot/config.json` and `~/.nanobot/secrets.json` manually.

```json
{
  "agents": {
    "defaults": {
      "provider": "siliconflow",
      "model": "siliconflow::nex-agi/Nex-N2-Pro",
      "fallbackModels": ["siliconflow::nex-agi/Nex-N2-Pro"]
    }
  },
  "streaming": {
    "enabled": true
  }
}
```

`~/.nanobot/secrets.json`:

```json
{
  "siliconflow": {
    "apiKey": "sk-..."
  }
}
```

MCP stdio / HTTP / SSE:

```json
{
  "tools": {
    "mcpServers": {
      "local-files": {
        "transport": "stdio",
        "command": "npx",
        "arguments": ["-y", "@modelcontextprotocol/server-filesystem", "C:/work"]
      },
      "remote-http": {
        "transport": "streamableHttp",
        "url": "https://mcp.example.com/mcp",
        "headers": {
          "Authorization": "Bearer ${REMOTE_MCP_TOKEN}"
        }
      },
      "remote-sse": {
        "transport": "sse",
        "url": "https://mcp.example.com/sse"
      }
    }
  }
}
```

Nong CLI bridge:

```json
{
  "tools": {
    "nong": {
      "enabled": true,
      "command": "nong",
      "appendJson": true,
      "timeoutMs": 120000,
      "maxOutputChars": 20000,
      "allowedRoots": [
        "commands",
        "word",
        "inspect",
        "chart",
        "excel",
        "diagram",
        "genre",
        "icons",
        "skill",
        "pptx",
        "ocr",
        "pdf"
      ]
    }
  }
}
```

Gateway channels:

```json
{
  "gateway": {
    "heartbeat": {
      "enabled": true,
      "intervalSeconds": 1800
    },
    "webSocket": {
      "prefix": "http://localhost:8765/ws/",
      "token": "local-dev-token"
    }
  },
  "channels": {
    "telegram": {
      "enabled": true,
      "token": "123456:telegram-token"
    },
    "slack": {
      "enabled": true,
      "token": "xoxb-...",
      "endpoint": "http://localhost:8781/slack/"
    },
    "discord": {
      "enabled": true,
      "token": "discord-bot-token",
      "endpoint": "http://localhost:8782/discord/"
    },
    "feishu": {
      "enabled": true,
      "appId": "cli_...",
      "appSecret": "...",
      "endpoint": "http://localhost:8783/feishu/"
    }
  }
}
```

## Runtime Architecture

```text
CLI / Chat Gateway / WebSocket Gateway
        |
      Agent
        |
   AgentLoop ---- Memory + Skills + Session History
        |
   AgentRunner ---- RuntimeEventBus + Hooks
        |
  Providers: OpenAI-compatible / Anthropic / Azure / Fallback
  Tools: built-ins + memory + MCP adapters
  Services: Cron + Dream + Heartbeat
```

Important implementation points:

- `ProviderConfigurationFactory` resolves config, environment overrides, model IDs, API model IDs, provider capabilities, and fallback chain selection.
- `AgentRunner` executes tool-call loops up to 20 iterations and caps tool output at 15,000 characters.
- `FileMemoryStore` reads and writes durable memory files and appends per-session history to `history.jsonl`.
- `DreamConsolidator` uses the selected LLM provider to fold new history into `MEMORY.md`.
- `McpClientFactory` chooses stdio, streamable HTTP, or SSE transport from config.
- `NongTool` exposes `run_nong` as an argument-array tool, resolves working directories inside the workspace, applies a root-command allowlist, and appends `--json` by default.
- `NetworkSecurityGuard` blocks loopback, private, link-local, CGNAT, multicast, broadcast, and unsafe IPv6 ranges.

## Environment Variables

| Variable | Purpose |
| --- | --- |
| `SILICONFLOW_API_KEY` | SiliconFlow OpenAI-compatible provider API key |
| `SILICONFLOW_API_BASE` | Override SiliconFlow base URL, default `https://api.siliconflow.cn/v1/` |
| `SILICONFLOW_MODEL` | Override SiliconFlow default model, default `nex-agi/Nex-N2-Pro` |
| `NANOBOT_STREAMING` | `1`, `true`, or `yes` enables streaming |
| `BRAVE_API_KEY` | Web search API key |
| `GITHUB_TOKEN` | GitHub tool token |
| `NANOBOT_WS_PREFIX` | WebSocket listener prefix |
| `NANOBOT_WS_TOKEN` | WebSocket auth token |

## Testing

```bash
dotnet test
dotnet build

# Real integration tests need credentials.
NANOBOT_RUN_INTEGRATION_TESTS=1 SILICONFLOW_API_KEY=... dotnet test --filter RealIntegrationTests
```

Current local verification:

| Check | Result |
| --- | --- |
| `dotnet test` | 136 passed, 0 failed, 0 skipped |
| WebUI SiliconFlow settings smoke (2026-06-14) | `/api/settings/model` 200, active provider `siliconflow`, active model `nex-agi/Nex-N2-Pro`, available providers `1` |
| `dotnet build` | 0 warnings, 0 errors |
| WebUI API smoke (2026-06-14) | `/api/runtime/status` 200, `/api/system/status` 200, `/api/sessions` 200, `/api/gitcode/auth/status` 404, live `nong.commandCount = 126` |
| WebUI browser smoke (2026-06-14) | Desktop and narrow layouts load with runtime `就绪`, `providerOptions = 1`, send enabled, and no console/runtime exceptions |
| Source audit | 0 TODO, 0 stub, 0 `NotImplementedException` |

## Safety Boundary

Nong.NanoBot.Net has practical guardrails, not a complete hostile-user sandbox.

- Shell execution is bounded to the configured workspace.
- Nong execution uses argument arrays rather than shell command strings and rejects working directories outside the workspace.
- HTTP fetches are checked for SSRF before requests and redirects.
- WebSocket auth uses constant-time token comparison.
- Tool errors are returned as structured JSON.
- Public deployment still needs stronger authentication, authorization, rate limits, observability, and secret management.

## License

Apache-2.0. Inspired by lightweight personal-agent projects, built as an independent .NET runtime.
