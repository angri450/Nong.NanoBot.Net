<div align="center">

# NanoBot.net

**一个基于 .NET 8 的个人 AI Agent 运行时，支持本地自动化、聊天网关、工具、记忆、MCP 和多 Provider LLM 路由。**

[English README](README.md) · [Releases](https://github.com/angri450/NanoBot.net/releases) · [GitHub](https://github.com/angri450/NanoBot.net)

![.NET 8](https://img.shields.io/badge/.NET-8-6d28d9?style=for-the-badge)
![C# 12](https://img.shields.io/badge/C%23-12-2563eb?style=for-the-badge)
![Tests](https://img.shields.io/badge/tests-77%20passed-16a34a?style=for-the-badge)
![Build](https://img.shields.io/badge/build-0%20warnings%20%2F%200%20errors-16a34a?style=for-the-badge)
![License](https://img.shields.io/badge/license-Apache--2.0-374151?style=for-the-badge)

</div>

## 这是什么

NanoBot.net 是一个独立的 .NET 8 个人 Agent 运行时，面向本地自动化、工具执行、记忆、聊天网关、MCP 和多 Provider LLM 路由。它的目标不是做一个演示脚本，而是给 C# 生态提供一个紧凑但具备工程形态的 local-first Agent 底座。

项目保留轻量个人 Agent 的核心工作方式：本地配置、本地工作区、本地记忆、直接工具执行，不强依赖云端控制面。它曾受 [HKUDS/nanobot](https://github.com/HKUDS/nanobot) 等轻量 Agent 项目启发，但现在按独立 .NET runtime 方向演进，不再定位为逐行移植。

当前代码已经是成熟的集成就绪基线：Agent loop、Provider 路由、流式输出、工具、记忆写入、Dream 记忆整理、MCP stdio/HTTP/SSE、cron、heartbeat、WebSocket 网关和多聊天通道适配都已经落地，并且有测试覆盖。

它还不是可直接暴露给公网的多租户生产服务。更准确的定位是：可靠的个人 Agent 运行时，以及内部集成和继续产品化的基础。

## 当前状态

| 模块 | 状态 | 说明 |
| --- | --- | --- |
| Agent loop | 完整 | 多轮工具调用、流式、hooks、session 隔离、运行时事件 |
| Providers | 完整 | OpenAI 兼容、Anthropic、Azure OpenAI、fallback 链 |
| 流式输出 | 完整 | OpenAI 兼容、Anthropic SSE、Azure OpenAI SSE |
| Memory | 完整 | `MEMORY.md`、`SOUL.md`、`USER.md`、可写记忆、`remember` 工具、`history.jsonl` |
| Dream | 完整 | 定期把会话历史整理进长期 Markdown 记忆 |
| MCP | 完整 | stdio、streamable HTTP、SSE endpoint discovery、`tools/list`、`tools/call` |
| Channels | 基线完整 | Telegram，加 Slack、Discord、飞书 HTTP callback / REST 适配 |
| Gateway | 完整 | CLI、带 token 认证的 WebSocket、聊天网关和 cron |
| Heartbeat | 完整 | `HEARTBEAT.md` active task 检测并接入 gateway |
| Tools | 完整 | 文件、shell、Nong CLI bridge、web、天气、CSV 股票报价、GitHub、摘要、记忆 |
| 安全 | 基线完整 | SSRF 防护、workspace 内 shell、结构化工具错误 |
| CI/release | 完整 | build/test、integration workflow、tag release workflow |

## 快速开始

```bash
# 1. 前置条件：.NET 8 SDK
git clone https://github.com/angri450/NanoBot.net.git
cd NanoBot.net

# 2. 创建 ~/.nanobot/config.json 和 ~/.nanobot/workspace
dotnet run --project Nanobot.CLI -- onboard

# 3. 在 ~/.nanobot/config.json 中填 API key
#    或设置 OPENAI_API_KEY

# 4. 开始聊天
dotnet run --project Nanobot.CLI
```

工作区结构：

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

## 命令

| 命令 | 用途 |
| --- | --- |
| `dotnet run --project Nanobot.CLI` | 交互式聊天 |
| `dotnet run --project Nanobot.CLI -- chat` | 显式进入交互式聊天 |
| `dotnet run --project Nanobot.CLI -- agent -m "..."` | 单轮 Agent 执行 |
| `dotnet run --project Nanobot.CLI -- gateway` | 启动已启用的聊天通道、cron、Dream、heartbeat |
| `dotnet run --project Nanobot.CLI -- websocket` | 启动 WebSocket Agent 网关 |
| `dotnet run --project Nanobot.CLI -- onboard` | 创建默认配置和工作区 |

## 配置

最小 OpenAI 兼容配置：

```json
{
  "providers": {
    "openai": {
      "kind": "openai-compatible",
      "apiKey": "sk-...",
      "apiBase": "https://api.openai.com/v1/",
      "defaultModel": "gpt-4o",
      "models": [
        {
          "id": "gpt-4o",
          "apiModelId": "gpt-4o",
          "supportsStreaming": true,
          "supportsTools": true
        }
      ]
    }
  },
  "agents": {
    "defaults": {
      "model": "openai::gpt-4o",
      "fallbackModels": ["openai::gpt-4o"],
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

多 Provider fallback：

```json
{
  "providers": {
    "openai": {
      "kind": "openai-compatible",
      "apiKey": "sk-...",
      "defaultModel": "gpt-4o"
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
      "deployment": "gpt-4o",
      "apiVersion": "2024-10-21"
    }
  },
  "agents": {
    "defaults": {
      "fallbackModels": [
        "openai::gpt-4o",
        "anthropic::claude-sonnet-4-5",
        "azure-openai::gpt-4o"
      ]
    }
  }
}
```

MCP stdio / HTTP / SSE：

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

Nong CLI bridge：

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

聊天网关：

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

## 运行时架构

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

关键实现点：

- `ProviderConfigurationFactory` 负责 config、环境变量覆盖、模型 ID、API 模型 ID、provider 能力和 fallback 链。
- `AgentRunner` 执行最多 20 轮工具调用循环，工具输出上限 15000 字符。
- `FileMemoryStore` 读写长期记忆文件，并把 session 历史追加到 `history.jsonl`。
- `DreamConsolidator` 使用当前 LLM provider 把新历史整理进 `MEMORY.md`。
- `McpClientFactory` 根据配置选择 stdio、streamable HTTP 或 SSE transport。
- `NongTool` 把 `run_nong` 暴露为参数数组工具，工作目录限制在 workspace 内，执行根命令 allowlist，并默认补 `--json`。
- `NetworkSecurityGuard` 阻止 loopback、private、link-local、CGNAT、multicast、broadcast 和不安全 IPv6 地址。

## 环境变量

| 变量 | 用途 |
| --- | --- |
| `OPENAI_API_KEY` | OpenAI 兼容 provider API key |
| `OPENAI_API_BASE` | 覆盖 OpenAI 兼容 base URL |
| `OPENAI_MODEL` | 覆盖默认模型，支持 `provider::model` |
| `ANTHROPIC_API_KEY` | 启用 Anthropic provider |
| `ANTHROPIC_API_BASE` | 覆盖 Anthropic base URL |
| `ANTHROPIC_MODEL` | 覆盖 Anthropic 默认模型 |
| `AZURE_OPENAI_API_KEY` | 启用 Azure OpenAI provider |
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint |
| `AZURE_OPENAI_DEPLOYMENT` | Azure OpenAI deployment |
| `AZURE_OPENAI_API_VERSION` | Azure OpenAI API version |
| `NANOBOT_STREAMING` | `1`、`true` 或 `yes` 启用流式 |
| `BRAVE_API_KEY` | Web 搜索 API key |
| `GITHUB_TOKEN` | GitHub 工具 token |
| `NANOBOT_WS_PREFIX` | WebSocket 监听地址 |
| `NANOBOT_WS_TOKEN` | WebSocket 认证 token |

## 测试

```bash
dotnet test
dotnet build

# 真实集成测试需要凭据
NANOBOT_RUN_INTEGRATION_TESTS=1 OPENAI_API_KEY=... dotnet test --filter RealIntegrationTests
```

当前本地验证结果：

| 检查 | 结果 |
| --- | --- |
| `dotnet test` | 77 passed，0 failed，0 skipped |
| `dotnet build` | 0 warnings，0 errors |
| 源码审计 | 0 TODO，0 stub，0 `NotImplementedException` |

## 安全边界

NanoBot.net 有实用安全护栏，但不是完整的敌意用户沙箱。

- Shell 执行限制在配置的 workspace 内。
- Nong 执行使用参数数组而不是 shell 命令字符串，并拒绝 workspace 外工作目录。
- HTTP fetch 会在请求和重定向前做 SSRF 校验。
- WebSocket token 使用常量时间比较。
- 工具错误以结构化 JSON 返回。
- 公网部署仍然需要更强认证、授权、限流、观测和密钥管理。

## License

Apache-2.0。受轻量个人 Agent 项目启发，作为独立 .NET runtime 演进。
