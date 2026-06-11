# Nong.NanoBot.Net 施工规范

这个文件只固定开发节奏，不做过多产品约束。原则是：先对齐方向，快速施工，验证结果，留下记录。

## 施工顺序

1. 先看指导文件
   - `AGENTS.md`
   - `DEVELOPMENT_PLAN.zh-CN.md`
   - 当前任务相关的 `docs/` 或 `changelog/`

2. 明确本次目标
   - 判断这次改的是 NanoBot 主线，还是外部参考项目调研。
   - 外部项目只作为参考，默认不改主线目标。
   - 能直接推进就直接做，不为小改动写长方案。

3. 开始施工
   - 优先沿用现有代码结构和风格。
   - 代码改动保持聚焦，避免顺手大重构。
   - API key、OAuth token、refresh token、用户本地 auth 文件绝不进仓库。

4. 验证
   - 有代码改动时优先跑 `dotnet build` 和 `dotnet test`。
   - WebUI 改动尽量做桌面和窄屏检查。
   - 文档-only 改动至少跑 `git diff --check`。

5. 写记录
   - 重要阶段、架构决策、模型接入、安装分发、WebUI 变化，都写 dated changelog。
   - README 只在用户可见行为变化时更新。

6. 收尾
   - 检查 `git status`。
   - 提交信息简洁说明结果。
   - 用户要求推送时，完成后 push。

## 默认判断

- Nong.NanoBot.Net 是当前主线。
- 默认技术基线是 .NET 8、Apache-2.0、本地优先。
- WebUI 中文优先，深色/浅色都要能用。
- Nong.Toolkit.Net / Nong.Cli.Net 走 plugin/bootstrap，不默认打包进主安装包。
- GitCode/CodingPlan 可以积极接入，但不复制或逆向私有签名实现。
