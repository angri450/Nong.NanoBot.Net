# Nong.NanoBot.Net P2 完成记录与 P3 计划

> 记录日期：2026-06-05
> 背景文档：`changelog/2026-06-05-agent-implementation-plan.md`
> 状态：P2 已施工完成，P3 进入可执行规划。

---

## 一、P2 完成摘要

P2 的目标是先把 .NET 版从单一 `Agent` 循环拆成后续可扩展的核心边界，不提前混入 MCP、WebUI、多 Provider 或完整 SessionManager。

已完成：

- `AgentRunner`：负责 provider 对话、工具调用循环、工具结果截断、最大轮次兜底。
- `AgentLoop`：负责系统提示词、memory、skill、短期历史、run 生命周期。
- `AgentExecutionContext`：提供 `SessionId`、`Workspace`、`IsEphemeral`、`ParentRunId`、`AllowedTools`。
- `RuntimeEventBus`：发布 run/tool started/completed/failed 事件。
- `IAgentHook`：支持 run/tool before、after、error 生命周期；tool hook 可修改或拒绝工具调用。
- `SkillLoader`：扫描 `<workspace>/skills/*/SKILL.md`，按目录名排序并注入系统提示词。
- `Agent`：保留 `RunAsync(string input)` 兼容入口，并新增可传入 `AgentExecutionContext` 的重载。

验证：

```bash
dotnet test
dotnet build
```

结果：20 个测试通过，整体构建 0 warning、0 error。

---

## 二、P3 目标

状态：已完成。完成细节见 `changelog/2026-06-05-p3-completion.md`。

P3 目标是把 P2 建好的边界用于能力扩展，但仍保持任务可独立提交、可测试、可回滚。

总体优先级：

1. Provider registry 与 fallback chain。
2. OpenAI-compatible、Anthropic、Azure OpenAI provider 分层。
3. MCP 工具接入。
4. 轻量 WebSocket Gateway；Blazor WebUI 后置。

---

## 三、P3 任务拆分

### P3-0：Provider Registry

目标：

- 新增 `ProviderRegistry`，按 provider 名称注册和解析 `ILLMProvider`。
- 新增最小 `ProviderDescriptor`，记录 name、kind、default model、capabilities。
- CLI 仍默认使用现有 OpenAI 配置，不改变当前启动方式。

验收：

- 可注册、覆盖、查询 provider。
- 未知 provider 返回清晰错误。
- `dotnet test` 覆盖默认 OpenAI provider 不受影响。

### P3-1：Fallback Chain

目标：

- 新增 `FallbackLLMProvider`，按顺序尝试多个 `ILLMProvider`。
- provider 抛异常或返回 `FinishReason == "error"` 时进入下一个 provider。
- 最终失败时返回包含所有失败 provider 名称的错误响应。

验收：

- 第一 provider 成功时不会调用后续 provider。
- 第一 provider 失败时自动降级到第二 provider。
- 全部失败时保留失败链路信息。

### P3-2：OpenAI-compatible Provider 分层

目标：

- 将现有 `OpenAIProvider` 抽成 OpenAI-compatible 基础实现，支持 api key、base URL、model。
- 保留 `OpenAIProvider` 作为默认包装，不破坏 CLI 配置。
- 为后续 xAI、Zhipu、OpenRouter 等兼容接口留出 provider descriptor。

验收：

- 现有 OpenAI 行为和测试不变。
- compatible provider 能通过 base URL 和 model 构造。
- provider registry 可注册多个 compatible provider。

### P3-3：Anthropic 与 Azure OpenAI

目标：

- 新增 `AnthropicProvider`，实现 `ILLMProvider` 的非流式 chat 与 tool schema 映射。
- 新增 `AzureOpenAIProvider`，先支持 API key，AAD 认证后置为独立任务。
- 不在本任务中实现 streaming。

验收：

- 使用 fake HTTP handler 或可注入 client 覆盖请求构造与响应解析。
- tool call 能映射为当前 `ToolCallRequest`。
- 错误响应转换为 `LLMResponse(FinishReason = "error")` 或明确异常策略。

### P3-4：MCP 工具接入

目标：

- 新增 `McpToolProvider`，把 MCP server 暴露的 tools 适配为 `ITool`。
- 支持最小 stdio MCP server 配置。
- MCP 工具纳入 `ToolRegistry`，并受 `AgentExecutionContext.AllowedTools` 过滤。

验收：

- fake MCP server 可列出工具。
- 调用 MCP 工具能返回字符串结果。
- MCP server 启动失败、工具调用失败有结构化错误文本。

### P3-5：轻量 WebSocket Gateway

目标：

- 新增轻量 gateway 作为 API/WebUI 前置能力，不直接上 Blazor。
- 支持 WebSocket 收发消息，调用现有 `Agent.RunAsync`。
- 为后续 WebUI、channel multiplex、runtime event streaming 留接口。

验收：

- 本地 WebSocket client 可发送消息并收到回复。
- Gateway 可订阅 `RuntimeEventBus` 并向客户端推送 run/tool 事件。
- 不影响 CLI `chat`、`agent --message`、Telegram gateway。

---

## 四、P3 非目标

- 不在 P3-0 到 P3-3 中实现 streaming；streaming 仍属于 P1/P3 后续交叉任务。
- 不在 MCP 首版实现断线重连、OAuth、远程 HTTP MCP。
- 不在 WebSocket Gateway 首版实现完整 WebUI、登录、权限、多租户。
- 不一次性迁移 Python 原版全部 provider 和 channel。

---

## 五、建议执行顺序

1. `P3-0 Provider Registry`
2. `P3-1 Fallback Chain`
3. `P3-2 OpenAI-compatible Provider 分层`
4. `P3-3 Anthropic 与 Azure OpenAI`
5. `P3-4 MCP 工具接入`
6. `P3-5 轻量 WebSocket Gateway`

每个任务结束都必须运行：

```bash
dotnet test
dotnet build
```
