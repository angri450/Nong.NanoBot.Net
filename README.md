# NanoBot.net

**NanoBot.net** is a .NET 10 personal-agent runtime inspired by [HKUDS/nanobot](https://github.com/HKUDS/nanobot). It keeps the small CLI-first workflow and adds a structured agent loop, provider registry, model-safe configuration, tool safety boundaries, streaming output, MCP tool adaptation, and lightweight gateways.

[中文说明](README.zh-CN.md)

---

## Status

NanoBot.net is now an integration-ready development baseline. It is suitable for local agent workflows, internal testing, provider integration, and release packaging. It is not yet a fully hardened public multi-tenant service.

| Area | Status |
|---|---|
| Build and tests | `dotnet build` clean, `dotnet test` passing |
| Agent runtime | `AgentLoop` + `AgentRunner`, hooks, events, session history |
| Configuration | Provider/model references use `providerId::modelId` |
| Providers | OpenAI-compatible, Anthropic, Azure OpenAI, registry, fallback |
| Streaming | OpenAI-compatible streaming, Agent streaming API, CLI and WebSocket deltas |
| Tools | Filesystem, shell, web search/fetch, weather, stocks, GitHub, MCP adapter |
| Safety | WebFetch SSRF guard, shell workspace boundary, timeout, output cap |
| Gateways | CLI, Telegram, authenticated WebSocket gateway |
| Delivery | GitHub Actions CI, manual real integration workflow, tag release workflow |

Current local verification:

```text
55 tests passed
0 build warnings
0 build errors
```

---

## Highlights

- **Stable model identity**: models are selected as `providerId::modelId`, while each provider can map that to a real API model id.
- **Config-driven providers**: OpenAI-compatible, Anthropic, and Azure OpenAI can be configured from `~/.nanobot/config.json`, with environment variables still taking priority.
- **Fallback chain**: the agent can try configured models in order, each bound to its own provider and API model id.
- **Streaming runtime**: OpenAI-compatible providers stream text deltas; Agent, CLI, and WebSocket gateway expose the same flow.
- **Runtime observability**: run/tool started/completed/failed events plus hook extension points.
- **Workspace context**: memory and skills are loaded from the workspace and injected into the system prompt.
- **Tool safety baseline**: web fetch blocks restricted networks and redirect SSRF; shell execution stays inside the workspace.
- **Release path**: CI, manual real integration tests, and multi-platform release artifacts are defined in `.github/workflows`.

---

## Requirements

- .NET 10 SDK
- An OpenAI-compatible API key for the default setup
- Optional: Brave Search API key, Telegram token, GitHub token

Initialize local config and workspace:

```bash
dotnet run --project Nanobot.CLI -- onboard
```

The default workspace is:

```text
~/.nanobot/workspace
```

---

## Configuration

The CLI reads environment variables first, then falls back to `~/.nanobot/config.json`.

### Model References

NanoBot.net uses the same stable model identity idea that mature provider registries use:

```text
providerId::modelId
```

Examples:

```text
openai::gpt-4o
openrouter::gpt-4o
anthropic::claude-sonnet-4-5
azure-openai::production-chat
```

The left side selects the provider. The right side is the configured model id. A model entry may map it to a different `apiModelId` for the provider request.

### Config File

```json
{
  "providers": {
    "openai": {
      "kind": "openai-compatible",
      "apiKey": "",
      "apiBase": null,
      "defaultModel": "gpt-4o",
      "models": [
        {
          "id": "gpt-4o",
          "apiModelId": "gpt-4o",
          "supportsStreaming": true,
          "supportsTools": true
        }
      ]
    },
    "openrouter": {
      "kind": "openai-compatible",
      "apiKey": "",
      "apiBase": "https://openrouter.ai/api/v1",
      "models": [
        {
          "id": "gpt-4o",
          "apiModelId": "openai/gpt-4o"
        }
      ]
    }
  },
  "agents": {
    "defaults": {
      "model": "openai::gpt-4o",
      "fallbackModels": [
        "openai::gpt-4o",
        "openrouter::gpt-4o"
      ]
    }
  },
  "streaming": {
    "enabled": true
  },
  "gateway": {
    "webSocket": {
      "prefix": "http://localhost:8765/ws/",
      "token": ""
    }
  },
  "webSearch": {
    "apiKey": ""
  }
}
```

### Environment Variables

| Variable | Purpose |
|---|---|
| `OPENAI_API_KEY` | Overrides the OpenAI-compatible provider API key |
| `OPENAI_API_BASE` | Overrides the OpenAI-compatible base URL |
| `OPENAI_MODEL` | Overrides the default model; accepts `provider::model` |
| `ANTHROPIC_API_KEY` | Enables the Anthropic provider |
| `ANTHROPIC_API_BASE` | Optional Anthropic-compatible base URL |
| `ANTHROPIC_MODEL` | Anthropic default model |
| `AZURE_OPENAI_API_KEY` | Enables Azure OpenAI |
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint |
| `AZURE_OPENAI_DEPLOYMENT` | Azure deployment/model name |
| `AZURE_OPENAI_API_VERSION` | Azure API version |
| `NANOBOT_STREAMING` | `1`, `true`, or `yes` enables streaming |
| `NANOBOT_WS_PREFIX` | WebSocket listener prefix |
| `NANOBOT_WS_TOKEN` | WebSocket bearer/query token |
| `BRAVE_API_KEY` | Web search backend |
| `GITHUB_TOKEN` | GitHub tool access |

---

## Usage

Interactive chat:

```bash
dotnet run --project Nanobot.CLI
```

Single message:

```bash
dotnet run --project Nanobot.CLI -- agent -m "Summarize the files in my workspace."
```

Telegram gateway:

```bash
dotnet run --project Nanobot.CLI -- gateway
```

WebSocket gateway:

```bash
dotnet run --project Nanobot.CLI -- websocket
```

When `gateway.webSocket.token` or `NANOBOT_WS_TOKEN` is set, clients must send either:

```text
Authorization: Bearer <token>
```

or:

```text
ws://localhost:8765/ws/?token=<token>
```

WebSocket requests can be plain text or JSON:

```json
{
  "message": "What changed today?",
  "sessionId": "default"
}
```

Gateway messages are JSON with type `delta`, `response`, `event`, or `error`.

---

## Workspace

| Path | Purpose |
|---|---|
| `~/.nanobot/config.json` | Local config fallback |
| `~/.nanobot/workspace/memory/MEMORY.md` | Long-term memory context |
| `~/.nanobot/workspace/skills/<name>/SKILL.md` | Skills injected into the system prompt |

---

## Architecture

```text
CLI / Telegram / WebSocket
        |
      Agent
        |
   AgentLoop  ---- Memory + Skills + Session History
        |
   AgentRunner ---- Provider + ToolRegistry
        |
  Providers / Built-in Tools / MCP Tools
```

Key pieces:

- `AgentLoop`: builds prompt context, memory, skills, history, run events.
- `AgentRunner`: handles LLM turns, streaming deltas, tool calls, and tool events.
- `ProviderConfigurationFactory`: resolves config/env into provider registry, model refs, and fallback chain.
- `ProviderRegistry`: named providers and descriptors.
- `FallbackLLMProvider`: sequential model/provider fallback.
- `RuntimeEventBus`: in-process lifecycle events.
- `IAgentHook`: extension points around runs and tools.
- `McpToolProvider`: converts MCP server tools into `ITool`.

---

## Safety

NanoBot.net includes a practical safety baseline:

- `web_fetch` only allows `http` and `https`.
- DNS results are checked before requests.
- Loopback, private, link-local, carrier-grade NAT, multicast, unspecified, and other restricted IPs are blocked.
- Redirect targets are checked before following.
- `run_shell` runs inside a configured workspace.
- Shell working directories cannot escape the workspace.
- Shell commands have timeout and output limits.
- Tool errors are returned as structured JSON.
- WebSocket gateway supports token authentication.

This is still not a complete sandbox. Do not expose the gateway to untrusted users without stronger authorization, rate limits, and deployment controls.

---

## Tests And Delivery

Run unit tests:

```bash
dotnet test
```

Build all projects:

```bash
dotnet build
```

Run real integration tests locally:

```bash
NANOBOT_RUN_INTEGRATION_TESTS=1 OPENAI_API_KEY=... dotnet test --filter FullyQualifiedName~RealIntegrationTests
```

GitHub Actions:

| Workflow | Purpose |
|---|---|
| `.github/workflows/ci.yml` | Restore, build, test on push/PR |
| `.github/workflows/integration.yml` | Manual real OpenAI + WebSocket integration smoke tests |
| `.github/workflows/release.yml` | Tag-triggered CLI publish for `win-x64`, `linux-x64`, `osx-arm64` |

---

## Known Boundaries

- Azure OpenAI currently uses API key auth, not AAD.
- Anthropic and Azure providers are non-streaming today; OpenAI-compatible streaming is implemented.
- MCP stdio support exists, but remote MCP, OAuth, reconnect, and advanced lifecycle management are not finished.
- WebSocket auth is token-based; full authorization, event filtering, and WebUI are future work.
- This is not a full Python upstream clone; session compaction, Dream memory, and every original channel are not fully ported.

---

## License

MIT
