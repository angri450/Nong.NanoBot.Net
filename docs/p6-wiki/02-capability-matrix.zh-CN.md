# P6 能力矩阵

| 项目 | 流式/Reasoning | 会话/记忆 | 工具/MCP/Skill | Workspace/安全 | UI/Runtime 边界 | 许可风险 | NanoBot 吸收优先级 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| CodeWhale.net | 强，支持 reasoning stream、tool call stream、usage/cache | 强，会话、任务、memory、skills | 强，文件、shell、MCP、subagent、task、RLM、hooks | 中高，exec policy、snapshot、LSP diagnostics | TUI + HTTP/SSE runtime API | MIT，思想可学 | 最高：DeepSeek V4、cache、runtime event |
| DeepSeek-GUI.net / Kun | 强，SSE text/reasoning/tool/usage/completed | 强，`messages.jsonl`、`events.jsonl`、memory | 强，文件、shell、web、MCP 聚合、plan/todo/goal | 强，workspace path guard、read-before-edit | Renderer -> IPC -> Kun HTTP/SSE runtime | MIT，但 Electron 不采用 | 最高：WebUI 信息架构、SSE replay |
| PilotDeck.net | 强，统一 streaming model runtime | 强，JSONL transcript、EdgeClaw white-box memory | 强，ToolRuntime、PermissionRuntime、MCP、Skill、Always-on | 强，workspace roots、fresh read snapshot、权限模式 | Gateway 统一 Web/CLI/TUI/IM | AGPL-3.0，高风险 | 高：workspace、permission、memory 思想 |
| GenericAgent.net | 中，前端 streaming queue | 强，L0-L4 memory、skill SOP、Goal Hive | 中高，9 原子工具、自进化 skill | 中，工具权限偏宽 | CLI/TUI/PyQt/Streamlit/IM，runtime 边界较轻 | MIT | 中高：极简工具、skill/memory |
| EvoScientist.net | 强，thinking/tool/subagent/approval/ask_user stream | 中高，科研记忆和 channel session | 强，DeepAgents tools、MCP、EvoSkills、子 agent | 中，Python 运行时需谨慎 | CLI/TUI/WebUI/channel -> MessageBus -> LangGraph | Apache-2.0 | 中：多 agent workflow、MessageBus |
| soloncode.net | 中高，Java stream trace | 中，memoryEnabled/memoryIsolation | 中高，ReActAgent、skills、MCP、channel | 中，Java/Solon 体系 | CLI/Web/Desktop/ACP 三端 | MIT | 中：中文体验、三端贯通 |
| agent-framework.net | 强，框架级 streaming/workflow | 强，checkpoint、durable、compaction | 强，MCP、skills、tool abstractions | 强，HITL、prompt injection ADR、observability | .NET/Python framework + hosting | MIT | 高：.NET 抽象、workflow、HITL |

## 共识能力

多个项目反复出现，应该优先进入 NanoBot：

- 统一 runtime API：UI/CLI/未来 WinUI 都只调用 runtime API。
- SSE 事件流：`content.delta`、`reasoning.delta`、`tool.started`、`tool.completed`、`usage.updated`、`approval.requested`、`user_input.requested`。
- JSONL transcript：会话恢复、断线 replay、调试和审计都靠它。
- ToolRuntime + PermissionRuntime：工具注册、schema validation、approval、audit、输出裁剪、失败恢复。
- Workspace isolation：所有文件、shell、Nong、plugin 能力都必须有 workspace 边界。
- Read-before-edit / fresh read snapshot：修改文件前必须有足够新的读取依据。
- DeepSeek usage/cache metrics：解析并展示 `prompt_cache_hit_tokens`、`prompt_cache_miss_tokens`、`reasoning_tokens`。
- 白盒 memory：能看、能编辑、能删除、能回滚，并能追踪来源。
- Plugin/Skill marketplace：GroundPA-Toolkit 和 Nong 通过 `plugin.json` 安装，不内置完整负载。

## 暂缓能力

这些能力有价值，但不应在 P6 后第一批施工：

- Always-on 自动改工作区。
- 多 IM 渠道全量接入。
- 复杂 Agent OS / Gateway 全量复刻。
- EdgeClaw 级别的重 LLM memory pipeline。
- 自动工具选择的额外 LLM 路由，除非工具数量已经明显过多。
- 桌面 Electron/WebView 壳。

