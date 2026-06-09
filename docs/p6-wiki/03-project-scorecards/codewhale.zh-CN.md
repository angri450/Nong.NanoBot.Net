# CodeWhale.net Scorecard

## 定位

CodeWhale 是 DeepSeek-TUI 改名后的 Rust 本地 coding agent，面向 DeepSeek V4 Flash/Pro 做了很强的长上下文、缓存命中、reasoning、工具调用和 TUI/runtime 工程。

它对 NanoBot 的价值不是 Rust TUI，而是 DeepSeek V4 和 agent harness 经验。

## 功能

- 流式输出和流式 reasoning。
- DeepSeek V4 Flash/Pro 模型 profile，包含 1M context、max output、reasoning 能力。
- `reasoning_content` replay，支持工具调用后继续对话。
- `prompt_cache_hit_tokens` / `prompt_cache_miss_tokens` 成本与命中率展示。
- 会话持久化、checkpoint、crash recovery。
- HTTP/SSE runtime API。
- 子 agent、RLM、MCP、skills、hooks、LSP diagnostics。
- durable task manager、todo/plan/goal、workspace snapshot/restore。

## 工具

主要工具族：

- 文件：read/write/edit/apply_patch。
- Shell：命令执行、策略检查、输出截断。
- 搜索：grep/search/web_search/fetch_url/web_run。
- 协作：subagent、RLM、parallel。
- 任务：task、todo、plan、goal、verifier/test_runner。
- 外部：MCP、GitHub、skills、hooks。
- 上下文：remember、retrieve_tool_result、truncate/spillover。

关键路径：

```text
crates/cli/src/main.rs
crates/tui/src/main.rs
crates/tui/src/core/engine.rs
crates/tui/src/core/engine/turn_loop.rs
crates/tui/src/client.rs
crates/tui/src/client/chat.rs
crates/tui/src/models.rs
crates/tui/src/prefix_cache.rs
crates/tui/src/pricing.rs
crates/tui/src/cost_status.rs
crates/tui/src/compaction.rs
crates/tui/src/seam_manager.rs
crates/tui/src/tools/registry.rs
crates/tui/src/runtime_api.rs
crates/tui/src/runtime_threads.rs
crates/tui/src/task_manager.rs
docs/ARCHITECTURE.md
docs/RUNTIME_API.md
docs/SUBAGENTS.md
docs/MEMORY.md
```

## 贯穿方式

```text
codewhale dispatcher
  -> codewhale-tui
  -> TUI / exec / serve / ACP / MCP
  -> core engine
  -> DeepSeek-compatible client
  -> tool registry / MCP / hooks / skills / LSP
  -> session/task/runtime stores
```

单轮数据流：

```text
User input
  -> context render: static system/tool catalog + history + dynamic tail
  -> LLM SSE stream
  -> text/reasoning/tool_calls/usage
  -> execute tools with policy
  -> append tool results
  -> replay reasoning_content when required
  -> persist session/task events
```

## NanoBot 可吸收

- DeepSeek V4 Flash/Pro 专用 provider profile。
- `reasoning_content` 只在支持模型上回传，避免污染普通 OpenAI-compatible provider。
- 稳定 prompt 分层：system、tool catalog、history、dynamic tail。
- 工具 schema 固定排序，减少 cache miss。
- prefix fingerprint 诊断，不记录完整 prompt。
- usage/cache metrics 持久化和 WebUI 展示。
- 工具结果截断、去重、句柄化。
- 子 agent 后台执行和完成事件注入。
- 文件修改后接 LSP diagnostics 作为下一轮上下文。

## 风险

- Rust 架构不能直接移植。
- CodeWhale 的 TUI/Constitution/harness 风格不能照搬品牌和 prompt。
- DeepSeek 特性必须 model gate，否则其他 provider 可能拒绝 `reasoning_content`。
- 自动压缩会破坏 prefix cache，1M context 下应晚触发、可观测、可关闭。

