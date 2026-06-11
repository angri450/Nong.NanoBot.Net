# 2026-06-08 P7 Runtime Engineering 施工安排

## 背景

P6 已完成外部智能体项目调研，并固定 Nong.NanoBot.Net 不换主线。P7 开始把调研结论转成 NanoBot 自身 runtime 的施工计划。

## 决策

P7 不再继续横向调研，重点进入工程落地：

- DeepSeek V4 Flash / Pro 一等模型能力。
- Stable Context Renderer 和 prefix fingerprint。
- Runtime event contract。
- Session JSONL / thread-turn-item 持久化。
- ToolRuntime / PermissionRuntime。
- WebUI 展示 reasoning、tool timeline、usage/cache。

## 新增文档

```text
docs/p7-runtime-engineering-plan.zh-CN.md
```

## 施工顺序

1. DeepSeek usage/cache DTO 和 provider gate。
2. RuntimeEvent 扩展和 sequence。
3. JSONL event store。
4. ContextRenderer fingerprint。
5. DeepSeek V4 streaming parser。
6. ToolRuntime/PermissionRuntime。
7. WebUI event/timeline/usage/cache 展示。
8. GitCode `deepseek-v4-flash` profile 绑定。

## 边界

- 不引入 Electron/WebView。
- 不复制 AGPL 代码。
- 不做无人值守 always-on 自动改 workspace。
- 不把 Nong.Toolkit.Net/Nong 打包进主安装包。

