# ADR-001：NanoBot.net 保持主线

日期：2026-06-08

## 背景

P6 调研覆盖 CodeWhale.net、DeepSeek-GUI.net、PilotDeck.net、GenericAgent.net、EvoScientist.net、soloncode.net、agent-framework.net。它们各有强项，但没有一个同时满足 NanoBot 的目标组合：

- .NET 8
- Apache-2.0
- 本地优先
- CLI first，WebUI second
- MSI 分发
- 不使用 WebView2 / Electron 桌面壳
- GroundPA-Toolkit / Nong 通过 plugin/bootstrap 接入

## 决策

NanoBot.net 继续作为 GroundPA / Nong agent runtime 主线。外部项目只作为研究教材和局部能力来源。

## 影响

- P6 后施工继续落在 `Nanobot.Core`、`Nanobot.CLI`、`Nanobot.Web`。
- 不在外部仓库另起主线。
- 不复制外部项目源码、品牌、素材或 prompt。
- 调研结论转成 NanoBot 的 provider、runtime event、session、tool、memory、plugin、WebUI 任务。

