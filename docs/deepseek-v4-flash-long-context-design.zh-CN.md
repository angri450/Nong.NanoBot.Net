# DeepSeek V4 Flash 长上下文与缓存命中设计

调研日期：2026-06-08

本文把 DeepSeek V4 Flash 接入提升为 NanoBot.net 的主任务之一。目标不是只把模型 ID 加进列表，而是让 NanoBot 真正吃到 DeepSeek V4 Flash 的 1M 上下文、思考模式、工具调用、流式输出和上下文缓存收益。

参考资料：

- DeepSeek Models & Pricing: https://api-docs.deepseek.com/quick_start/pricing
- DeepSeek Create Chat Completion: https://api-docs.deepseek.com/api/create-chat-completion
- DeepSeek Thinking Mode: https://api-docs.deepseek.com/guides/thinking_mode
- DeepSeek Tool Calls: https://api-docs.deepseek.com/guides/function_calling
- DeepSeek Context Caching: https://api-docs.deepseek.com/guides/kv_cache
- AtomCode reference: `C:\Users\Administrator\Documents\Github\atomcode.net`
- DeepSeek-TUI reference: `C:\Users\Administrator\Documents\Github\DeepSeek-TUI`

## 结论

DeepSeek V4 Flash 应作为 NanoBot 的第一主力长上下文工具模型，而不是便宜备用模型。

核心原因：

- `deepseek-v4-flash` 官方上下文长度是 1M。
- Flash 支持 thinking 和 non-thinking 两种模式。
- Flash 支持 tool calls、JSON output、chat prefix completion。
- Flash 的 cache-hit input 价格远低于 cache-miss input，适合长会话、长项目上下文和 agent 工具循环。
- GitCode 免费模型如果最终确认为 DeepSeek V4 Flash 系列，应自动套用这套 profile。

NanoBot 后续应围绕 Flash 建一个专门 profile：

```text
id: deepseek-v4-flash
context_window: 1000000
max_output: 384000
supports_streaming: true
supports_tools: true
supports_reasoning: true
supports_interleaved_thinking: true
supports_prompt_cache_metrics: true
reasoning_effort: off | high | max
default_reasoning_effort: high
```

当前 DMX `deepseek-v4-pro-guan` 仍作为已可用 fallback。GitCode/CodingPlan 打通前，NanoBot 可以先在 DMX / OpenAI-compatible 通道上实现 DeepSeek V4 协议能力。

## 官方调用规则

### 模型与价格

DeepSeek 官方 API 当前暴露两个 V4 模型 ID：

```text
deepseek-v4-flash
deepseek-v4-pro
```

两者都是 1M context，最大输出 384K，均支持 tool calls 和 thinking mode。官方页面同时说明 `deepseek-chat` 和 `deepseek-reasoner` 将在 2026-07-24 15:59 UTC 废弃，兼容映射到 `deepseek-v4-flash` 的 non-thinking / thinking 模式。

2026-06-08 查询到的官方价格：

```text
deepseek-v4-flash
  cache hit input:  $0.0028 / 1M tokens
  cache miss input: $0.14   / 1M tokens
  output:           $0.28   / 1M tokens

deepseek-v4-pro
  cache hit input:  $0.003625 / 1M tokens
  cache miss input: $0.435    / 1M tokens
  output:           $0.87     / 1M tokens
```

价格必须当作可变配置，不要写死成业务逻辑。

### Thinking Mode

OpenAI 格式请求可使用：

```json
{
  "thinking": { "type": "enabled" },
  "reasoning_effort": "high"
}
```

规则：

- `thinking.type` 支持 `enabled` / `disabled`，默认是 `enabled`。
- `reasoning_effort` 支持 `high` / `max`。
- `low`、`medium` 会映射到 `high`；`xhigh` 会映射到 `max`。
- thinking mode 下 `temperature`、`top_p`、`presence_penalty`、`frequency_penalty` 不生效。
- 思考内容通过 `reasoning_content` 返回，与最终 `content` 平级。

NanoBot 需要把 `reasoning_content` 当成一等事件流，而不是塞进最终回答。

建议 UI 表达：

```text
思考模式：关闭 / 高 / 最大
```

内部映射：

```text
关闭 -> thinking.type=disabled, 不发送 reasoning_effort
高   -> thinking.type=enabled,  reasoning_effort=high
最大 -> thinking.type=enabled,  reasoning_effort=max
```

### Thinking + Tools 的关键坑

DeepSeek V4 thinking mode 支持工具调用，但有一个硬要求：

- 如果 assistant 这一轮没有 tool call，历史 `reasoning_content` 可以不回传；即使回传也会被 API 忽略。
- 如果 assistant 这一轮有 tool call，这条 assistant message 的 `reasoning_content` 必须在后续请求中继续回传。
- 如果工具调用历史没有正确回传 `reasoning_content`，API 会返回 400。

这意味着 NanoBot 的消息模型必须能保存：

```json
{
  "role": "assistant",
  "content": "...",
  "reasoning_content": "...",
  "tool_calls": []
}
```

现有 `Message` / `LLMResponse` 只保存 `Content`、`ToolCalls` 和 `Usage`，不够。后续需要增加：

```text
Message.ReasoningContent
LLMResponse.ReasoningContent
LLMStreamChunk.ReasoningDelta
RuntimeEventType.ReasoningDelta
```

历史回传策略：

```text
assistant with tool_calls:
  回传 content + reasoning_content + tool_calls

assistant without tool_calls:
  默认只回传 content
  reasoning_content 存本地 UI/审计，不送回 API
```

### Streaming

DeepSeek 流式返回中，`delta.reasoning_content` 和 `delta.content` 是分开的。NanoBot 需要把它们拆成两个流：

```text
reasoning.delta -> WebUI 思考块
content.delta   -> WebUI 最终回答
```

如果要在流式请求拿到完整 usage，需要发送：

```json
{
  "stream": true,
  "stream_options": {
    "include_usage": true
  }
}
```

API 会在 `[DONE]` 之前额外发送一个 usage chunk。NanoBot 必须解析这个 chunk，否则 WebUI 看不到缓存命中率。

### Usage 与缓存指标

DeepSeek usage 中有：

```text
prompt_tokens
prompt_cache_hit_tokens
prompt_cache_miss_tokens
completion_tokens
total_tokens
completion_tokens_details.reasoning_tokens
```

关系：

```text
prompt_tokens = prompt_cache_hit_tokens + prompt_cache_miss_tokens
cache_hit_rate = prompt_cache_hit_tokens / prompt_tokens
```

NanoBot 应统一把 usage 存成结构化对象，不能长期用 `Dictionary<string, int>` 凑合。最小字段：

```text
InputTokens
CachedInputTokens
UncachedInputTokens
OutputTokens
ReasoningTokens
TotalTokens
CacheHitRate
```

WebUI 必须展示：

```text
本轮缓存命中率
命中 token
未命中 token
输出 token
思考 token
会话累计命中率
```

## 上下文缓存机制

DeepSeek context cache 是服务端能力，默认开启，客户端不需要创建 cache。

命中规则的关键点：

- cache 只匹配输入前缀。
- 后续请求如果与已持久化 cache prefix unit 完全匹配，重合部分才算命中。
- 每次请求会在用户输入结束位置和模型输出结束位置生成 cache prefix unit。
- 多个请求出现共同前缀时，服务端可能把共同前缀持久化成独立 unit。
- 长输入/长输出会在固定 token 间隔切 prefix unit。
- cache 是 best-effort，不保证 100% 命中。
- cache 构建需要数秒；不用后通常数小时到数天清理。

对 NanoBot 来说，缓存命中率不是靠一个开关获得，而是靠稳定 prompt 工程获得。

## 最大化命中率的工作安排

### 一条原则

把稳定内容放前面，把变化内容放最后；已经发过的历史字节不能再变。

### 请求结构

推荐 NanoBot 渲染顺序：

```text
1. system: NanoBot 固定身份、工具纪律、安全边界
2. system/user: 项目固定指导
   - AGENTS.md
   - DEVELOPMENT_PLAN.zh-CN.md 摘要或固定片段
   - agent.md
3. system/user: 当前会话目标与长期计划快照
4. tools: 稳定排序后的工具 schema
5. messages: 已冻结的历史消息
6. user: 当前这一轮最新请求
7. user: 当前这一轮动态状态
   - 当前时间
   - 本轮文件树差异
   - 本轮插件状态
   - 本轮用户临时约束
```

不能做：

- 每轮把当前时间插进 system prompt 顶部。
- 每轮重新排序工具 schema。
- 每轮根据文件系统现状重写历史 `read_file` 结果。
- 每轮更新一个位于前缀中间的“当前状态”段落。
- 在历史消息中追加 duration、随机 ID、临时路径等易变字段。

### 会话启动策略

为了让 DeepSeek 更快进入高命中状态，NanoBot 应支持“上下文预热”。

建议流程：

1. 新会话开始时，先构建稳定前缀：系统指令、项目指导、工具 schema、会话目标。
2. 第一轮请求通常是 cache miss，这正常。
3. 第二轮开始，完整复用第一轮前缀。
4. 如果是大型项目分析，可以先做一轮“建立项目工作上下文”的轻量请求，让稳定前缀先被持久化。
5. 后续真实施工全部在同一个会话里 append，不随便开新会话。

可选命令：

```powershell
nanobot session warmup
```

WebUI 可叫：

```text
预热上下文
```

预热不是为了得到答案，而是让模型和服务端 cache 建立稳定 anchor。

### 规划工作怎么安排

Flash 的 1M 上下文适合“长线工作台”，不适合每个任务开新 chat。

推荐工作流：

```text
阶段 1：读固定指导
  读取 AGENTS.md / agent.md / DEVELOPMENT_PLAN
  固化成本轮 session anchor

阶段 2：读项目事实
  文件树、关键源码、配置、测试入口
  大输出只读必要片段

阶段 3：写计划
  计划作为一次历史消息冻结
  后续只 append 状态，不重写原计划

阶段 4：施工
  每完成一步 append 结果
  工具结果冻结
  文件变化靠新的 read_file 获取

阶段 5：验证
  build/test 输出截断且冻结
  失败信息 append，新尝试 append

阶段 6：记录
  changelog append
  最后总结 append
```

这样会形成一个越来越长但前缀稳定的会话。DeepSeek 的 cache hit 会越来越高，只有当前轮新增内容和少量动态尾部 miss。

### 工具调用策略

工具 schema 是 cache 前缀中的大块稳定内容，必须稳定：

- 工具按 name 排序。
- JSON schema 使用确定性序列化。
- 不把运行时状态塞进 schema description。
- 工具描述少改，改了就等于前缀大块失效。
- 大量 plugin 工具不要全部展开到 prompt。使用 capability catalog + 按需激活。

工具结果要冻结：

- `read_file` 返回的是当时快照，后续文件变了也不能改旧消息。
- 如果模型需要新内容，再调用一次 `read_file`，把新快照 append 到尾部。
- shell/build 输出只保存截断后的稳定文本，完整日志可落本地文件并给稳定 ref。
- 工具执行耗时、pid、临时文件路径等审计字段留在 runtime event，不进模型上下文。

### Compaction 策略

对 1M context，频繁压缩是灾难。压缩会改写历史前缀，导致缓存从变动点之后全部 miss。

NanoBot 应采用：

```text
低水位：不压缩，只 append
中水位：压缩新大工具结果，不碰旧消息
高水位：持久化 compaction，一次性生成冷摘要
极限水位：最后手段，保护最近完整 turn
```

建议初始阈值：

```text
soft_warning: 70%
prepare_compaction: 85%
persisted_compaction: 92%
hard_drop: 97%
```

压缩规则：

- 不做每轮滑动窗口重写。
- compaction 摘要一旦生成就冻结。
- 后续如果还要压缩，追加新摘要版本，不在原摘要中改数字、改列表、改顺序。
- 保留最近完整工具调用链，不能截断 assistant tool_calls 与 tool result 的配对。
- DeepSeek tool-call assistant 的 `reasoning_content` 不能丢。

### Prefix Fingerprint

为了定位 cache miss，NanoBot 应在本地记录 prefix fingerprint。

不要记录完整 prompt，只记录分段 hash：

```text
system_hash
project_guidance_hash
tool_schema_hash
frozen_history_prefix_hash
dynamic_tail_hash
sent_tokens_estimate
```

当 cache hit rate 突然下降，WebUI 可以提示：

```text
缓存命中下降：工具 schema 发生变化
缓存命中下降：历史消息被压缩
缓存命中下降：系统指令变化
缓存命中下降：新会话冷启动
```

这会让 NanoBot 的缓存优化变成可观测工程，而不是猜。

## Provider 实现路线

当前 `OpenAICompatibleProvider` 使用 OpenAI .NET SDK，适合普通兼容模型，但不够承接 DeepSeek V4 特性：

- 不稳定暴露 `reasoning_content`。
- 不稳定暴露 `prompt_cache_hit_tokens` / `prompt_cache_miss_tokens`。
- 难以控制 `thinking` extra body。
- 难以确保 streaming usage chunk 被解析。

建议新增专用 provider：

```text
Nanobot.Core/Providers/DeepSeekV4Provider.cs
Nanobot.Core/Providers/DeepSeekV4Models.cs
```

第一版直接用 `HttpClient` 发 OpenAI-compatible JSON，而不是强行绕 SDK。

请求最小形态：

```json
{
  "model": "deepseek-v4-flash",
  "messages": [],
  "tools": [],
  "stream": true,
  "stream_options": { "include_usage": true },
  "thinking": { "type": "enabled" },
  "reasoning_effort": "high",
  "max_tokens": 4096
}
```

thinking off：

```json
{
  "thinking": { "type": "disabled" }
}
```

注意：thinking mode 下不要依赖 `temperature` 生效。provider 可以在 thinking enabled 时不发送 temperature。

## NanoBot 数据结构改造

### ModelSettings

新增字段：

```text
DisplayName
ContextWindow
MaxOutputTokens
SupportsReasoning
SupportsInterleavedThinking
SupportsPromptCacheMetrics
ReasoningEffort
PlanAvailable
ProviderModelFamily
```

### Message

新增字段：

```text
ReasoningContent
Metadata
```

`ReasoningContent` 只在 provider 策略要求时回传给模型。UI/审计存储不等于 API 回传。

### LLMResponse

新增字段：

```text
ReasoningContent
Usage: LLMUsage
Model
Provider
```

### RuntimeEvent

新增事件：

```text
ReasoningDelta
ContentDelta
UsageUpdated
CacheMetricsUpdated
ModelSelected
```

## WebUI 设计

模型区要直接告诉用户：

```text
DeepSeek V4 Flash
1M 上下文
工具调用
思考模式
缓存命中率
```

每轮回答展示：

```text
思考过程：可折叠
最终回答：默认展开
工具调用：按调用链展示
缓存：命中 982,000 / 未命中 18,000 / 命中率 98.2%
```

会话顶部展示：

```text
模型：DeepSeek V4 Flash
上下文：128K / 1M
缓存状态：预热中 / 高命中 / 命中下降
思考：高
```

设置项：

```text
思考模式：关闭 / 高 / 最大
上下文预热：启用 / 禁用
缓存诊断：启用 / 禁用
工具结果冻结：启用
```

`工具结果冻结` 不应该让普通用户关闭，最多作为开发调试开关。

## GitCode / CodingPlan 对接

当 GitCode 模型目录返回 `deepseek-v4-flash` 或同类模型时：

- 自动套用 DeepSeek V4 Flash profile。
- `context_window` 优先使用 GitCode `models-v2` 返回值。
- 如果服务端没返回或返回 0，对 DeepSeek V4 Flash 使用 1,000,000。
- `plan_available=false` 的 locked 模型不进入普通模型选择器。
- GitCode provider 暂时仍保持三态：`NotConfigured` / `CatalogOnly` / `Callable`。

GitCode 免费模型一旦可调用，NanoBot 的默认路线应是：

```text
优先：gitcode::deepseek-v4-flash
回退：dmx::deepseek-v4-pro-guan
```

## 实施阶段

### P5.1：DeepSeek V4 协议层

- 新增 `DeepSeekV4Provider`。
- 支持 raw JSON chat completions。
- 支持 streaming SSE。
- 支持 `thinking.type` 和 `reasoning_effort`。
- 支持 `stream_options.include_usage`。
- 解析 `reasoning_content`。
- 解析 `prompt_cache_hit_tokens`、`prompt_cache_miss_tokens`、`reasoning_tokens`。

验收：

- 单元测试覆盖非流式 reasoning + usage。
- 单元测试覆盖流式 reasoning delta + content delta + final usage。
- 不需要真实 key。

### P5.2：消息历史与工具调用回传

- `Message` 保存 `ReasoningContent`。
- assistant tool_calls 历史回传 `reasoning_content`。
- assistant no-tool 历史默认不回传 reasoning。
- 工具调用配对 sanitize，避免 assistant tool_calls 后缺 tool message。

验收：

- 构造 DeepSeek tool-call 历史，检查发送 JSON 包含 reasoning_content。
- 构造 no-tool 历史，检查发送 JSON 不包含 reasoning_content。
- 工具结果缺失时不发送非法请求。

### P5.3：稳定前缀与缓存诊断

- 引入 context renderer。
- 固定工具 schema 排序。
- 冻结历史工具结果。
- 分段 prefix fingerprint。
- usage 入库。

验收：

- 文件被修改后，旧 `read_file` tool result 序列化字节不变。
- 连续两轮只 append 当前用户输入，前缀 hash 不变。
- 工具 schema 顺序稳定。

### P5.4：WebUI 命中率与思考块

- 显示 reasoning blocks。
- 显示 per-turn cache hit rate。
- 显示 session cache hit rate。
- 显示 cache miss 诊断提示。
- 模型设置增加思考模式选择。

验收：

- 桌面和窄屏都能看清思考、工具、缓存三类信息。
- 没有 usage 时显示“暂无缓存数据”，不报错。

### P5.5：GitCode Flash 主线

- CodingPlan 模型同步后识别 DeepSeek V4 Flash。
- 隐藏 locked 模型。
- GitCode callable 后默认优先 Flash。
- 不可调用时继续 DMX fallback。

验收：

- `plan_available=false` 不显示在普通模型选择器。
- Flash profile 自动带 1M 上下文、tools、reasoning、cache metrics。

## 测试计划

本地单元测试：

```powershell
dotnet test
```

真实 API smoke 只用环境变量：

```powershell
$env:NANOBOT_RUN_INTEGRATION_TESTS = "1"
$env:DEEPSEEK_API_KEY = "<local-secret>"
$env:DEEPSEEK_API_BASE = "https://api.deepseek.com"
dotnet test --filter DeepSeekV4IntegrationTests
```

DMX 通道测试：

```powershell
$env:NANOBOT_RUN_INTEGRATION_TESTS = "1"
$env:DMX_API_KEY = "<local-secret>"
$env:DMX_API_BASE = "https://www.dmxapi.cn/v1/"
$env:DMX_MODEL = "deepseek-v4-pro-guan"
dotnet test --filter DmxDeepSeekIntegrationTests
```

任何 key、token、auth 文件都不能进仓库。

## 当前判断

下一步优先级应调整为：

1. 先实现 DeepSeek V4 协议层和 usage/cache 解析。
2. 再改消息历史，保证 reasoning_content + tool calls 合法回传。
3. 同步做稳定前缀和缓存诊断。
4. WebUI 展示思考块与命中率。
5. GitCode 免费 Flash 通道作为 provider 来源接入。

这条路线比单纯“接 GitCode 免费模型”更重要。因为只要 DeepSeek V4 协议层和缓存工程做好，DMX、官方 API、GitCode、OpenRouter 等通道都能复用同一套 DeepSeek V4 Flash 能力。
