# 2026-06-08 DeepSeek V4 Flash 长上下文主线

## 背景

GitCode / AtomCode 的免费模型通道让 DeepSeek V4 Flash 成为 NanoBot.net 下一阶段的核心模型候选。用户明确要求把 DeepSeek 调用、1M 上下文、交错思考、工具调用和上下文命中率作为主要任务深入挖掘。

## 记录

- 新增 `docs/deepseek-v4-flash-long-context-design.zh-CN.md`。
- 将 DeepSeek V4 Flash 从“模型配置项”提升为 P5 主任务。
- 明确 NanoBot 要实现 DeepSeek V4 专用 provider，而不是只依赖通用 OpenAI-compatible SDK。
- 固定缓存命中率工程方向：稳定前缀、冻结历史工具结果、延迟 compaction、解析 cache hit/miss usage、WebUI 展示命中率。
- 明确 thinking + tool calls 的硬要求：assistant tool-call 历史必须保存并回传 `reasoning_content`。
- 更新 `DEVELOPMENT_PLAN.zh-CN.md` 的近期执行清单，把 DeepSeek V4 Flash 调用层排在 GitCode/CodingPlan 和 plugin/bootstrap 之前。

## 下一步

优先实现 DeepSeek V4 协议层、usage/cache 解析、reasoning_content 历史回传和 WebUI 命中率展示。GitCode 免费 Flash 通道作为 provider 来源接入，但不阻塞 DeepSeek V4 能力在 DMX / 官方 API 通道上先落地。
