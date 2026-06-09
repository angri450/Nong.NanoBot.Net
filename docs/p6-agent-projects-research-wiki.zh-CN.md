# P6 智能体项目调研 Wiki

调研日期：2026-06-08

本轮 P6 的目标是先把外部智能体项目研究清楚，再决定 NanoBot.net 后续吸收什么。一个问题不能两头都是谜团：NanoBot 继续作为主线，但 P6 不直接施工新功能，先做横向调研和架构取舍。

研究副本放在：

```text
C:\Users\Administrator\Documents\Github\_agent-research-p6
```

这些副本已经浅克隆并删除 `.git`，只用于阅读学习，不作为后续更新源。

分册版 Wiki 入口：

```text
docs/p6-wiki/00-index.zh-CN.md
```

单篇总览保留在本文中，用于快速判断主线和优先级；源码路径、能力矩阵、项目 scorecard 和 ADR 以 `docs/p6-wiki/` 为准。

## 总体结论

NanoBot.net 仍然最适合作为主线。

这些项目各有价值，但没有一个同时满足 NanoBot 的目标组合：

- .NET 8 主线
- Apache-2.0
- 本地优先
- 不用 WebView2 / Electron 做桌面壳
- 能分发 MSI
- 能和 Nong / GroundPA-Toolkit 走 plugin/bootstrap
- 能把 DeepSeek V4 Flash、GitCode、DMX、Nong、WebUI、未来原生 UI 统一到一个 runtime

P6 的正确姿势不是换主线，而是把每个项目最强的设计吸收到 NanoBot：

```text
CodeWhale.net       -> DeepSeek V4 Flash、长上下文、缓存命中、工具/会话/任务 runtime
DeepSeek-GUI.net    -> WebUI/桌面工作台信息架构、HTTP/SSE runtime 边界、Kun 的 token economy
PilotDeck.net       -> WorkSpace 隔离、白盒 memory、smart routing、always-on 任务、插件协议
GenericAgent.net    -> 极简原子工具、自进化 skill/memory、最小 agent loop
EvoScientist.net    -> 多 agent 科研工作流、多渠道 message bus、技能/MCP/onboarding
soloncode.net       -> CLI/Web/IDE 三端、Java ReActAgent 插件扩展、中文优先配置经验
agent-framework.net -> .NET production-grade agent/workflow 抽象、Human-in-the-loop、OpenTelemetry、Durable workflow
```

## 横向排名

### 最像 NanoBot 可吸收 runtime 的项目

1. **CodeWhale.net**
   - 工具、会话、runtime API、任务、MCP、skills、hooks、DeepSeek V4 都很完整。
   - 缺点是 Rust 主线，不适合直接成为 NanoBot 主仓库。

2. **PilotDeck.net**
   - Agent OS 思路明确，WorkSpace / memory / router / plugin 边界值得学习。
   - 缺点是 AGPL-3.0，且 Node/TS 体系不适合作为 NanoBot 主线。

3. **Microsoft agent-framework.net**
   - .NET 生产级抽象最有参考价值，尤其 workflow、hosting、observability、HITL。
   - 缺点是框架很重，偏 Azure/Foundry 生态，不适合直接绑死 NanoBot。

### 最像 UI / 产品形态参考的项目

1. **DeepSeek-GUI.net**
   - Code / Write / 连接手机 / 设置 / Skill / MCP / 计划 / Review / diff 审查信息架构完整。
   - 但技术栈是 Electron/Vite，用户明确拒绝 WebView/Electron，NanoBot 只能学 WebUI 信息架构。

2. **PilotDeck.net**
   - WorkSpace、white-box memory、multi-agent 可视化很适合 NanoBot WebUI P6/P7。

3. **GenericAgent.net**
   - PyQt / Textual / Streamlit 前端都比较实用，但产品质感不应作为 NanoBot 主 UI 标杆。

### 最像模型 / 上下文 / DeepSeek 参考的项目

1. **CodeWhale.net**
2. **DeepSeek-GUI.net / Kun**
3. **soloncode.net**

CodeWhale 和 Kun 都把 DeepSeek V4 Flash 当成主力模型处理，并显式关注 cache hit/miss、reasoning、工具调用、上下文压缩。

### 不适合作为 NanoBot 主线的项目

- DeepSeek-GUI.net：Electron 桌面壳，不符合用户桌面分发偏好。
- CodeWhale.net：Rust 主线太强，适合参考，不适合替换 .NET 主线。
- PilotDeck.net：AGPL-3.0，协议和技术栈都不适合直接吸收代码。
- GenericAgent.net：Python 极简路线优秀，但和 .NET/Nong/GroundPA 统一目标不一致。
- EvoScientist.net：科研 agent 方向强，但主线偏 Python + LangGraph/DeepAgents。
- soloncode.net：Java/Solon 生态，适合作为三端和中文体验参考。
- agent-framework.net：可学抽象，不能把 NanoBot 变成 Azure/Foundry 重框架附属。

## CodeWhale.net

### 项目定位

CodeWhale 是 DeepSeek-TUI 改名后的 Rust 终端 coding agent，围绕 DeepSeek V4 Pro / Flash 构建。它不是聊天壳，而是完整 agent harness：TUI、runtime API、工具、会话、子 agent、RLM、MCP、skills、hooks、LSP、任务队列、成本统计、缓存命中率。

### 功能

- 流式输出：支持。
- 流式 reasoning blocks：支持。
- 会话持久化：支持，`~/.codewhale/sessions`。
- checkpoint / crash recovery：支持。
- 跨会话 memory：支持 user memory 和 skills。
- 工具调用：支持，工具事件和 approval 都比较完整。
- 子 agent：支持 `agent_open` / `agent_eval` / `agent_close`，后台并发。
- RLM：支持持久递归语言模型会话，用于批量分析。
- MCP：支持。
- LSP：支持 post-edit diagnostics。
- HTTP/SSE runtime API：支持。
- 任务队列：支持 durable task manager。
- 成本和缓存：支持 cache hit/miss breakdown。

### 工具

README 和架构文档明确列出：

- shell
- file read/write/edit/apply_patch
- todo
- durable tasks
- GitHub
- automation/scheduling
- plan
- subagent
- RLM
- MCP tools
- skills
- hooks
- web/search providers
- restore/revert workspace snapshot

关键路径：

```text
crates/tui/src/core/engine.rs
crates/tui/src/core/engine/turn_loop.rs
crates/tui/src/client.rs
crates/tui/src/llm_client.rs
crates/tui/src/tools/
crates/tui/src/runtime_api.rs
crates/tui/src/runtime_threads.rs
crates/tui/src/task_manager.rs
crates/tui/src/lsp/
crates/agent/src/lib.rs
crates/execpolicy/src/lib.rs
crates/hooks/src/lib.rs
docs/ARCHITECTURE.md
docs/SUBAGENTS.md
docs/RUNTIME_API.md
docs/MEMORY.md
```

### 架构贯穿

```text
codewhale dispatcher
  -> codewhale-tui
  -> ratatui UI / one-shot / HTTP runtime API
  -> core engine
  -> llm client
  -> tool registry
  -> hooks / MCP / skills / LSP
  -> session/task/runtime thread stores
```

数据流是标准 agent loop：

```text
User input
  -> Engine
  -> LLM streaming
  -> parse content/reasoning/tool calls
  -> execute tools with approval/policy
  -> append tool results
  -> continue model loop
  -> persist events/session/task timeline
```

### 对 NanoBot 最有价值

- DeepSeek V4 Flash profile：模型别名、provider 映射、supportsTools、supportsReasoning。
- `reasoning_content` 与工具调用历史回传。
- cache hit/miss 成本统计和 UI/状态展示。
- 子 agent 后台并发模型：启动立即返回，完成后向父 transcript 注入 sentinel。
- LSP post-edit diagnostics：文件修改后把诊断作为下一轮上下文。
- side-git snapshot：不污染用户 `.git` 的回滚机制。
- durable task manager：后台任务、timeline、artifact、verifier gate。
- runtime thread/turn/item event schema。
- approval policy 与工具级 ask rule。

### 风险

- Rust 实现不能直接移植。
- CodeWhale 的 Constitution/harness 风格很强，NanoBot 不应照搬品牌和 prompt。
- 有些 sandbox 能力平台差异大，Windows 不能直接等价。

## DeepSeek-GUI.net

### 项目定位

DeepSeek-GUI 是以 Kun 为本地运行时的桌面工作台。它的产品形态很完整：Code、Write、连接手机、定时任务、Skill/MCP 管理、计划、Todo、目标、Review、diff 审查、设置、本地日志、主题/语言。

它最适合作为 NanoBot WebUI / 原生 UI 的信息架构参考，而不是技术栈参考。

### 功能

- 流式输出：支持。
- reasoning / thinking 展示：支持。
- 会话持久化：支持。
- 工作区文件：支持。
- 文件 diff / 变更审查：支持。
- 多会话、fork、归档、恢复：支持。
- 计划、Todo、目标模式：支持。
- Review：支持。
- Skill/MCP 图形管理：支持。
- 连接手机：飞书 / Lark / 微信。
- 定时任务：支持。
- Write 模式：Markdown 写作空间、Live 编辑、FIM 补全、导出。
- 主题/语言：支持中英文与主题设置。

### 工具

Kun 内置工具方向：

- read
- write
- edit / edit_file / apply_patch
- shell
- todo_list / todo_write
- web_fetch / web_search
- MCP search/describe/call
- create_plan
- memory
- approval gate
- user input gate
- scheduled task

关键路径：

```text
kun/src/adapters/model/deepseek-compat-model-client.ts
kun/src/adapters/tool/
kun/src/adapters/file/file-session-store.ts
kun/src/adapters/hybrid/hybrid-session-store.ts
kun/src/cache/immutable-prefix.ts
src/shared/kun-endpoints.ts
src/shared/ds-gui-api.ts
src/shared/gui-plan.ts
src/shared/app-settings-types.ts
docs/kun-architecture.md
docs/kun-cache-optimization.md
kun/README.md
```

### 前后端贯穿

README 给出的简化链路：

```text
Renderer (React)
  -> KunRuntimeProvider
  -> preload: dsGui.runtimeRequest / startSse
  -> main: LocalHttpRuntimeAdapter
  -> kun serve (HTTP + SSE)
  -> cache-first AgentLoop
```

端点集中在：

```text
src/shared/kun-endpoints.ts
```

包括：

```text
/v1/runtime/tools
/v1/memory
/v1/memory/diagnostics
/v1/sessions/{id}/resume-thread
```

### 对 NanoBot 最有价值

- WebUI 信息架构：Code / Write / 手机连接 / 定时任务 / 设置。
- runtime HTTP/SSE 边界：UI 不直接碰 agent loop。
- `runtimeRequest + startSse` 这类前端桥接思路可以转成 NanoBot Web API。
- token economy 设置：工具描述压缩、工具结果压缩、工具风暴抑制、参数修复。
- `ImmutablePrefix`：system prompt + tools + pinned constraints + fewshots 的稳定 fingerprint。
- `FileSessionStore` + JSONL event replay + canonical session snapshot。
- `approvalGate` / `userInputGate` 模型。
- `create_plan` 工具让计划成为结构化可追踪 artifact。

### 风险

- Electron/Vite/桌面壳不符合用户拒绝 WebView/Electron 的要求。
- Kun 是 TypeScript runtime，NanoBot 不应直接引入。
- MIT 许可可参考思想，但不能复制品牌和资产。

## PilotDeck.net

### 项目定位

PilotDeck 是 WorkSpace-centric Agent OS，强调 workspace 隔离、白盒 memory、smart routing、always-on background execution、MCP native 和 plugin.json 扩展。

它对 NanoBot 的 product direction 很有价值，尤其是 GroundPA 未来要做多 workspace、多任务、白盒记忆和后台运行。

### 功能

- Web UI：支持。
- CLI/TUI/channel：支持多前端。
- 流式输出：支持，API server 支持 OpenAI-compatible stream。
- 会话管理：支持 sessionKey / projectKey。
- WorkSpace：核心概念，文件、memory、skills 隔离。
- 白盒 memory：可见、可编辑、可回滚。
- Dream mode：空闲时记忆整理。
- Smart routing：任务难度路由到不同模型。
- Always-on：后台发现任务、执行、落文件。
- MCP：native。
- plugin.json：扩展协议。
- 多渠道：Feishu、Weixin、QQ、Telegram、Discord、Slack、Matrix、Email、SMS、HomeAssistant 等适配器。

### 工具

代码中可见：

- ToolRegistry
- ToolRuntime
- SequentialToolScheduler
- PermissionRuntime
- builtin agent tools
- MCP tools
- workspace file tools
- project files adapter
- tool_call_started / tool_call_finished events

关键路径：

```text
src/agent/loop/AgentLoop.ts
src/router/index.ts
src/tool/index.ts
src/tool/builtin/
src/adapters/channel/api-server/ApiServerChannel.ts
src/adapters/web/projectFiles.ts
src/adapters/index.ts
scripts/tui-e2e-record.tsx
scripts/tui-e2e-permission.tsx
ui/
```

### 架构贯穿

```text
UI / CLI / IM / OpenAI-compatible API
  -> channel adapter / session mapper
  -> AgentLoop
  -> RouterRuntime
  -> model provider
  -> ToolRegistry + PermissionRuntime + ToolScheduler
  -> workspace / memory / plugin / MCP adapters
  -> event stream back to channel
```

### 对 NanoBot 最有价值

- WorkSpace 作为一等边界：文件、memory、skills、session 都隔离。
- 白盒 memory：每条记忆可追踪来源、可编辑、可删除、可回滚。
- Smart routing：主模型 + 子模型，按任务难度和成本自动分派。
- Always-on：后台任务不只是定时器，而是任务生命周期 + artifact。
- `plugin.json` 扩展协议与 lifecycle hooks。
- OpenAI-compatible API server 可以把 agent 暴露给外部客户端。
- 多渠道 SessionMapper 思路，适合 NanoBot 后续 IM / mobile。

### 风险

- AGPL-3.0，不适合复制代码进入 Apache-2.0 的 NanoBot。
- Node/TS 体系和 NanoBot .NET 主线不同。
- 产品野心大，NanoBot 初期不应一次性复制全部 Agent OS。

## GenericAgent.net

### 项目定位

GenericAgent 是极简、自进化、自举型 Python agent。核心叙事是 9 个原子工具 + 约 100 行 agent loop + 分层记忆 + 自动沉淀 skill。

它不是最适合直接做 NanoBot runtime 的项目，但它的“最小工具集”和“自进化 skill”非常适合 GroundPA 的长期路线。

### 功能

- 流式输出：支持，前端有 streaming queue。
- 多前端：PyQt、Textual TUI、Streamlit、IM bot。
- 会话恢复：支持 `/continue`。
- 跨会话 memory：强项，L0-L4 分层。
- 自进化 skill：核心能力。
- 浏览器控制：真实浏览器，保留登录态。
- 系统控制：键鼠、屏幕视觉、ADB。
- 多 agent / Goal Hive：支持。
- Morphling mode：吸收外部项目能力的流程。

### 工具

README 明确 9 个原子工具：

```text
code_run
file_read
file_write
file_patch
web_scan
web_execute_js
ask_user
update_working_checkpoint
start_long_term_update
```

关键路径：

```text
agent_loop.py
agentmain.py
ga.py
llmcore.py
memory/
reflect/goal_mode.py
memory/morphling_sop.md
memory/goal_hive_sop.md
frontends/tuiapp_v2.py
frontends/qtapp.py
assets/agent_bbs.py
```

### 架构贯穿

```text
frontend / IM / CLI
  -> Agent instance
  -> minimal loop
  -> LLM session
  -> atomic tools
  -> memory files / skill SOP
  -> frontend streaming queue
```

### 对 NanoBot 最有价值

- 工具集最小化：不要一开始暴露海量工具；用少数原子能力组合出复杂能力。
- 自进化 skill：任务完成后把路径沉淀成 SOP/skill。
- 分层 memory：L0 rules、L1 index、L2 facts、L3 skills、L4 session archive。
- Morphling mode：从外部项目提取目标和测试，再决定调用、重写或舍弃。
- Agent BBS：多 agent 简单协作公告板。

### 风险

- Python 动态执行能力强但边界风险高。
- `code_run` 权限过大，NanoBot/Nong 必须保留严格 allowlist、workspace、timeout。
- Python GUI/IM 代码不能作为 NanoBot 直接主线。

## EvoScientist.net

### 项目定位

EvoScientist 是科研工作流 agent，基于 DeepAgents/LangGraph 生态，强调多 agent team、自进化 memory、科研 lifecycle、MCP/skills、多渠道。

它适合作为 NanoBot 未来“任务型 agent team”和“跨渠道入口”的参考。

### 功能

- 多 agent team：plan、research、code、debug、analyze、write。
- 科研工作流：intake -> plan -> execute -> evaluate -> write -> verify。
- 自进化 memory：每轮观察和用户 profile 提炼。
- 多 provider：Anthropic、OpenAI、Google、MiniMax、NVIDIA 等。
- 多 channel：Telegram、Slack、Feishu、WeChat、QQ、Discord、Email、iMessage 等。
- WebUI beta：支持。
- CLI/TUI：支持。
- MCP 与 Skills：支持。
- human approval：支持。
- ask_user：支持。
- streaming thinking：渠道 consumer 支持 thinking/tool/subagent 事件。

### 工具与关键路径

核心不是手写极简工具，而是 DeepAgents/LangGraph 工具体系 + MCP/skills/channel adapter。

关键路径：

```text
EvoScientist/EvoScientist.py
EvoScientist/channels/bus/events.py
EvoScientist/channels/bus/message_bus.py
EvoScientist/channels/consumer.py
EvoScientist/channels/standalone.py
EvoScientist/channels/plugin.py
EvoScientist/cli/widgets/
EvoScientist/mcp/
EvoScientist/skills/
```

### 架构贯穿

```text
CLI/TUI/WebUI/channel
  -> message bus
  -> channel consumer
  -> EvoScientist agent / LangGraph stream
  -> event typing: text / thinking / tool_call / subagent_text / approval / ask_user
  -> outbound channel renderer
```

### 对 NanoBot 最有价值

- 多渠道 message bus：channel 不直接调用 agent 内部，而是通过统一 bus。
- channel session key：`channel:chat_id` 作为会话边界。
- streaming event 分类：thinking、tool_call、subagent_text、approval、ask_user。
- 科研 workflow 角色拆分：plan/research/code/debug/analyze/write。
- Agent 主动问用户问题的流程。
- MCP/skill browser 的 TUI 交互。

### 风险

- Python/LangGraph/DeepAgents 路线和 NanoBot .NET 主线差异大。
- 科研任务很强，但 NanoBot 当前更偏本地 coding/GroundPA runtime。

## soloncode.net

### 项目定位

soloncode 是 Java/Solon AI coding agent，支持 CLI、Web、IDE desktop/ACP，中文提示驱动，Java8 到 Java26 运行环境。

它的价值在于三端统一、中文优先、Solon ReActAgent 扩展和配置经验。

### 功能

- CLI interactive：支持。
- Web interactive：支持。
- Desktop IDE / ACP：支持。
- 流式输出：支持，`agent.prompt(...).stream()`。
- 子代理：配置支持 `subagentEnabled`。
- MCP：配置支持。
- memory：配置支持，且支持 workspace isolation。
- HITL：支持。
- 多模型配置：支持。
- 钉钉等 channel：代码中有实现。

### 工具与关键路径

关键路径：

```text
soloncode-cli/release/config.yml
soloncode-cli/src/main/java/org/noear/solon/codecli/portal/cli/CliShell.java
soloncode-cli/src/main/java/org/noear/solon/codecli/channel/dingtalk/
examples/extension_demo/src/main/java/org/codecli/ext1/Extension1.java
```

`config.yml` 中可见：

```text
tools: "**"
sessionWindowSize: 8
subagentEnabled: true
mcpEnabled: true
memoryEnabled: true
memoryIsolation: true
modelRetries: 5
```

### 架构贯穿

```text
CLI/Web/IDE
  -> HarnessEngine
  -> AgentSession
  -> ReActAgent
  -> ChatModel
  -> stream trace
  -> HITL / tool display / session memory
```

CLI 关键调用在 `CliShell.performAgentTask`：

```text
engine.getModelOrMain(...)
engine.getAgentOrMain(...)
agent.prompt(originalPrompt)
  .session(session)
  .options(o -> o.chatModel(chatModel))
  .stream()
```

### 对 NanoBot 最有价值

- 一个 runtime 同时服务 CLI/Web/IDE 的思路。
- `@agentName task` 选择具体 agent 的交互。
- extension demo：通过 builder/interceptor 定制 ReActAgent。
- 配置项中文友好：subagent、mcp、memory、memoryIsolation、session window。
- channel 绑定与本地 credential store。

### 风险

- Java/Solon 体系不适合直接并入 NanoBot。
- sessionWindowSize=8 这种小窗口策略与 DeepSeek V4 Flash 1M 长上下文路线不同。

## agent-framework.net

### 项目定位

Microsoft Agent Framework 是生产级 .NET/Python agent 和 workflow 框架。它不是一个 coding agent 产品，而是 agent/workflow 基础设施。

### 功能

- .NET / Python 双语言。
- 多 provider。
- middleware。
- workflow orchestration：sequential、concurrent、handoff、group collaboration。
- checkpointing、streaming、human-in-the-loop、time-travel。
- OpenTelemetry observability。
- DurableTask / Azure Functions / A2A / AGUI hosting。
- Declarative agents YAML。
- MCP。
- Skills。
- DevUI。

关键路径：

```text
dotnet/src/Microsoft.Agents.AI
dotnet/src/Microsoft.Agents.AI.Workflows
dotnet/src/Microsoft.Agents.AI.Mcp
dotnet/src/Microsoft.Agents.AI.Tools.Shell
dotnet/src/Microsoft.Agents.AI.Hosting.AspNetCore
dotnet/src/Microsoft.Agents.AI.Hosting.OpenAI
dotnet/samples/02-agents/
dotnet/samples/03-workflows/
```

### 对 NanoBot 最有价值

- .NET 侧 agent abstraction 和 workflow abstraction。
- Human-in-the-loop workflow 节点。
- OpenTelemetry tracing。
- Durable workflow/checkpointing 思路。
- Declarative agents / skills schema。
- ASP.NET hosting 与 OpenAI-compatible hosting。

### 风险

- 体系很重，容易把 NanoBot 做成框架集成项目，而不是轻量 GroundPA runtime。
- Azure/Foundry 生态耦合需要谨慎隔离。
- 许可是 MIT，可参考，但 NanoBot 不应直接大规模依赖。

## P6 对 NanoBot 的吸收路线

### P6.1 建立项目能力矩阵

把每个项目按以下维度固化成表：

```text
流式输出
reasoning/thinking
工具调用
工具调用详情
会话持久化
跨会话记忆
workspace 隔离
文件树 / 文件编辑
shell / command approval
MCP
skills / plugin
子 agent
任务队列 / always-on
模型路由
缓存命中率
runtime API
WebUI / TUI / CLI / IM
安装分发
license 风险
NanoBot 可吸收优先级
```

### P6.2 先吸收共识能力，不追单个项目

多个项目都重复出现的能力，优先进入 NanoBot：

- Runtime API + SSE event stream。
- Stable session/thread/turn/item model。
- ToolRegistry + ToolRuntime + approval policy。
- Memory 可视化和可编辑。
- Workspace isolation。
- Plugin/Skill marketplace。
- DeepSeek V4 Flash reasoning/cache usage。
- 多渠道 session mapper。

### P6.3 NanoBot 不换主线

P6 调研不是为了决定“换成某个项目”，而是为了让 NanoBot 少走弯路。

最终产品定位：

```text
NanoBot.net = .NET 8 GroundPA runtime host
CodeWhale/Kun = DeepSeek 和 token economy 教材
PilotDeck = workspace/memory/router/always-on 教材
GenericAgent = 极简工具和自进化 skill 教材
EvoScientist = 多 agent 科研 workflow 和 channel bus 教材
soloncode = 中文 CLI/Web/IDE 体验教材
agent-framework = .NET workflow/hosting/observability 教材
```

## P6 之后建议施工顺序

1. DeepSeek V4 Flash provider 和 usage/cache metrics。
2. Stable context renderer：immutable prefix、工具 schema 排序、历史工具结果冻结。
3. Runtime event model：content/reasoning/tool/usage/cache/approval/user_input。
4. Session/thread/turn/item 持久化。
5. WebUI 展示 reasoning、tool detail、cache hit rate、workspace、memory。
6. Plugin/skill marketplace 设计。
7. Workspace-scoped white-box memory。
8. Smart routing / sub-agent / always-on task。

这条顺序的逻辑是：先让 NanoBot 的单 agent loop 可靠、可观测、可持久，再上多 agent、always-on 和复杂 workspace。
