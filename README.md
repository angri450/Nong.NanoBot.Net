<div align="center">

# Nong.NanoBot.Net

**A typed .NET 8 personal-agent runtime for local automation, chat gateways, tools, memory, MCP, and multi-provider LLM routing.**

[中文说明](README.zh-CN.md) · [Releases](https://github.com/angri450/Nong.NanoBot.Net/releases) · [GitHub](https://github.com/angri450/Nong.NanoBot.Net)

![.NET 8](https://img.shields.io/badge/.NET-8-6d28d9?style=for-the-badge)
![C# 12](https://img.shields.io/badge/C%23-12-2563eb?style=for-the-badge)
![Tests](https://img.shields.io/badge/tests-79%20passed-16a34a?style=for-the-badge)
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

# 3. Add a DMX API key to ~/.nanobot/config.json
#    or export DMX_API_KEY

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
| `dotnet run --project Nanobot.CLI -- onboard` | Create default config and workspace |

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

The normal command requires the .NET 8 SDK and ASP.NET Core Runtime 8. On a machine without the ASP.NET Core 8 runtime, run it self-contained:

```bash
dotnet run --project Nanobot.Web --self-contained -r win-x64 --urls http://127.0.0.1:8788
```

The workbench still loads when provider configuration is incomplete. It reports the runtime error in the status panel and enables chat after `~/.nanobot/config.json` or provider environment variables are configured and the WebUI is restarted.

Current WebUI behavior:

- Chinese is the default UI language, with an optional English toggle.
- Dark and light themes are both supported from the header toggle.
- The left sidebar includes a model settings panel for DMX DeepSeek V4 Pro. It saves the API key to the local `~/.nanobot/config.json` file and reloads the runtime without committing secrets to the repository.
- Chat uses the runtime streaming path and renders partial assistant output as it arrives.
- WebUI sessions are persisted under `~/.nanobot/workspace/.webui/sessions.json`.
- Workspace file browsing is restricted to `~/.nanobot/workspace`; internal `.webui` files are hidden.
- Tool calls are shown in a live event timeline with a detail panel for run/session/tool/error/content fields.

## Windows MSI

Nong.NanoBot.Net can be packaged as a Windows x64 MSI without WebView2, Electron, or a resident browser shell. The MSI installs the self-contained CLI and WebUI runtime, creates Start Menu shortcuts, and adds `nanobot.exe` to the current user's PATH. Nong.Toolkit.Net and Nong.Cli.Net are still installed later through the plugin/bootstrap path; they are not bundled into the MSI payload.

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

Minimal DMX DeepSeek V4 Pro config:

You can either fill this from the WebUI model settings panel or edit `~/.nanobot/config.json` manually.

```json
{
  "providers": {
    "dmx": {
      "kind": "openai-compatible",
      "apiKey": "sk-...",
      "apiBase": "https://www.dmxapi.cn/v1/",
      "defaultModel": "deepseek-v4-pro-guan",
      "models": [
        {
          "id": "deepseek-v4-pro-guan",
          "apiModelId": "deepseek-v4-pro-guan",
          "supportsStreaming": true,
          "supportsTools": true
        }
      ]
    }
  },
  "agents": {
    "defaults": {
      "model": "dmx::deepseek-v4-pro-guan",
      "fallbackModels": ["dmx::deepseek-v4-pro-guan"],
      "dream": {
        "enabled": true,
        "intervalHours": 6
      }
    }
  },
  "streaming": {
    "enabled": true
  }
}
```

Fallback across providers:

```json
{
  "providers": {
    "dmx": {
      "kind": "openai-compatible",
      "apiKey": "sk-...",
      "apiBase": "https://www.dmxapi.cn/v1/",
      "defaultModel": "deepseek-v4-pro-guan"
    },
    "anthropic": {
      "kind": "anthropic",
      "apiKey": "sk-ant-...",
      "defaultModel": "claude-sonnet-4-5"
    },
    "azure-openai": {
      "kind": "azure-openai",
      "apiKey": "...",
      "endpoint": "https://example.openai.azure.com",
      "deployment": "my-azure-deployment",
      "apiVersion": "2024-10-21"
    }
  },
  "agents": {
    "defaults": {
      "fallbackModels": [
        "dmx::deepseek-v4-pro-guan",
        "anthropic::claude-sonnet-4-5",
        "azure-openai::my-azure-deployment"
      ]
    }
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
| `DMX_API_KEY` | DMX OpenAI-compatible relay API key |
| `DMX_API_BASE` | Override DMX base URL, default `https://www.dmxapi.cn/v1/` |
| `DMX_MODEL` | Override DMX model, default `deepseek-v4-pro-guan` |
| `OPENAI_API_KEY` | OpenAI-compatible provider API key |
| `OPENAI_API_BASE` | Override OpenAI-compatible base URL |
| `OPENAI_MODEL` | Override default model, supports `provider::model` |
| `ANTHROPIC_API_KEY` | Enable Anthropic provider |
| `ANTHROPIC_API_BASE` | Override Anthropic base URL |
| `ANTHROPIC_MODEL` | Override Anthropic default model |
| `AZURE_OPENAI_API_KEY` | Enable Azure OpenAI provider |
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint |
| `AZURE_OPENAI_DEPLOYMENT` | Azure OpenAI deployment |
| `AZURE_OPENAI_API_VERSION` | Azure OpenAI API version |
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
NANOBOT_RUN_INTEGRATION_TESTS=1 DMX_API_KEY=... dotnet test --filter RealIntegrationTests
```

Current local verification:

| Check | Result |
| --- | --- |
| `dotnet test` | 77 passed, 0 failed, 0 skipped |
| `dotnet build` | 0 warnings, 0 errors |
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
