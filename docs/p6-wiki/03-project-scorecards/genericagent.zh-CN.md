# GenericAgent.net Scorecard

## 定位

GenericAgent 是极简、自进化、自举型 Python agent。它的核心价值不是 UI 或工程化 runtime，而是 9 个原子工具、分层 memory 和把经验沉淀成 skill/SOP 的方式。

## 功能

- 极简 agent loop。
- 多前端：CLI、Textual TUI、PyQt、Streamlit、Telegram、desktop static。
- 会话恢复：`/continue`。
- L0-L4 分层 memory。
- 自进化 skill / SOP。
- Morphling mode：吸收外部项目能力。
- Goal Hive：多 worker 协作。
- 真实浏览器控制、系统控制、ADB 等宽工具能力。

## 工具

README 和 schema 中的 9 个原子工具：

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
assets/tools_schema.json
assets/tools_schema_cn.json
memory/
memory/morphling_sop.md
memory/goal_hive_sop.md
memory/memory_management_sop.md
reflect/goal_mode.py
frontends/tuiapp_v2.py
frontends/qtapp.py
frontends/desktop/static/app.js
```

## 贯穿方式

```text
CLI / TUI / GUI / IM
  -> Agent instance
  -> minimal loop
  -> LLM session
  -> 9 atomic tools
  -> memory files / SOP / skill
  -> frontend streaming queue
```

## NanoBot 可吸收

- 工具集最小化原则：先让少数原子能力稳定，再扩展工具生态。
- `ask_user` 作为一等工具，而不是临时异常。
- `update_working_checkpoint` 思想可转成 NanoBot run checkpoint。
- 分层 memory：规则、索引、事实、skill、会话归档。
- Morphling：外部项目调研时先提取目标、测试、能力边界，再决定吸收或舍弃。
- Goal Hive：多 agent 协作可先用简单公告板/任务队列，不急着引入复杂框架。

## 风险

- `code_run`、浏览器、系统控制、ADB 权限过宽。
- runtime API 和持久事件模型不如 CodeWhale/Kun/PilotDeck 清晰。
- Python GUI/IM 代码不能作为 NanoBot 主线。
- 自进化 memory 要有审计和回滚，否则容易把错误经验固化。

