# NanoBot.net

**NanoBot.net** 是受 [HKUDS/nanobot](https://github.com/HKUDS/nanobot) 启发的 .NET 10 个人智能体运行时。它保留轻量、CLI 优先的使用方式，同时补上结构化 Agent Loop、Provider 注册表、稳定模型配置、工具安全边界、流式输出、MCP 工具适配和轻量网关。

[English README](README.md)

---

## 当前状态

NanoBot.net 现在是一个可集成、可验证、可继续发布的开发基线。它适合本地智能体工作流、内部验证、Provider 集成和 release 打包；但还不是可以直接暴露给公网的完整多租户生产服务。

| 模块 | 状态 |
|---|---|
| 构建与测试 | `dotnet build` 干净，`dotnet test` 通过 |
| Agent 运行时 | `AgentLoop` + `AgentRunner`、hooks、events、session history |
| 配置模型 | 使用 `providerId::modelId` 作为稳定模型身份 |
| Providers | OpenAI-compatible、Anthropic、Azure OpenAI、registry、fallback |
| 流式输出 | OpenAI-compatible streaming、Agent streaming API、CLI/WebSocket delta |
| 工具系统 | 文件、Shell、Web 搜索/抓取、天气、股票、GitHub、MCP 适配 |
| 安全边界 | WebFetch SSRF 防护、Shell workspace 限制、超时、输出截断 |
| 网关 | CLI、Telegram、带认证的 WebSocket gateway |
| 交付链路 | GitHub Actions CI、手动真实集成测试、tag release workflow |

当前本地验证结果：

```text
55 tests passed
0 build warnings
0 build errors
```

---

## 核心亮点

- **稳定模型身份**：模型选择统一为 `providerId::modelId`，provider 内部再映射到真实 `apiModelId`。
- **配置驱动 Provider**：OpenAI-compatible、Anthropic、Azure OpenAI 都可通过 `~/.nanobot/config.json` 配置，环境变量仍然优先。
- **Fallback Chain**：可按顺序尝试多个 provider/model，每一项都有自己的真实 API model。
- **流式运行时**：OpenAI-compatible provider 支持流式文本；Agent、CLI、WebSocket 共享同一条 streaming 路径。
- **运行时可观测**：run/tool started/completed/failed 事件和 hook 生命周期都已接入。
- **工作区上下文**：memory 和 skills 从 workspace 读取并注入系统提示词。
- **工具安全基线**：WebFetch 阻止受限网络与 redirect SSRF，Shell 限制在 workspace 内。
- **发布路径**：CI、真实集成测试、跨平台 release artifact 已进入 `.github/workflows`。

---

## 环境要求

- .NET 10 SDK
- 默认配置需要一个 OpenAI-compatible API key
- 可选：Brave Search API key、Telegram token、GitHub token

初始化本地配置和工作区：

```bash
dotnet run --project Nanobot.CLI -- onboard
```

默认工作区：

```text
~/.nanobot/workspace
```

---

## 配置

CLI 优先读取环境变量，未设置时回退到 `~/.nanobot/config.json`。

### 模型引用

NanoBot.net 使用成熟 Provider Registry 常见的稳定模型身份：

```text
providerId::modelId
```

示例：

```text
openai::gpt-4o
openrouter::gpt-4o
anthropic::claude-sonnet-4-5
azure-openai::production-chat
```

左侧选择 provider，右侧是配置中的模型 id。模型项可以通过 `apiModelId` 映射到 provider 请求时真正使用的模型名。

### 配置文件

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

### 环境变量

| 变量 | 用途 |
|---|---|
| `OPENAI_API_KEY` | 覆盖 OpenAI-compatible provider API key |
| `OPENAI_API_BASE` | 覆盖 OpenAI-compatible base URL |
| `OPENAI_MODEL` | 覆盖默认模型，支持 `provider::model` |
| `ANTHROPIC_API_KEY` | 启用 Anthropic provider |
| `ANTHROPIC_API_BASE` | 可选 Anthropic-compatible base URL |
| `ANTHROPIC_MODEL` | Anthropic 默认模型 |
| `AZURE_OPENAI_API_KEY` | 启用 Azure OpenAI |
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint |
| `AZURE_OPENAI_DEPLOYMENT` | Azure deployment/model 名称 |
| `AZURE_OPENAI_API_VERSION` | Azure API version |
| `NANOBOT_STREAMING` | `1`、`true`、`yes` 表示启用 streaming |
| `NANOBOT_WS_PREFIX` | WebSocket 监听前缀 |
| `NANOBOT_WS_TOKEN` | WebSocket bearer/query token |
| `BRAVE_API_KEY` | Web 搜索后端 |
| `GITHUB_TOKEN` | GitHub 工具访问 |

---

## 使用方式

交互聊天：

```bash
dotnet run --project Nanobot.CLI
```

发送单次消息：

```bash
dotnet run --project Nanobot.CLI -- agent -m "总结一下工作区里的文件。"
```

Telegram 网关：

```bash
dotnet run --project Nanobot.CLI -- gateway
```

WebSocket 网关：

```bash
dotnet run --project Nanobot.CLI -- websocket
```

配置 `gateway.webSocket.token` 或 `NANOBOT_WS_TOKEN` 后，客户端必须携带：

```text
Authorization: Bearer <token>
```

或：

```text
ws://localhost:8765/ws/?token=<token>
```

WebSocket 请求支持纯文本，也支持 JSON：

```json
{
  "message": "今天改了什么？",
  "sessionId": "default"
}
```

网关返回 JSON，类型包括 `delta`、`response`、`event`、`error`。

---

## 工作区

| 路径 | 作用 |
|---|---|
| `~/.nanobot/config.json` | 本地配置回退文件 |
| `~/.nanobot/workspace/memory/MEMORY.md` | 长期记忆上下文 |
| `~/.nanobot/workspace/skills/<name>/SKILL.md` | 注入系统提示词的 skill |

---

## 架构

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

关键组件：

- `AgentLoop`：构建 prompt context、memory、skills、history、run events。
- `AgentRunner`：处理 LLM turn、streaming delta、工具调用和 tool events。
- `ProviderConfigurationFactory`：把 config/env 解析为 provider registry、model refs、fallback chain。
- `ProviderRegistry`：按名称注册 provider 和 descriptor。
- `FallbackLLMProvider`：按顺序执行 provider/model fallback。
- `RuntimeEventBus`：进程内生命周期事件。
- `IAgentHook`：run/tool 前后和错误阶段扩展点。
- `McpToolProvider`：把 MCP server tools 转成 `ITool`。

---

## 安全模型

NanoBot.net 当前已经有实用安全基线：

- `web_fetch` 只允许 `http` 和 `https`。
- 请求前检查 DNS 解析结果。
- 阻止 loopback、private、link-local、CGNAT、multicast、unspecified 等受限地址。
- 每次 redirect 前重新校验目标。
- `run_shell` 限制在配置的 workspace 内执行。
- Shell 工作目录不能逃逸 workspace。
- Shell 命令有超时和输出长度限制。
- 工具错误以结构化 JSON 返回。
- WebSocket gateway 支持 token 认证。

这仍然不是完整沙盒。不要在缺少更强授权、限流和部署控制的情况下暴露给不可信用户。

---

## 测试与交付

运行单元测试：

```bash
dotnet test
```

构建项目：

```bash
dotnet build
```

本地运行真实集成测试：

```bash
NANOBOT_RUN_INTEGRATION_TESTS=1 OPENAI_API_KEY=... dotnet test --filter FullyQualifiedName~RealIntegrationTests
```

GitHub Actions：

| Workflow | 作用 |
|---|---|
| `.github/workflows/ci.yml` | push/PR 时 restore、build、test |
| `.github/workflows/integration.yml` | 手动运行真实 OpenAI + WebSocket 集成冒烟测试 |
| `.github/workflows/release.yml` | tag 触发 CLI 发布，产出 `win-x64`、`linux-x64`、`osx-arm64` |

---

## 已知边界

- Azure OpenAI 当前是 API key auth，还没有 AAD。
- Anthropic 与 Azure provider 当前是非流式；OpenAI-compatible streaming 已实现。
- MCP stdio 已支持，但远程 MCP、OAuth、断线重连和完整生命周期还没完成。
- WebSocket auth 是 token 级别；完整授权、事件过滤和 WebUI 是后续工作。
- 这不是 Python 原版的完整复刻；session compaction、Dream memory、所有原版 channel 还没有完全迁移。

---

## 许可证

MIT
