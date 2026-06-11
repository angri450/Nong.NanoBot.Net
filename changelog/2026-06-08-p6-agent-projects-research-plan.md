# 2026-06-08 P6 智能体项目调研计划

## 背景

用户指出“一个问题不能有两头都是谜团”，因此 P6 不直接继续堆功能，而是先把若干有特色的智能体项目系统研究清楚，再决定 Nong.NanoBot.Net 后续吸收路线。

## 研究副本

已在仓库外创建研究目录：

```text
C:\Users\Administrator\Documents\Github\_agent-research-p6
```

已浅克隆并删除研究副本中的 `.git`：

- DeepSeek-GUI.net
- EvoScientist.net
- soloncode.net
- GenericAgent.net
- agent-framework.net
- PilotDeck.net
- CodeWhale.net

PilotDeck.net clone 时 Git LFS 额度不足，已改用 `GIT_LFS_SKIP_SMUDGE=1` 克隆源码和 LFS 指针，避免下载大媒体资源。

## 记录

- 新增 `docs/p6-agent-projects-research-wiki.zh-CN.md`。
- 新增分册版 P6 Wiki：`docs/p6-wiki/00-index.zh-CN.md`。
- 新增能力对比框架：`docs/p6-wiki/01-comparison-framework.zh-CN.md`。
- 新增能力矩阵：`docs/p6-wiki/02-capability-matrix.zh-CN.md`。
- 新增 7 个项目 scorecard，覆盖 CodeWhale、DeepSeek-GUI/Kun、PilotDeck、GenericAgent、EvoScientist、soloncode、agent-framework。
- 新增 3 条 P6 ADR，固定 NanoBot 主线、runtime API/SSE 优先、DeepSeek V4 Flash 一等模型。
- 新增 `docs/p6-wiki/10-p6-roadmap.zh-CN.md`，作为 P6 后施工路线。
- 固定 P6 的定位：Nong.NanoBot.Net 仍为主线，外部项目作为教材和能力来源。
- 梳理 7 个项目的定位、功能、工具、架构贯穿、NanoBot 可吸收点和风险。
- 初步判断：
  - CodeWhale.net 是 DeepSeek V4 Flash / 长上下文 / 缓存命中 / runtime API 最重要参考。
  - DeepSeek-GUI.net 是 WebUI 信息架构和 Kun runtime 边界的重要参考。
  - PilotDeck.net 是 WorkSpace、白盒 memory、smart routing、always-on 和 plugin 协议的重要参考。
  - GenericAgent.net 是极简原子工具和自进化 skill 的重要参考。
  - EvoScientist.net 是多 agent 科研 workflow 和多渠道 message bus 的重要参考。
  - soloncode.net 是中文 CLI/Web/IDE 三端体验和 ReActAgent 扩展参考。
  - agent-framework.net 是 .NET workflow、hosting、observability、human-in-the-loop 参考。

## 下一步

P6 调研已经形成可施工的第一版 Wiki。后续如果继续深挖，优先补每个项目的源码细节和测试用例；进入 NanoBot 施工时，从 DeepSeek V4 Flash provider、稳定上下文、runtime event model、session/thread/turn/item 持久化开始。
