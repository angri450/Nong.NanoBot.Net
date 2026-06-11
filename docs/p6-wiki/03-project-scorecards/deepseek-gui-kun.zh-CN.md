# DeepSeek-GUI.net / Kun Scorecard

## 定位

DeepSeek-GUI.net 是 Electron + React 桌面 AI 工作台，核心运行时是内置 `kun/` agent runtime。对 NanoBot 最有价值的是工作台信息架构和 GUI 到 runtime 的 HTTP/SSE 边界。

NanoBot 不采用它的 Electron/WebView 桌面路线。

## 功能

- 流式输出：text、reasoning、tool_call、usage、completed 事件。
- 会话持久化：`threads/{threadId}/messages.jsonl`、`events.jsonl`，`index.sqlite3` 可重建索引。
- SSE 断线续传：支持 `since_seq` / `Last-Event-ID`。
- 工作区文件能力：read/write/edit/ls/find/grep。
- Shell 长任务：run/poll/write/stop。
- Web search/fetch。
- MCP 聚合：`mcp_search`、`mcp_describe`、`mcp_call`、`mcp_refresh_catalog`。
- 计划、Todo、Goal、Review、diff 审查。
- 记忆：`memory_create/update/delete`，检索后注入上下文。
- 中英文和主题设置。

## 工具

主要工具：

```text
read
write
edit
ls
find
grep
bash
web_search
web_fetch
create_plan
todo_list
todo_write
get_goal
create_goal
update_goal
user_input
memory_create
memory_update
memory_delete
mcp_search
mcp_describe
mcp_call
mcp_refresh_catalog
delegate_task
```

关键路径：

```text
src/renderer/src/agent/kun-runtime.ts
src/preload/index.ts
src/main/runtime/kun-adapter.ts
src/main/runtime-sse-ipc.ts
src/main/kun-process.ts
kun/src/server/runtime-factory.ts
kun/src/server/routes/index.ts
kun/src/server/routes/events.ts
kun/src/server/sse.ts
kun/src/loop/agent-loop.ts
kun/src/adapters/model/deepseek-compat-model-client.ts
kun/src/adapters/tool/local-tool-host.ts
kun/src/cache/immutable-prefix.ts
kun/src/cache/tool-catalog-fingerprint.ts
kun/src/contracts/events.ts
kun/src/contracts/turns.ts
kun/src/contracts/threads.ts
kun/src/contracts/usage.ts
docs/kun-architecture.md
docs/kun-cache-optimization.md
```

## 贯穿方式

```text
React Renderer
  -> KunRuntimeProvider
  -> preload runtimeRequest / startSse
  -> Electron main runtime adapter
  -> kun serve HTTP/SSE
  -> AgentLoop
  -> DeepSeek-compatible model client
  -> LocalToolHost / MCP / memory / session store
```

结论：前端不直接执行 agent，真正的边界是 Kun HTTP/SSE runtime。这个边界适合 NanoBot 学习。

## NanoBot 可吸收

- 本地 HTTP/SSE runtime 边界。
- `events.jsonl` + SSE replay/live subscribe。
- `read-before-edit` 和 workspace path guard。
- MCP 聚合搜索，不一次性暴露大量 MCP tools。
- plan/todo/goal 作为可恢复状态工具。
- tool storm control、输出裁剪、参数修复。
- immutable prefix 和 tool catalog fingerprint。
- WebUI 的工作台布局：会话、聊天、文件树、工具详情、运行状态、设置、模型/Key 配置。

## 风险

- Electron/WebView 桌面壳不进入 NanoBot。
- Kun 是 TS runtime，不能直接替换 .NET Core。
- Write/FIM 绕过 Kun 的路径不适合 NanoBot，容易形成第二套模型、权限和成本逻辑。
- Shell 长任务和 `approvalPolicy:auto` 需要更严格宿主限制。
- 记忆偏轻量，不足以直接作为 Nong.Toolkit.Net 长期白盒记忆。

