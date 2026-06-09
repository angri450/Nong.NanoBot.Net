# P6 后施工路线

P6 的出口不是“换项目”，而是把研究结论转成 NanoBot.net 的施工顺序。

P7 已单独拆成 runtime engineering 施工方案：

```text
docs/p7-runtime-engineering-plan.zh-CN.md
```

## P6 退出标准

- 七个项目已有 scorecard。
- 能力矩阵明确哪些能力先吸收、哪些暂缓。
- NanoBot 主线决策明确。
- P6 后第一批工程任务明确。

## 第一批施工：DeepSeek V4 Flash 与上下文

1. 实现 DeepSeek V4 专用调用层。
2. 解析并持久化 usage/cache metrics。
3. `reasoning_content` 只在 DeepSeek V4 profile 上保存和回传。
4. 建立稳定 prompt renderer：system、tool catalog、history、dynamic tail 分层。
5. 工具 schema 排序固定，生成 prefix fingerprint。
6. WebUI 展示 reasoning、cache hit rate、context usage。

## 第二批施工：Runtime 事件和会话恢复

1. 定义 session/thread/turn/item DTO。
2. 建立 JSONL transcript。
3. SSE 支持 live stream 和 replay。
4. 统一事件类型：content、reasoning、tool、usage、approval、user_input、interrupt。
5. WebUI 不再靠最终回答猜测工具状态。

## 第三批施工：ToolRuntime / PermissionRuntime

1. 工具注册统一进入 ToolRegistry。
2. 工具执行统一进入 ToolRuntime。
3. 权限策略独立成 PermissionRuntime。
4. 文件编辑加入 read-before-edit / fresh read snapshot。
5. Shell/Nong 继续使用 argument array、workspace、allowlist、timeout、输出上限。
6. 工具结果支持截断、去重、句柄化。

## 第四批施工：Memory / Plugin / GroundPA

1. 白盒 memory：查看、编辑、删除、回滚、来源 trace。
2. `plugin.json` manifest 第一版。
3. marketplace add/install/status/update。
4. GroundPA-Toolkit 作为第一块 plugin 样板。
5. Nong 能力通过 `nong commands --json` 生成 capability catalog。
6. WebUI 展示 plugin、GroundPA、Nong 的 ready/installing/failed/update 状态。

## 第五批施工：多 agent / task / always-on

1. 先做 task manager 和后台 run，不直接自动改 workspace。
2. 子 agent 先做受控 task/delegate，不做完整 Agent OS。
3. Research/Science mode 可参考 EvoScientist，但不引入 Python runtime。
4. Always-on 只先做计划、报告、人工确认，不做无人值守写入。

## 明确暂缓

- Electron/WebView 桌面客户端。
- 复制 PilotDeck AGPL 代码。
- 全量 EdgeClaw memory。
- 全量多 IM 渠道。
- 双 runtime 或第二套模型调用路径。
- 不可审计的自动 workspace mutation。
