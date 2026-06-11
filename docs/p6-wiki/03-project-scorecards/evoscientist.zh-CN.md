# EvoScientist.net Scorecard

## 定位

EvoScientist 是 Python 3.11+ 科研 agent，基于 DeepAgents/LangGraph。它不是通用 coding TUI，而是科研工作流、多 agent team、MCP/skills、异步任务和多渠道入口的参考。

## 功能

- 科研流程：intake、plan、execute/debug、evaluate/iterate、write report、verify。
- 6 个子 agent：planner、research、code、debug、data-analysis、writing。
- streaming：thinking、tool、subagent、approval、ask_user。
- MessageBus：多渠道 inbound/outbound queue。
- 多渠道：Telegram、Discord、Slack、Feishu、WeChat、QQ、Signal、Email 等。
- MCP：stdio/http/sse/websocket，支持 `expose_to`、allowlist、env 插值。
- EvoSkills。
- QuickJS code interpreter。
- 后台进程工具：run/check/stop/list。
- Context editing：上下文达到阈值时清理旧 tool uses。
- Tool selector：工具过多时由 LLM 选择可见工具。

## 工具

核心工具族：

```text
think_tool
tavily_search
skill_manager
DeepAgents file/execution/task tools
QuickJS code interpreter
run_in_background
check_process
stop_process
list_processes
MCP dynamic tools
channel adapters
approval / ask_user
```

关键路径：

```text
EvoScientist/EvoScientist.py
EvoScientist/prompts.py
EvoScientist/subagents/*.yaml
EvoScientist/subagents/_factory.py
EvoScientist/langgraph_dev/graphs.py
EvoScientist/langgraph_dev/manager.py
EvoScientist/mcp/client.py
EvoScientist/mcp/README.md
EvoScientist/middleware/tool_selector.py
EvoScientist/middleware/context_editing.py
EvoScientist/middleware/ask_user.py
EvoScientist/channels/bus/message_bus.py
EvoScientist/channels/consumer.py
EvoScientist/stream/events.py
EvoScientist/stream/emitter.py
EvoScientist/cli/widgets/
```

## 贯穿方式

```text
CLI / TUI / WebUI / channel
  -> MessageBus
  -> channel consumer
  -> EvoScientist agent
  -> DeepAgents / LangGraph runtime
  -> subagents / MCP / tools / middleware
  -> typed stream events back to channel
```

## NanoBot 可吸收

- 多 agent workflow 的角色拆分：planner/research/code/debug/analysis/writing。
- `think_tool` 作为强制反思工具，但要避免过度噪音。
- MessageBus：channel 不直接调用 agent 内部。
- `expose_to`：MCP 工具只暴露给 main 或特定 agent/workflow。
- approval / ask_user / thinking / subagent_text 的事件分类。
- Research/Science mode 可以作为未来模式，不改变 NanoBot 主 loop。
- 后台进程要转成 NanoBot task manager，不直接照搬 Python process 管理。

## 风险

- Python + LangGraph/DeepAgents 重依赖，不进入 NanoBot 主 runtime。
- 科研工作流强，但 NanoBot 当前主线是本地 Nong.Toolkit.Net/coding runtime。
- Tool selector 会多一次模型调用，只在工具数量明显过多时使用。
- 上下文编辑和压缩可能破坏 DeepSeek prefix cache，必须可观测。

