# P6 智能体项目调研 Wiki

调研日期：2026-06-08

P6 的任务不是立刻堆新功能，而是把外部智能体项目拆清楚：它们各自有什么功能、有什么工具、前后端或入口和 runtime 是怎么贯穿的、哪些设计能进入 Nong.NanoBot.Net，哪些只能看不能碰。

## 研究副本

研究目录：

```text
C:\Users\Administrator\Documents\Github\_agent-research-p6
```

已拉取并删除 `.git` 的项目：

- `DeepSeek-GUI.net`
- `EvoScientist.net`
- `soloncode.net`
- `GenericAgent.net`
- `agent-framework.net`
- `PilotDeck.net`
- `CodeWhale.net`

说明：DeepSeek-TUI 已改名为 CodeWhale，本轮以 `CodeWhale.net` 作为名称记录。

## 主结论

Nong.NanoBot.Net 继续作为 Nong.Toolkit.Net / Nong.Cli.Net agent runtime 主线。外部项目不作为替代主仓库，而是作为教材：

- CodeWhale.net：学习 DeepSeek V4 Flash/Pro、1M 上下文、reasoning、cache hit/miss、runtime event、任务和子 agent。
- DeepSeek-GUI.net / Kun：学习工作台信息架构、本地 HTTP/SSE runtime、`events.jsonl` replay、工具详情、计划/Todo/Goal。
- PilotDeck.net：学习 Workspace、白盒 memory、ToolRuntime/PermissionRuntime、模型路由、always-on 的产品思想。
- GenericAgent.net：学习 9 个原子工具、分层 memory、自进化 skill、Morphling/Goal Hive。
- EvoScientist.net：学习科研 workflow、多 agent team、多渠道 MessageBus、approval/ask_user/thinking stream。
- soloncode.net：学习中文 CLI/Web/IDE 贯通、ReActAgent 扩展、配置项和 HITL。
- agent-framework.net：学习 .NET 抽象、workflow、HITL、OpenTelemetry、durable/checkpoint、prompt injection 防御。

## Wiki 目录

- [对比框架](01-comparison-framework.zh-CN.md)
- [能力矩阵](02-capability-matrix.zh-CN.md)
- [P6 后施工路线](10-p6-roadmap.zh-CN.md)

项目分册：

- [CodeWhale.net](03-project-scorecards/codewhale.zh-CN.md)
- [DeepSeek-GUI.net / Kun](03-project-scorecards/deepseek-gui-kun.zh-CN.md)
- [PilotDeck.net](03-project-scorecards/pilotdeck.zh-CN.md)
- [GenericAgent.net](03-project-scorecards/genericagent.zh-CN.md)
- [EvoScientist.net](03-project-scorecards/evoscientist.zh-CN.md)
- [soloncode.net](03-project-scorecards/soloncode.zh-CN.md)
- [agent-framework.net](03-project-scorecards/agent-framework.zh-CN.md)

决策记录：

- [ADR-001：Nong.NanoBot.Net 保持主线](04-decisions/adr-001-nanobot-stays-mainline.zh-CN.md)
- [ADR-002：先稳定 runtime API 与 SSE 事件](04-decisions/adr-002-runtime-api-and-sse-first.zh-CN.md)
- [ADR-003：DeepSeek V4 Flash 作为一等模型](04-decisions/adr-003-deepseek-v4-flash-first-class.zh-CN.md)

## 代码复用边界

P6 只吸收架构思想、交互模式、数据模型和工程取舍，不复制外部项目源码、品牌、素材或 prompt 文案。

尤其注意：

- PilotDeck.net 是 AGPL-3.0，高风险，只学设计。
- DeepSeek-GUI.net 的 Electron/WebView 桌面路线不进入 NanoBot。
- CodeWhale.net 是 Rust TUI 主线，不能把 NanoBot 改成第二个 Rust runtime。
- agent-framework.net 很重，只吸收 .NET production-grade 抽象，不把 NanoBot 绑进 Azure/Foundry 重生态。

