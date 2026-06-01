# NanoBot.net 调研报告与移植建议

> 调研日期：2026-06-01
> 对比基准：lepollo/NanoBot.net (2026-02-24 停更) vs HKUDS/nanobot (2026-05-30 活跃)

---

## 一、现状速览

**NanoBot.net** 是 lepollo 在 2026 年 2 月用 6 个 commit 完成的 .NET 10 移植版，核心约 30 个 .cs 文件，不足 2000 行。它忠实复刻了当时原版 nanobot 的"极简个人助手"定位：单一 Agent 循环 + 工具调用 + 文件记忆 + Telegram 网关 + 定时任务。MIT 许可证。

但 2 月至今的 3 个月里，原版 Python 项目合并了约 44 个 PR，版本从 ~v0.1.3 涨到 v0.2.0，架构已经发生了质变。

**一句话：.NET 版停在了一个"能用但简陋"的阶段，原版已经演进成了一个多 Agent 平台。**

---

## 二、功能差距对照表

### 2.1 LLM Provider（模型供应商）

| 功能 | .NET 版 | Python 原版 | 差距 |
|---|---|---|---|
| OpenAI 兼容 | 有 (仅 OpenAI SDK) | 有 (OpenAI + 兼容接口) | 相当 |
| Anthropic Claude | 无 | 有 | 缺失 |
| Azure OpenAI (AAD) | 无 | 有 | 缺失 |
| AWS Bedrock | 无 | 有 | 缺失 |
| GitHub Copilot | 无 | 有 | 缺失 |
| xAI Grok (OAuth) | 无 | 有 | 缺失 |
| 智谱 (Zhipu) | 无 | 有 | 缺失 |
| Provider 注册表 | 无 | 有 | 缺失 |
| Fallback 降级链 | 无 | 有 | 缺失 |
| 流式输出 | 无 | 有 | **严重缺失** |

### 2.2 Agent 核心能力

| 功能 | .NET 版 | Python 原版 | 差距 |
|---|---|---|---|
| 工具调用循环 | 有 (最多 20 轮) | 有 (动态预算) | 基本相当 |
| 上下文管理 | 简单截断 (20K/15K) | auto-compaction + 智能压缩 | 差距大 |
| Hook 系统 | 无 | 有 (pre/post tool, loop detect, reflect retry) | **缺失** |
| Sub-agent 派生 | 无 | 有 (spawn 工具) | 缺失 |
| 技能系统 (Skills) | 无 | 有 (BM25-lite 路由, 提示词减 60%) | **缺失** |
| 任务规划 (Plan) | 无 | 有 | 缺失 |
| Dream 后台任务 | 无 | 有 | 缺失 |
| 事件总线 | 无 | 有 (进程内解耦) | 缺失 |
| 进度回调 | 无 | 有 (progress_hook) | 缺失 |

### 2.3 工具系统

| 工具 | .NET 版 | Python 原版 | 备注 |
|---|---|---|---|
| 文件读写 | 有 (Read/Write/Edit/ListDir) | 有 (更完善，含 apply_patch) | 可改进 |
| Shell 执行 | 有 | 有 (含 sandbox、exec_session) | 可改进 |
| Web 搜索 | 有 (Brave) | 有 (Brave + 多种后端) | 相当 |
| Web 抓取 | 有 | 有 | 相当 |
| 天气 | 有 | 无 (已移除或以 skill 形式) | - |
| 股票 | 有 | 无 (已移除或以 skill 形式) | - |
| GitHub | 有 | 无 (以 skill 形式) | - |
| MCP 协议 | 无 | 有 | **缺失** |
| CLI Apps | 无 | 有 | 缺失 |
| 图片生成 | 无 | 有 (OpenAI/Codex/Zhipu) | 缺失 |
| 消息发送 | 无 | 有 (跨 channel) | 缺失 |
| 沙盒隔离 | 无 | 有 | 缺失 |

### 2.4 Channel（消息通道）

| Channel | .NET 版 | Python 原版 |
|---|---|---|
| CLI | 有 | 有 |
| Telegram | 有 | 有 |
| Discord | 无 | 有 |
| Slack | 无 | 有 |
| 企业微信 | 无 | 有 |
| 飞书 | 无 | 有 |
| 钉钉 | 无 | 有 |
| QQ | 无 | 有 |
| 微信 | 无 | 有 |
| Matrix | 无 | 有 |
| Signal | 无 | 有 |
| WhatsApp | 无 | 有 |
| MSTeams | 无 | 有 |
| Email | 无 | 有 |
| WebSocket | 无 | 有 |

### 2.5 WebUI

| 功能 | .NET 版 | Python 原版 |
|---|---|---|
| Web 界面 | 无 | 有 (完整 SPA) |
| 项目工作区 | 无 | 有 |
| 访问控制 | 无 | 有 |
| 命令面板 | 无 | 有 |
| 斜杠动作 | 无 | 有 |

### 2.6 安全

| 项目 | .NET 版 | Python 原版 |
|---|---|---|
| SSRF 防护 | 无 | 有 (IPv6 规范化) |
| 沙盒逃逸防护 | 无 | 有 (symlink 检测) |
| WebSocket 鉴权 | - | 有 |
| 工作区权限控制 | 无 | 有 |

### 2.7 高级特性

| 功能 | .NET 版 | Python 原版 |
|---|---|---|
| Agent-to-Agent 通信 (N2N) | 无 | 有 (IPC/Redis/NATS) |
| RAG 本地检索 | 无 | 有 |
| 多模态 (音视频) | 无 | 有 |
| 插件式记忆后端 | 无 | 有 |
| 语音 (STT/TTS) | 无 | 有 |

---

## 三、问题与风险

### 3.1 .NET 版自身的质量问题

1. **流式输出缺失**：用户体验最大的问题。CLI 模式下用户要等全部完成才看到结果，体感很慢。

2. **上下文管理粗暴**：当前实现是简单的字符截断（20000 字符一刀切），没有智能压缩（compaction）。长会话会丢信息，这正是原版社区最痛的 issue #4044。

3. **错误处理弱**：Agent.RunAsync 返回 "No response." 或 "Error: xxx"，没有区分网络超时、API 限流、模型拒答等不同情况。

4. **无单元测试覆盖**：Tests 目录下只有空的 UnitTest1.cs，ToolTests.cs 只有一个占位方法。

5. **只有 OpenAI 提供商**：虽然 OpenRouter 等兼容接口能用，但没有 Anthropic 原生支持、没有 fallback 降级链。

6. **安全空白**：没有 SSRF 防护、没有 shell 沙盒、没有工作区边界控制。

### 3.2 与原版同步的风险

1. **架构不兼容**：原版的事件总线重构 (#4135) 和 Dream 调度变更 (#3990) 是破坏性的，.NET 版如果直接照搬会遇到很大的架构调整。

2. **Python 特有生态**：原版的 MCP、CLI Apps、语音等能力依赖 Python 生态（asyncio、websockets 等），.NET 移植需要找对应替代方案。

3. **维护成本**：原版有 645 个开放 PR 在并行推进，一个人不可能全部跟上。必须有取舍。

---

## 四、移植建议（按优先级排序）

### P0 —— 立即要做（安全和稳定性）

| 编号 | 项目 | 说明 | 参考 PR |
|---|---|---|---|
| P0-1 | SSRF 防护 | URL 请求前检查内网 IP、IPv6 规范化 | #4086 |
| P0-2 | Shell 沙盒 | 限制 exec 工作区边界，防 symlink 逃逸 | #4098 |
| P0-3 | WebSocket 鉴权 | Gateway 模式下的 token 校验 | #4103 |
| P0-4 | 健壮错误处理 | 区分超时/限流/拒答，加 retry 逻辑 | #2880 |

### P1 —— 尽快要做（用户体验和核心能力）

| 编号 | 项目 | 说明 | 参考 PR |
|---|---|---|---|
| P1-1 | 流式输出 | CLI 和 API 都要支持 streaming | - |
| P1-2 | 智能上下文压缩 | 替代当前的字符截断，解决长会话丢记忆 | #4044 |
| P1-3 | Session 锁 | process_direct 加 session 级别锁，防并发写 | #4080 |
| P1-4 | Heartbeat 修复 | 空任务不发送，容错传递 | #4111 |
| P1-5 | 多 Provider | Anthropic、Azure、Provider 注册表 | #3994 |

### P2 —— 应该要做（功能对齐）

| 编号 | 项目 | 说明 | 参考 PR |
|---|---|---|---|
| P2-1 | Skill 系统 | 技能定义 + 注册 + 路由（可先做简化版） | #3865 |
| P2-2 | Sub-agent (Spawn) | 派生子 agent 处理独立任务 | - |
| P2-3 | Hook 系统 | pre/post tool hook，loop detect | #3728 |
| P2-4 | Plan 工具 | 任务分解与进度跟踪 | #3791 |
| P2-5 | Discord Channel | 覆盖最广的 IM 之一 | - |

### P3 —— 锦上添花（差异化竞争）

| 编号 | 项目 | 说明 |
|---|---|---|
| P3-1 | WebUI | .NET 可以用 Blazor 做，比 Python 方案更轻 |
| P3-2 | MCP 协议 | 用 ModelContextProtocol NuGet 包 |
| P3-3 | N2N 通信 | .NET 原生 IPC (named pipe / gRPC) |
| P3-4 | RAG | 可用 Microsoft.Extensions.AI 或 OllamaSharp |

---

## 五、建议的里程碑

### Milestone 1：稳定可用（2-3 周）

完成 P0 全部 + P1-1（流式输出）+ P1-2（上下文压缩）。

达成效果：CLI 体验追平原版，安全底线补齐，可以作为日常助手稳定使用。

### Milestone 2：功能对齐（4-6 周）

完成 P1 剩余 + P2 核心项（Skill、Sub-agent、Hook）。

达成效果：核心 Agent 能力与原版 v0.2.0 对齐，可以吸引第一批用户。

### Milestone 3：差异化（8-12 周）

完成 P3 中 1-2 项。利用 .NET 的原生优势（AOT 编译、Windows 集成、Blazor WebUI）打出差异化。

---

## 六、与原版保持同步的策略

1. **Watch 原版仓库**：在 GitHub 上 watch `HKUDS/nanobot`，关注 Release 和重要 PR。
2. **选择性合并**：不是每个 PR 都要移植。重点跟安全修复 + Agent 核心逻辑变更。
3. **README 注明基线**：写明"基于 nanobot v0.2.0 的功能对齐"，让用户知道进度。
4. **向上游贡献**：如果在 .NET 版发现了通用性 bug（比如 prompt 策略问题），可以回馈给原版。
