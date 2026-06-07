# NanoBot.net 开发施工方案

本文是 NanoBot.net 的主施工方案。DeepSeek-GUI.net、CodeBuddy、DeepSeek-TUI、DeepSeek-GUI 等项目只作为 UI/交互/信息架构参考；真正要开发和沉淀能力的主仓库是 `NanoBot.net`。

## 方向判断

NanoBot.net 继续作为 GroundPA / Nong agent-runtime 层主线，目标是做成一个 .NET 8、本地优先、可视化可用、工具调用可审计的 agent runtime。

当前不把重点放在重写 DeepSeek-GUI，不做第二个运行时项目。GUI 需要服务 NanoBot：先把 NanoBot 的底层能力通过稳定 API 暴露出来，再让 WebUI 或未来桌面壳调用这些能力。

## 当前基线

- Runtime：`Nanobot.Core`
- CLI：`Nanobot.CLI`
- WebUI：`Nanobot.Web`
- 默认框架：.NET 8
- License：Apache-2.0
- 默认模型：DMX 中转 DeepSeek V4 Pro，模型 ID `deepseek-v4-pro-guan`
- 默认 provider：`dmx`
- 本地配置：`~/.nanobot/config.json`
- 本地工作区：`~/.nanobot/workspace`
- 本地记忆：workspace 下的 `MEMORY.md`、`history.jsonl` 等文件

## 总体架构

```text
NanoBot UI
  WebUI first, optional desktop shell later
        |
        | HTTP / SSE / WebSocket
        v
Nanobot.Web / Runtime Host
  sessions, streaming, file tree, tool events, approvals
        |
        v
Nanobot.Core
  Agent loop
  Provider routing
  Tool registry
  Runtime events
  Memory
  MCP
  Nong bridge
        |
        v
Local workspace + external model/tool APIs
```

开发顺序必须从 NanoBot 自己的 runtime API 出发，而不是先改外部 GUI：

1. `Nanobot.Core` 先有稳定能力和事件。
2. `Nanobot.Web` 把能力暴露成可视化工作台。
3. 后续桌面壳只调用 NanoBot API，不复制一套 agent loop。

## 要调用出来的底层能力

### Agent Loop

- 普通对话
- 流式输出
- 多轮工具调用
- 工具调用失败后的模型恢复
- fallback provider chain
- 每轮 run/session/thread 标识

### Provider / Model

- 默认 `dmx::deepseek-v4-pro-guan`
- OpenAI-compatible provider 继续保留，作为兼容层
- Anthropic、Azure OpenAI 继续保留为 fallback 能力
- API key 只能来自环境变量或用户本地配置，不能进入仓库
- 后续补 usage/token/model 统计，给 UI 展示成本和调用状态

### Tools

- 文件读写编辑
- workspace 内 shell
- Web search / fetch
- GitHub
- Weather / stock
- Memory
- Summarize
- MCP stdio / HTTP / SSE
- Nong bridge

Nong bridge 是重点能力，必须保持 argument-array 调用、workspace 边界、root command allowlist、timeout、输出截断和结构化错误。

### Memory

- `FileMemoryStore`
- `MEMORY.md`
- per-session history
- Dream consolidation
- WebUI 展示记忆状态和最近更新
- 后续增加记忆编辑/冻结/回滚入口

### Runtime Events

UI 不应该靠解析最终回答来猜测状态。NanoBot runtime 要输出结构化事件：

- `run.started`
- `content.delta`
- `content.completed`
- `tool.started`
- `tool.completed`
- `tool.failed`
- `memory.updated`
- `approval.requested`
- `user_input.requested`
- `run.failed`

这些事件是 WebUI、未来桌面壳、日志、调试面板的共同协议。

## WebUI 施工路线

WebUI 是当前第一优先级视觉入口。要求：

- 中文优先，英文可切换
- 深色/浅色主题都完整
- 不做营销 landing page，第一屏就是可用工作台
- 布局参考 CodeBuddy / DeepSeek GUI 的密度和质感，但不复制源码、素材、品牌或专有命名
- 工作台核心区包括会话、聊天、文件树、工具详情、runtime 状态、记忆和配置反馈

### P1：工作台可用

已完成/接近完成：

- WebUI shell
- 会话创建与切换
- 基础聊天
- runtime 状态
- 中文界面

### P2：真实 runtime 能力

已完成/接近完成：

- 流式输出
- 会话持久化
- workspace 文件树
- 工具调用详情

### P3：视觉与交互强化

当前正在施工：

- 参考 DeepSeek-GUI 的浅色质感和工作台布局
- 深色/浅色主题统一
- 聊天画布、侧边栏、右侧详情面板优化
- 工具详情可读性增强
- 移动/窄屏基本可用

### P4：Agent 操作面板

下一阶段：

- plan / todo 面板
- tool approval / deny
- user input gate
- run interrupt
- 会话搜索、归档、fork、resume
- memory 查看与编辑

### P5：桌面壳

等 WebUI API 稳定后再做：

- Electron 或 WinUI 作为 NanoBot 桌面壳
- 复用 NanoBot HTTP/SSE API
- 复用 NanoBot 配置和 workspace
- 不在桌面壳内再实现一套 agent loop

DeepSeek-GUI.net 可作为桌面壳参考，但不是 NanoBot 主施工仓库。

## API 边界

建议 NanoBot runtime API 逐步稳定为：

```text
GET  /api/status
GET  /api/sessions
POST /api/sessions
GET  /api/sessions/{id}
POST /api/chat
POST /api/chat/stream
GET  /api/events?sessionId=&since=
GET  /api/workspace/tree?path=
GET  /api/workspace/file?path=
GET  /api/tools
GET  /api/memory
POST /api/approvals/{id}
POST /api/user-inputs/{id}
POST /api/runs/{id}/interrupt
```

现有 API 可以先保持兼容；新增能力优先围绕这条边界扩展，避免 UI 直接碰 `AgentLoop` 内部。

## 模型配置方案

默认配置：

```json
{
  "providers": {
    "dmx": {
      "kind": "openai-compatible",
      "apiKey": "",
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
      "fallbackModels": ["dmx::deepseek-v4-pro-guan"]
    }
  }
}
```

本地测试使用：

```powershell
$env:DMX_API_KEY = "<local-secret>"
$env:DMX_API_BASE = "https://www.dmxapi.cn/v1/"
$env:DMX_MODEL = "deepseek-v4-pro-guan"
```

注意：OpenAI-compatible SDK 会自己拼接 `chat/completions`，所以 `apiBase` 必须写 `https://www.dmxapi.cn/v1/`，不要写完整的 `/chat/completions` endpoint。

## 近期执行清单

1. 完成并提交当前 WebUI P3 视觉刷新。
2. 把 runtime events 整理成稳定 DTO，减少 WebUI 对内部字段的猜测。
3. 增加 approval / user input gate 的 core contract 和 WebUI 展示。
4. 增强 memory 面板：展示、编辑、刷新、Dream 状态。
5. 增强 Nong tool detail：展示 command、args、cwd、exit code、stdout/stderr、截断状态。
6. 增加 usage/model 状态：当前 provider、模型、streaming、token usage、错误原因。
7. 再评估 Electron/WinUI 桌面壳，前提是 NanoBot Web API 足够稳定。

## 验证标准

每个有意义的阶段至少执行：

```powershell
dotnet test
dotnet build
```

涉及 WebUI 时额外检查：

- 桌面宽屏
- 窄屏/移动宽度
- 深色主题
- 浅色主题
- 无 API key 时的错误提示
- DMX key 配好后的流式输出
- 工具调用详情
- workspace 文件树

真实模型集成只用本机环境变量，不写入仓库：

```powershell
$env:NANOBOT_RUN_INTEGRATION_TESTS = "1"
$env:DMX_API_KEY = "<local-secret>"
dotnet test --filter RealIntegrationTests
```

## 仓库纪律

- 主施工仓库是 `NanoBot.net`。
- 外部 GUI 仓库只做参考和对比，不承载 NanoBot 的主方案。
- 不复制外部 UI 源码或资产。
- 不提交真实 key。
- 不把已有 WebUI、CLI、Core 能力拆成平行 runtime。
- 新能力先沉淀到 `Nanobot.Core` / `Nanobot.Web`，再考虑桌面壳。
