# Nong.NanoBot.Net Agent 编程行动规划

> 记录日期：2026-06-05
> 背景文档：`changelog/2026-06-01-research-report.md`
> 目标：把调研报告转化为后续可交给 agent 编程执行的任务路线。

---

## 一、命名逻辑

后续研究、决策、迁移记录统一放入 `changelog/`，不再使用独立 `docs/` 目录。

文件命名采用：

```text
YYYY-MM-DD-topic.md
```

规则：

1. 日期放在最前面，保证文件列表天然按时间排序。
2. topic 使用小写英文短语，用连字符连接，避免空格和中文文件名。
3. 调研、规划、执行记录都放在同一目录内，但通过 topic 区分类型。

当前已合并：

| 原路径 | 新路径 | 说明 |
|---|---|---|
| `docs/research-report-2026-06-01.md` | `changelog/2026-06-01-research-report.md` | Nong.NanoBot.Net 与 Python 原版差距调研报告 |

---

## 二、当前判断

`Nong.NanoBot.Net` 不只是缺少新功能，本记录初始版本确认当前基线已经不可构建。`dotnet test` 失败，原因是 `Nanobot.Core.Memory` 命名空间、`IMemory`、`FileMemoryStore` 被引用但项目中不存在。

2026-06-05 施工更新：已补齐最小 `Nanobot.Core.Memory`，删除空占位测试，`dotnet test` 已恢复通过。

因此后续 agent 编程不能直接从 SSRF、streaming 或 Skill 系统开始，必须先建立一个可验证的干净基线。

关键判断：

1. 当前 .NET 版仍是单 Agent 循环，不具备 Python 原版 v0.2.1 的 Runner、Loop、Session、Hook、Runtime Events 等核心边界。
2. 后续迁移应优先做架构边界，而不是逐个堆工具。
3. 每个任务必须能独立构建、独立测试、独立回滚，避免 agent 一次性大范围重写。
4. Python 原版只能作为行为参考，不能机械翻译到 .NET。

---

## 三、优先级路线

### P0-0：恢复可构建基线

状态：已完成。

目标：

- 补齐或重建 `Nanobot.Core.Memory`。
- 让 `dotnet test` 通过。
- 删除无意义占位代码或补充最小测试。

验收：

```bash
dotnet test
```

完成记录：

- 新增 `IMemory` 与 `FileMemoryStore`。
- `FileMemoryStore` 读取 `<workspace>/memory/MEMORY.md`，有内容时注入 `## Long-term Memory` 上下文。
- 新增 memory 单元测试，替换空占位测试。

### P0：安全和稳定性

目标：

- 为 `web_fetch` 增加 SSRF 防护，覆盖 loopback、private network、IPv6、redirect。
- 为 shell 执行增加工作区边界、超时、输出限制。
- 为工具错误返回建立结构化错误格式。

参考：

- Python 原版：`nanobot/agent/tools/web.py`
- Python 原版：`nanobot/security/network.py`
- Python 原版：`nanobot/agent/tools/sandbox.py`

### P1：核心用户体验

目标：

- Provider 接口支持 streaming。
- CLI 能边生成边输出。
- Agent 循环支持结构化错误、retry metadata、空响应恢复。
- 建立 session 级锁，避免并发写入历史。

参考：

- Python 原版：`nanobot/providers/base.py`
- Python 原版：`nanobot/agent/runner.py`
- Python 原版：`tests/test_api_stream.py`

### P2：架构对齐

状态：已完成。

目标：

- 拆分 `Agent` 为 `AgentLoop` 与 `AgentRunner`。
- 引入 Hook 生命周期。
- 引入 Runtime Event Bus。
- 引入简化版 Skill Loader。
- 为后续 Sub-agent 做上下文隔离准备。

参考：

- Python 原版：`nanobot/agent/loop.py`
- Python 原版：`nanobot/agent/runner.py`
- Python 原版：`nanobot/agent/hook.py`
- Python 原版：`nanobot/bus/runtime_events.py`
- Python 原版：`nanobot/agent/skills.py`

建议任务拆分：

1. `P2-0 AgentRunner 抽取`
   - 新增 `AgentRunner`，只负责 provider 对话、工具调用循环、工具结果截断与最终响应。
   - 保留现有 `Agent.RunAsync(string input)` 作为兼容门面，避免 CLI 和 Telegram 网关同步大改。
   - 验收：使用 fake provider 和 fake tool 覆盖无工具调用、单工具调用、多轮工具调用、达到最大轮次四种路径。

2. `P2-1 AgentLoop 边界`
   - 新增 `AgentLoop`，负责构建系统提示词、读取 memory、维护短期历史、调用 `AgentRunner`。
   - 将现有 `Agent` 缩减为组合 `AgentLoop` 的外层入口。
   - 验收：现有 CLI 行为不变，history 截断和 memory 注入仍由测试覆盖。

3. `P2-2 Runtime Event Bus`
   - 新增进程内 `RuntimeEventBus`，事件至少覆盖 run started/completed/failed、tool started/completed/failed。
   - 默认实现为内存发布订阅，不引入外部队列。
   - 验收：单元测试断言事件顺序和失败事件 payload，不改变用户输出。

4. `P2-3 Hook 生命周期`
   - 新增 `IAgentHook`，至少包含 run/tool 的 before、after、error 生命周期方法。
   - `AgentLoop` 和 `AgentRunner` 在对应阶段触发 hook；默认无 hook 时零额外行为。
   - 验收：hook 可观察并修改或拒绝工具调用；异常路径不会吞掉原始错误。

5. `P2-4 简化版 Skill Loader`
   - 新增 `SkillLoader`，扫描 `<workspace>/skills/*/SKILL.md`，按目录名排序读取。
   - 暂不实现 BM25 路由，只把读取到的 skill 内容合并进系统提示词，并设置总字符上限。
   - 验收：覆盖无 skill、单 skill、多 skill 排序、超长 skill 截断。

6. `P2-5 Sub-agent 上下文隔离预留`
   - 新增 `AgentExecutionContext`，包含 `SessionId`、`Workspace`、`IsEphemeral`、`ParentRunId`、允许工具集合。
   - 暂不实现 spawn 工具，只让 `AgentLoop`、`AgentRunner` 接收并传递该上下文。
   - 验收：默认根上下文行为与现有 Agent 一致；测试覆盖不同上下文不会共享短期历史。

P2 非目标：

- 不新增 MCP、WebUI、多 channel 或 provider registry。
- 不把 Python 原版 `SessionManager`、Dream、auto-compaction 一次性搬入 .NET。
- 不破坏 `Agent.RunAsync(string input)`、CLI `chat`、CLI `agent --message` 的现有调用方式。

完成记录：

- 新增 `AgentRunner` 与 `AgentLoop`，保留 `Agent.RunAsync(string input)` 兼容入口。
- 新增 `AgentExecutionContext`，支持 session、workspace、ephemeral、parent run、allowed tools。
- 新增 `RuntimeEventBus`，覆盖 run/tool started/completed/failed。
- 新增 `IAgentHook`，覆盖 run/tool before、after、error 生命周期。
- 新增 `SkillLoader`，读取 `<workspace>/skills/*/SKILL.md` 并注入系统提示词。
- 新增 P2 单元测试，覆盖 Runner、Loop、事件、Hook、Skill、上下文隔离。

详细记录：`changelog/2026-06-05-p2-completion-and-p3-plan.md`

### P3：能力扩展

状态：已完成。

目标：

- Provider registry 与 fallback chain。
- Anthropic、Azure OpenAI、OpenAI-compatible provider 分层。
- MCP 工具接入。
- Blazor WebUI 或轻量 WebSocket Gateway。

完成记录：

- 新增 Provider registry、provider descriptor、fallback chain。
- 新增 OpenAI-compatible provider 分层，保留 `OpenAIProvider` 兼容包装。
- 新增 Anthropic 与 Azure OpenAI 非流式 provider。
- 新增 MCP stdio client、MCP tool provider、MCP tool adapter。
- 新增轻量 WebSocket gateway，并在 CLI 增加 `websocket` 命令。
- 新增 P3 单元测试，覆盖 provider、HTTP 请求/解析、MCP、WebSocket 协议。

详细记录：

- `changelog/2026-06-05-p2-completion-and-p3-plan.md`
- `changelog/2026-06-05-p3-completion.md`

### P4：生产化与安全补齐

状态：已完成。

目标：

- 为 `web_fetch` 增加 SSRF 防护，覆盖 loopback、private network、IPv6、redirect。
- 为 shell 执行增加工作区边界、超时、输出限制。
- 为工具错误返回建立结构化错误格式。
- 重做英文 README 与中文 README。
- 固化 P4 工作量和后续边界。

完成记录：

- 新增 `NetworkSecurityGuard`，阻止受限地址和 redirect SSRF。
- `WebFetchTool` 改为手动 redirect，并在每一跳前校验目标。
- `ShellTool` 支持 workspace 边界、`workingDirectory`、`timeoutMs`、`maxOutputChars`。
- 新增 `ToolExecutionResult`，`ToolRegistry` 和 `AgentRunner` 接入结构化工具错误。
- README 重写为英文主文档，新增 `README.zh-CN.md` 中文版。
- 新增 P4 单元测试，测试总数提升到 39。

详细记录：

- `changelog/2026-06-05-p4-completion-and-worklog.md`

### P5：CI、配置化、Release、Streaming、Gateway 认证与真实集成测试

状态：已完成。

目标：

- 参考 cherry-studio 的 provider/model 分离设计，建立稳定模型引用 `providerId::modelId`。
- 将 provider、model、fallback chain、streaming、gateway auth 配置化。
- OpenAI-compatible provider 支持 streaming，Agent/CLI/WebSocket 接入流式输出。
- WebSocket gateway 支持 token 认证。
- 新增 CI、release、真实集成测试入口。
- 再次重做 README 英文版与中文版。

完成记录：

- 新增 `ModelReference`、`ProviderConfigurationFactory`、`ModelBoundLLMProvider`。
- 扩展 `AppConfig`，支持 provider kind、models、`apiModelId`、fallbackModels、streaming、gateway token。
- CLI 改为统一配置解析，环境变量仍优先。
- 新增 `LLMStreamChunk` 与 `IStreamingLLMProvider`。
- `OpenAICompatibleProvider` 接入 `CompleteChatStreamingAsync`。
- `AgentRunner`、`AgentLoop`、`Agent` 新增 streaming API。
- CLI 交互聊天和单次消息支持流式输出。
- WebSocket gateway 支持 bearer/query token，并新增 `delta` protocol。
- 新增 `.github/workflows/ci.yml`、`integration.yml`、`release.yml`。
- 新增真实集成测试入口，默认不访问外部 API。
- README 英文版与中文版按 P5 状态整体重写。
- 测试总数从 39 提升到 55。

详细记录：

- `changelog/2026-06-05-p5-completion-and-worklog.md`

---

## 四、Agent 任务包格式

后续每次交给 agent 编程时，建议固定使用以下任务书格式：

```markdown
# Task: <短标题>

## Goal
说明本次只要完成什么。

## Scope
允许修改哪些文件或模块。

## References
- Python 原版参考文件
- .NET 当前目标文件

## Non-goals
明确本次不做什么，防止 agent 顺手重构。

## Acceptance Criteria
- 可观察行为
- 必须新增或修改的测试
- 必须通过的命令

## Notes
兼容性、安全边界、命名约定。
```

---

## 五、下一步执行顺序

1. `P0-0` 已完成：恢复 `Memory` 模块并让项目可构建。
2. 为 `WebFetchTool` 建立 SSRF 测试，再实现防护。
3. 为 `ShellTool` 建立超时、工作区、输出截断测试，再实现限制。
4. 拆 Provider streaming 合约，不直接改 CLI 体验。
5. 在 streaming 合约稳定后接入 CLI 流式输出。
6. `P2` 已完成。
7. `P3` 已完成。
8. `P4` 已完成。
9. `P5` 已完成。

---

## 六、执行原则

1. 每个 PR 或 agent 任务只解决一个主题。
2. 所有行为变化先写测试或补验收命令。
3. 不追求一次追平 Python 原版，优先追平安全底线和核心体验。
4. 不把 WebUI、MCP、多 Channel 提前混入核心重构。
5. 每次移植都在 changelog 增加记录，写清楚参考的 Python 原版基线。
