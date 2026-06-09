# agent-framework.net Scorecard

## 定位

Microsoft Agent Framework 是 .NET/Python production-grade agent/workflow 框架。它不是 coding agent 产品，但对 NanoBot 的 .NET 抽象、workflow、HITL、OpenTelemetry、durable/checkpoint 和 prompt injection 防御非常有参考价值。

## 功能

- .NET / Python 双语言。
- Agent abstraction、ChatClient agent、middleware。
- Workflow：sequential、concurrent、handoff、group collaboration。
- Checkpointing、durable agents、long-running operations。
- Human-in-the-loop / user approval。
- OpenTelemetry instrumentation。
- MCP、Skills、Declarative agents YAML。
- Hosting：ASP.NET、OpenAI-compatible、A2A、AGUI、Azure Functions。
- Context compaction：sliding window、summarization、tool result compaction、truncation。
- Prompt injection defense ADR。

## 关键路径

```text
dotnet/src/Microsoft.Agents.AI
dotnet/src/Microsoft.Agents.AI/ChatClient/
dotnet/src/Microsoft.Agents.AI/Compaction/
dotnet/src/Microsoft.Agents.AI/OpenTelemetryAgent.cs
dotnet/src/Microsoft.Agents.AI.Workflows
dotnet/src/Microsoft.Agents.AI.Mcp
dotnet/src/Microsoft.Agents.AI.Tools.Shell
dotnet/src/Microsoft.Agents.AI.Hosting.AspNetCore
dotnet/src/Microsoft.Agents.AI.Hosting.OpenAI
dotnet/samples/02-agents/
dotnet/samples/03-workflows/
docs/decisions/0002-agent-tools.md
docs/decisions/0003-agent-opentelemetry-instrumentation.md
docs/decisions/0006-userapproval.md
docs/decisions/0009-support-long-running-operations.md
docs/decisions/0019-python-context-compaction-strategy.md
docs/decisions/0021-agent-skills-design.md
docs/decisions/0024-prompt-injection-defense.md
docs/features/durable-agents/
declarative-agents/
```

## 贯穿方式

```text
Application host
  -> Agent / Workflow abstraction
  -> ChatClient / model provider
  -> tools / middleware / approval
  -> workflow checkpoint / durable execution
  -> OpenTelemetry / hosting adapters
```

## NanoBot 可吸收

- .NET 侧接口命名和分层：Agent、RunOptions、Session、Tool、Middleware。
- HITL / user approval 作为框架级 contract。
- OpenTelemetry tracing，用于后续调试工具、模型、token、latency。
- Compaction strategy 作为可插拔策略，但 DeepSeek 1M context 下默认晚触发。
- Durable/long-running operation 的设计思想可用于 NanoBot task manager。
- Skills/declarative agents schema 可帮助设计 NanoBot `plugin.json`。
- Prompt injection 防御和 trust label 应进入工具/Web fetch/MCP 设计。

## 风险

- 框架体量重，不能把 NanoBot 变成 Agent Framework 外壳。
- Azure/Foundry 生态能力需要隔离，不做硬依赖。
- 抽象过早会拖慢 NanoBot 当前 P6 后落地，应该先吸收 contract，再逐步实现。

