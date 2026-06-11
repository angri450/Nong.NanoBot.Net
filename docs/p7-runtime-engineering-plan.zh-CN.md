# P7 Runtime Engineering 施工方案

日期：2026-06-08

P7 是 P6 调研后的工程落地阶段。P6 已经回答“学谁、学什么、不学什么”，P7 要回答“先改哪些 NanoBot 模块、形成什么 contract、怎么验收”。

## P7 总目标

把 Nong.NanoBot.Net 从“能聊天、能展示 WebUI、能跑基础工具”的状态推进到“可观测、可恢复、可扩展”的 agent runtime。

核心目标只有四条：

1. DeepSeek V4 Flash / Pro 成为一等模型能力。
2. runtime event 和 session JSONL 成为 UI/CLI/未来原生客户端的共同协议。
3. ToolRuntime / PermissionRuntime 形成统一工具执行边界。
4. P7 结束时，WebUI 能看到 reasoning、工具链、usage/cache、会话恢复状态。

## 不做什么

- 不换主线，不把 CodeWhale/Kun/PilotDeck 变成 NanoBot runtime。
- 不引入 Electron/WebView 桌面壳。
- 不复制 PilotDeck AGPL 代码。
- 不做全量多 IM channel。
- 不做无人值守 always-on 自动改 workspace。
- 不把 Nong.Toolkit.Net/Nong 打包进主安装包。

## 现有落点

P7 主要落在这些现有目录：

```text
Nanobot.Core/
  Providers/
  Events/
  Models/
  Agent/
  Tools/
  Memory/

Nanobot.Web/
  Program.cs
  WebContracts.cs
  WebSessionStore.cs
  WorkspaceFileBrowser.cs

Nanobot.Tests/
  ProviderTests.cs
  AgentArchitectureTests.cs
  ToolTests.cs
  MemoryTests.cs
```

## P7.1 DeepSeek V4 Provider

### 目标

在 `OpenAICompatibleProvider` 之外增加 DeepSeek V4 profile 能力。第一阶段可以复用 HTTP client，但不能把 DeepSeek 专属字段塞进所有 OpenAI-compatible provider。

### 建议落点

```text
Nanobot.Core/Providers/DeepSeekV4Provider.cs
Nanobot.Core/Providers/DeepSeekV4Options.cs
Nanobot.Core/Providers/DeepSeekUsage.cs
Nanobot.Core/Providers/DeepSeekModelProfile.cs
Nanobot.Tests/ProviderTests.cs
```

### 必须支持

- `deepseek-v4-flash`
- `deepseek-v4-pro`
- DMX 中转模型 `deepseek-v4-pro-guan`
- GitCode 同步模型 `deepseek-v4-flash`
- raw JSON chat completions
- SSE streaming
- `reasoning_effort`
- `thinking.type`
- `reasoning_content`
- `stream_options.include_usage`
- `prompt_cache_hit_tokens`
- `prompt_cache_miss_tokens`
- `reasoning_tokens`

### Provider gate

只有 DeepSeek V4 profile 允许：

- 请求里带 `thinking` / `reasoning_effort`。
- 消息历史回传 `reasoning_content`。
- usage 解析为 cache hit/miss。

普通 OpenAI-compatible provider 不允许收到这些字段。

### 验收

- mock stream 能解析 text delta、reasoning delta、tool call delta、usage。
- usage 能算出 cache hit rate。
- 不配置 key 时错误可读。
- DMX `deepseek-v4-pro-guan` 仍能作为默认模型工作。

## P7.2 Stable Context Renderer

### 目标

把 prompt/context 构造从“临时拼接”推进到可诊断结构。DeepSeek V4 的 1M 上下文和 prefix cache 只有在前缀稳定时才有价值。

### 建议落点

```text
Nanobot.Core/Agent/ContextRenderer.cs
Nanobot.Core/Agent/RenderedContext.cs
Nanobot.Core/Agent/ContextFingerprint.cs
Nanobot.Tests/AgentArchitectureTests.cs
```

### 分层

```text
static prefix:
  system instruction
  project guidance
  tool catalog
  model profile constraints

stable history:
  persisted user/assistant/tool items
  frozen tool results
  reasoning replay only when model supports it

dynamic tail:
  current user input
  volatile runtime status
  short workspace hints
```

### 规则

- 工具 schema 按 tool id 固定排序。
- project guidance hash 进入 fingerprint。
- 不记录完整 prompt，只记录 hash、token 估算、section sizes。
- no-tool reasoning 默认只本地保存，不回传。
- assistant tool call 历史在 DeepSeek V4 下保留必要 `reasoning_content`。

### 验收

- 同一 tool catalog 生成稳定 fingerprint。
- 工具顺序变化不影响 fingerprint。
- 动态 tail 变化不会污染 static prefix fingerprint。

## P7.3 Runtime Event Contract

### 目标

统一 Core、WebUI、CLI、日志和未来原生 UI 的事件协议。WebUI 不再靠解析最终回答猜工具状态。

### 建议落点

```text
Nanobot.Core/Events/RuntimeEvent.cs
Nanobot.Core/Events/RuntimeEventBus.cs
Nanobot.Core/Events/RuntimeEventTypes.cs
Nanobot.Web/WebContracts.cs
Nanobot.Tests/AgentArchitectureTests.cs
```

### 事件类型

```text
run.started
content.delta
content.completed
reasoning.delta
reasoning.completed
tool.started
tool.delta
tool.completed
tool.failed
usage.updated
cache.updated
approval.requested
user_input.requested
run.interrupted
run.failed
run.completed
```

### 事件字段

每个事件至少包含：

```text
eventId
sequence
timestamp
sessionId
threadId
turnId
runId
type
payload
```

### 验收

- 事件 sequence 单调递增。
- tool.started 和 tool.completed 能配对。
- usage/cache 能被 WebUI 独立展示。
- SSE 能按 `since` replay。

## P7.4 Session JSONL / Thread-Turn-Item

### 目标

建立可恢复 transcript，而不是只保存最终聊天文本。

### 建议落点

```text
Nanobot.Core/Sessions/SessionItem.cs
Nanobot.Core/Sessions/SessionThread.cs
Nanobot.Core/Sessions/SessionTurn.cs
Nanobot.Core/Sessions/JsonlSessionStore.cs
Nanobot.Web/WebSessionStore.cs
Nanobot.Tests/AgentArchitectureTests.cs
```

### 本地布局

```text
~/.nanobot/
  sessions/
    {sessionId}/
      session.json
      threads/
        {threadId}/
          events.jsonl
          messages.jsonl
          snapshot.json
```

### item 类型

```text
user_message
assistant_message
reasoning
tool_call
tool_result
usage
approval
user_input
system_note
```

### 验收

- 关闭 WebUI 后能 resume session。
- SSE 断线后能从 `Last-Event-ID` 或 `since` 继续。
- session snapshot 可由 JSONL 重建。

## P7.5 ToolRuntime / PermissionRuntime

### 目标

把工具执行从“直接调用 tool”推进到统一运行时：schema validation、权限、审计、输出裁剪、错误结构化。

### 建议落点

```text
Nanobot.Core/Tools/ToolRuntime.cs
Nanobot.Core/Tools/ToolPermissionPolicy.cs
Nanobot.Core/Tools/ToolAuditRecord.cs
Nanobot.Core/Tools/ToolResultHandle.cs
Nanobot.Tests/ToolTests.cs
```

### 第一阶段能力

- 统一执行入口。
- workspace path guard。
- read-before-edit / fresh read snapshot。
- shell / Nong argument array。
- timeout。
- stdout/stderr 截断。
- tool result handle，长结果可后续 retrieve。
- audit record 写入 runtime event/session JSONL。

### Permission mode

```text
default
plan
accept_edits
deny_side_effects
```

暂不做 `bypassPermissions` 这类宽松模式。

### 验收

- 写文件前必须已有有效 read snapshot。
- plan mode 禁止 write/edit/shell/Nong 副作用。
- Nong 仍只接受 argument array，不接受 shell command string。
- 长输出被截断并生成 handle。

## P7.6 WebUI 显示闭环

### 目标

WebUI 不只是“能聊天”，要能看到 runtime 正在发生什么。

### 建议落点

```text
Nanobot.Web/Program.cs
Nanobot.Web/WebContracts.cs
Nanobot.Web/wwwroot/app.js
Nanobot.Web/wwwroot/index.html
Nanobot.Web/wwwroot/styles.css
```

### 第一批展示

- reasoning block。
- tool timeline。
- tool args / cwd / exit code / stdout / stderr / truncated。
- usage tokens。
- cache hit/miss 和 hit rate。
- current model/profile。
- session resume 状态。

### 验收

- 无 key 时错误提示可读。
- DMX key 配好时能流式输出。
- mock tool call 能在右侧详情或 timeline 展示。
- 深色和浅色主题不回退。

## P7 实施顺序

1. DeepSeek usage/cache DTO 和 provider gate。
2. RuntimeEvent 扩展和 sequence。
3. JSONL event store。
4. ContextRenderer fingerprint。
5. DeepSeek V4 streaming parser。
6. ToolRuntime/PermissionRuntime。
7. WebUI event/timeline/usage/cache 展示。
8. GitCode `deepseek-v4-flash` profile 绑定。

## P7 验证标准

每批代码改动至少执行：

```powershell
dotnet build
dotnet test
```

涉及 WebUI 时额外检查：

- 桌面宽屏。
- 窄屏。
- 深色主题。
- 浅色主题。
- 无 API key 错误。
- 有 API key 流式输出。
- 工具详情。
- session resume。

真实模型集成仍只允许使用本机环境变量：

```powershell
$env:NANOBOT_RUN_INTEGRATION_TESTS = "1"
$env:DMX_API_KEY = "<local-secret>"
dotnet test --filter RealIntegrationTests
```

## P7 完成标准

P7 完成时，NanoBot 应该具备：

- DeepSeek V4 Flash / Pro profile。
- reasoning/cache usage 可观测。
- runtime event contract。
- session JSONL replay。
- ToolRuntime/PermissionRuntime 第一版。
- WebUI 能展示 reasoning、tool timeline、usage/cache。

这时再进入 P8：GitCode/CodingPlan、plugin marketplace、Nong.Toolkit.Net/Nong.Cli.Net bootstrap 的实现会更稳，因为 runtime、事件、session、工具边界已经先固定住。

