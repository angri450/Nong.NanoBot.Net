# ADR-003：DeepSeek V4 Flash 作为一等模型

日期：2026-06-08

## 背景

用户明确认为 GitCode 免费模型 DeepSeek V4 Flash 值得重点投入：1M 上下文、交错思考、工具调用、成本低。CodeWhale 和 DeepSeek-GUI/Kun 都把 DeepSeek V4 的 reasoning、cache 和长上下文作为核心能力处理。

## 决策

NanoBot P6 后把 DeepSeek V4 Flash 当成一等模型，而不是弱 fallback。

需要专用 profile：

```text
id: deepseek-v4-flash
context: 1M
supportsStreaming: true
supportsTools: true
supportsReasoning: true
supportsPromptCacheMetrics: true
supportsReasoningContentReplay: true
```

Provider 层需要支持：

- raw JSON OpenAI-compatible chat completions。
- streaming SSE。
- `thinking.type`。
- `reasoning_effort`。
- 流式 `reasoning_content`。
- 工具调用后的 `reasoning_content` replay。
- `stream_options.include_usage`。
- `prompt_cache_hit_tokens`。
- `prompt_cache_miss_tokens`。
- `reasoning_tokens`。

## 影响

- 普通 OpenAI-compatible provider 不默认发送 DeepSeek 专属字段。
- WebUI 显示思考块、cache hit rate、上下文使用量和成本。
- prompt renderer 必须稳定前缀，减少 cache miss。
- GitCode/CodingPlan 同步到 `deepseek-v4-flash` 时自动套用 Flash profile，并隐藏 locked 模型。

