# ADR-002：先稳定 runtime API 与 SSE 事件

日期：2026-06-08

## 背景

DeepSeek-GUI/Kun、CodeWhale、PilotDeck 都证明了一个共同点：UI 不应该直接理解 agent loop 内部。前端、CLI、未来 WinUI/WPF 都应该通过稳定 runtime API 获取事件、状态和历史。

## 决策

NanoBot P6 后优先稳定：

- HTTP runtime API。
- SSE event stream。
- JSONL transcript。
- session/thread/turn/item 数据模型。
- approval/user_input/interrupt contract。

第一批事件建议：

```text
run.started
content.delta
content.completed
reasoning.delta
reasoning.completed
tool.started
tool.delta
tool.completed
tool.failed
usage.updated
cache.updated
approval.requested
user_input.requested
run.interrupted
run.failed
run.completed
```

## 影响

- WebUI 不解析最终回答来猜工具状态。
- 会话恢复和断线续传以 JSONL/SSE replay 为基础。
- 后续 WinUI/WPF 原生客户端只调用 NanoBot API，不复制 agent loop。
- CLI/WebUI/插件状态/调试日志共用同一套事件。

