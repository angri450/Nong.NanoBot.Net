# soloncode.net Scorecard

## 定位

soloncode 是 Java/Solon AI coding agent，支持 CLI、Web、Desktop/ACP 和 IM channel。它对 NanoBot 的主要价值是中文优先体验、三端贯通、配置项设计和 ReActAgent 扩展方式。

## 功能

- CLI interactive。
- Web interactive。
- Desktop / ACP。
- 流式输出：`agent.prompt(...).stream()`。
- 多模型配置。
- 子代理开关：`subagentEnabled`。
- MCP 开关：`mcpEnabled`。
- Memory 开关和隔离：`memoryEnabled`、`memoryIsolation`。
- HITL。
- 钉钉、飞书、微信等 channel。
- Skill release 目录和 skill market 测试。

## 工具与路径

关键路径：

```text
soloncode-cli/release/config.yml
soloncode-cli/src/main/java/org/noear/solon/codecli/Configurator.java
soloncode-cli/src/main/java/org/noear/solon/codecli/config/AgentProperties.java
soloncode-cli/src/main/java/org/noear/solon/codecli/config/AgentSettings.java
soloncode-cli/src/main/java/org/noear/solon/codecli/portal/cli/CliShell.java
soloncode-cli/src/main/java/org/noear/solon/codecli/portal/web/WebController.java
soloncode-cli/src/main/java/org/noear/solon/codecli/portal/web/WebStreamBuilder.java
soloncode-cli/src/main/resources/static/web.html
soloncode-cli/src/main/resources/static/js/app-streaming.js
examples/extension_demo/src/main/java/org/codecli/ext1/Extension1.java
```

`config.yml` 里可见：

```text
tools: "**"
sessionWindowSize: 8
subagentEnabled: true
mcpEnabled: true
memoryEnabled: true
memoryIsolation: true
modelRetries: 5
```

## 贯穿方式

```text
CLI / Web / Desktop / ACP
  -> HarnessEngine
  -> AgentSession
  -> ReActAgent
  -> ChatModel
  -> stream trace
  -> HITL / memory / tools / channel render
```

CLI 关键链路在 `CliShell`：

```text
engine.getModelOrMain(...)
engine.getAgentOrMain(...)
agent.prompt(originalPrompt)
  .session(session)
  .options(o -> o.chatModel(chatModel))
  .stream()
```

## NanoBot 可吸收

- CLI/Web/IDE 多入口共享同一个 runtime session。
- `@agentName task` 的指定 agent 交互方式。
- 中文优先配置命名和默认体验。
- 配置项直接暴露 subagent、mcp、memory、memoryIsolation、session window。
- extension demo 的 builder/interceptor 思路可转成 NanoBot plugin hook。
- channel credential store 和绑定流程可参考。

## 风险

- Java/Solon 技术栈不进入 NanoBot。
- `sessionWindowSize=8` 与 DeepSeek V4 Flash 1M context 策略不一致。
- `tools: "**"` 太宽，NanoBot 要走 allowlist + approval。
- 产品体验参考价值高于 runtime 参考价值。

