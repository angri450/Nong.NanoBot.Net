# PilotDeck.net Scorecard

## 定位

PilotDeck 是 workspace-centric Agent OS / productivity platform，不是单一 GUI。它用 Gateway 统一 Web、CLI、TUI、IM 等入口，强调 Workspace、白盒记忆、模型路由、PermissionRuntime 和 always-on 后台任务。

它是重要设计参考，但 AGPL-3.0 使代码复用风险最高。

## 功能

- 流式调用：统一 streaming model runtime。
- 会话持久化：`~/.pilotdeck/projects/<projectId>/chats/<session>.jsonl`。
- 子 agent transcript：每个 session 下的 `subagents/`。
- 白盒记忆：turn 捕获、LLM 抽取、Markdown memory、`control.sqlite`、检索 trace、Dream mode、编辑/回滚/导入导出。
- 模型路由：catalog、fallback、retry、zero-usage retry、tokenSaver、autoOrchestrate。
- ToolRuntime：schema validation、生命周期 hook、权限 hook、audit、输出限制。
- PermissionRuntime：default/plan/acceptEdits/bypassPermissions/dontAsk。
- Workspace：roots 限制、`.git`/`node_modules`/`dist` 默认写保护、fresh read snapshot。
- Always-on：发现任务、准备 workspace、执行、报告、历史查询。

## 工具

主要工具：

```text
read_file
write_file
edit_file
edit_notebook
bash
glob
grep
web_search
web_fetch
agent
task_create
task_list
task_output
task_stop
structured_output
ask_user_question
enter_plan_mode
exit_plan_mode
todo_write
read_skill
mcp__server__tool
list_mcp_resources
read_mcp_resource
always_on_prepare_workspace
always_on_report
always_on_discovery_plan
always_on_chat_history
```

关键路径：

```text
src/cli/pilotdeck.ts
src/cli/pilotdeckServer.ts
src/cli/createLocalGateway.ts
src/gateway/server/GatewayServer.ts
src/gateway/client/InProcessGateway.ts
src/agent/session/AgentSession.ts
src/agent/turn/TurnRunner.ts
src/agent/loop/AgentLoop.ts
src/tool/execution/ToolRuntime.ts
src/tool/permission/PermissionRuntime.ts
src/tool/builtin/filesystem/pathSafety.ts
src/router/RouterRuntime.ts
src/model/ModelRuntime.ts
src/model/streaming/streamModel.ts
src/session/storage/ProjectSessionStorage.ts
src/session/transcript/JsonlTranscriptWriter.ts
src/session/resume/resumeAgentSession.ts
src/context/memory/EdgeClawMemoryProvider.ts
src/always-on/
ui/server/pilotdeck-bridge.js
```

## 贯穿方式

```text
Web / CLI / TUI / IM
  -> Gateway
  -> AgentSession
  -> TurnRunner
  -> AgentLoop
  -> RouterRuntime
  -> ModelRuntime
  -> ToolRuntime + PermissionRuntime
  -> Workspace / Memory / MCP / Skills
```

UI server 不拥有 agent runtime，只桥接 Gateway。这个模式对 NanoBot 未来多入口很有价值。

## NanoBot 可吸收

- Gateway 思想：所有入口共用 NanoBot runtime API。
- JSONL transcript + resume。
- ToolRuntime / PermissionRuntime 分层。
- 工具 audit record。
- fresh read snapshot，比普通 read-before-edit 更严格。
- RouterRuntime 的模型分级、fallback、成本/usage 统计。
- 白盒记忆的 trace、编辑、删除、回滚、导入导出产品原则。
- per-session MCP 隔离和资源读取模型。
- Always-on 的任务生命周期思想，但暂缓实现自动 workspace mutation。

## 风险

- AGPL-3.0，不复制代码。
- 架构体量大，不能把 NanoBot 早期做成完整 Agent OS。
- EdgeClaw 记忆系统重依赖 LLM，成本和延迟高。
- `bypassPermissions`、Shell、always-on workspace mutation 风险高。
- Always-on 的 git-worktree/snapshot-copy 需要清理、配额、失败恢复，早期不宜上。

