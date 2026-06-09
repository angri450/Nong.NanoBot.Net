# P6 对比框架

本框架用于判断外部项目哪些能力值得进入 NanoBot.net，避免凭 UI 好不好看或模型能不能白嫖来决定主线。

## 评分维度

| 维度 | 观察点 | 对 NanoBot 的意义 |
| --- | --- | --- |
| 主线匹配度 | 是否符合 .NET 8、本地优先、CLI first、WebUI second、Apache-2.0 | 决定能不能成为主线候选 |
| Runtime 边界 | HTTP/SSE/WebSocket、thread/run/event/session 数据模型 | 决定 WebUI/CLI/未来原生 UI 能否共用 runtime |
| Agent loop | 多轮工具、流式、审批、中断、fork/resume、用户输入 gate | 决定智能体是否真正可用 |
| 模型与上下文 | DeepSeek V4、1M context、reasoning、cache hit/miss、compaction | 决定能否吃满 DeepSeek V4 Flash |
| 记忆体系 | 白盒记忆、Dream/归纳、可编辑/可回滚、workspace 隔离 | 决定 GroundPA 是否可持续工作 |
| Tools/MCP/Skills | 工具注册、MCP transport、skill/plugin manifest、marketplace | 决定 Nong/GroundPA-Toolkit 怎么接入 |
| 安全边界 | workspace 限制、审批、沙箱、SSRF、prompt injection、密钥处理 | 决定能否放心在本机执行 |
| UI 参考价值 | 会话、工具详情、文件树、配置、主题、中文体验 | 决定 WebUI 怎么落地 |
| 分发运维 | Windows 安装、local data layout、diagnostics、任务队列 | 决定 MSI 和本地运行体验 |
| 许可证风险 | MIT/Apache/AGPL、能否直接复用代码 | 决定只能学思想还是可参考实现 |

## 判断规则

1. NanoBot.net 默认保持主线，除非有项目同时满足技术栈、许可、产品目标和本地优先。
2. 可吸收能力优先级高于项目替换。一个项目只能贡献局部能力，不代表要成为主仓库。
3. 多个项目共同出现的能力优先进入 NanoBot，例如 runtime event、JSONL transcript、tool approval、workspace isolation、memory 可视化。
4. DeepSeek V4 Flash / Pro 的模型协议是 P6 后第一优先，因为它影响上下文、工具调用、缓存命中和成本。
5. UI 只学信息架构和体验，不复制 Electron/WebView 路线。
6. AGPL 项目只记录思想，不复制实现。

## 本轮分类

| 类别 | 首选参考 | 辅助参考 |
| --- | --- | --- |
| DeepSeek V4 / 长上下文 | CodeWhale.net | DeepSeek-GUI.net / Kun |
| Runtime API / SSE | DeepSeek-GUI.net / Kun | CodeWhale.net, PilotDeck.net |
| ToolRuntime / Permission | PilotDeck.net | DeepSeek-GUI.net / Kun, agent-framework.net |
| 白盒 Memory | PilotDeck.net | GenericAgent.net, EvoScientist.net |
| 极简工具与 skill | GenericAgent.net | PilotDeck.net, DeepSeek-GUI.net / Kun |
| 多 agent workflow | EvoScientist.net | CodeWhale.net, agent-framework.net |
| .NET production 抽象 | agent-framework.net | NanoBot 现有 Core |
| 中文三端体验 | soloncode.net | DeepSeek-GUI.net |

