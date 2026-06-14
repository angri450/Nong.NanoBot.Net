# 2026-06-13 NanoBot 稳定化与可用性硬化

## Changed

- 新建并激活 `log/plans/2026-06-13-nanobot-stabilization-hardening-plan.md`，把当前施工从已完成的应用层 phase 切换到稳定化主线。
- `Nanobot.Core/Tools/Builtin/NongTool.cs` 现在在解析 `nong commands --json` 和 `nong commands --format openai-tools` 时只处理 JSON object 节点，移除了真实的 nullable 风险点，而不是用抑制绕过去。
- 新增 `Nanobot.Web/SystemStatusProbe.cs`，把 WebUI 的系统状态探针抽成可测试 helper：
  - `nong commands --json`
  - `nong ocr models --json`
  - `dotnet tool list --global`
- 外部工具状态改为单次 `dotnet tool list --global` 解析，不再为 6 个工具重复执行 6 次相同命令。
- 修复外部工具版本解析错误：现在读取 `dotnet tool list --global` 的 version 列，而不是把命令别名误当成版本号。
- OCR/Nong 状态解析现在可容忍稀疏数组、`null` 节点和异常输出，状态面板会降级返回 unavailable，而不是让页面依赖崩掉。
- 新增 `Nanobot.Tests/SystemStatusProbeTests.cs` 覆盖外部工具版本解析、Nong roots 去重和 OCR 模型解析。
- `Nanobot.Tests.csproj` 新增 `Nanobot.Web` 引用并设置 `RollForward=LatestMajor`，确保当前环境只有较新 ASP.NET Core runtime 时也能跑测试门禁。
- 新增 `Nanobot.Core/Memory/WorkspaceBootstrapper.cs`，`onboard` 和 `FileMemoryStore` 现在都会补齐 `SOUL.md`、`USER.md`、`HEARTBEAT.md`、`memory/MEMORY.md`、`history.jsonl`、`.dream_cursor` 工作区骨架。
- `Nanobot.Core/Skills/PluginManager.cs` 不再把 Nong.Toolkit.Net marketplace 源码 zip 当成单目录技能包硬解压；现在会读取 `.claude-plugin/marketplace.json` / `plugin.json`，把 `nong-toolkit` 全量包或 `word` 这类单技能插件正确映射到 `workspace/skills/*`。
- plugin 安装现在会同时保留 `references/shared/`，使 Toolkit 技能里的 `../references/shared/nong-cli-preflight.md` 这类共享引用在 NanoBot 运行时仍然可用。
- `Nanobot.Core/Skills/SkillLoader.cs` 现在会把 `SKILL.md` 里真实存在的相对 Markdown 引用加入 reference 列表，并允许 `load_skill_reference` 读取相对于 skill 根目录的 shared reference。
- 新增 `Nanobot.Tests/PluginManagerTests.cs` 与 `SkillLoaderTests` 扩展，覆盖 marketplace 安装、bundle 卸载、shared reference 发现与读取。
- 新增 `Nanobot.Core/Config/DefaultProviderCatalog.cs`，把初装默认 provider 目录统一到 SiliconFlow-first + DMX preset。
- `ProviderConfigurationFactory` 新增 `SILICONFLOW_API_KEY` / `SILICONFLOW_API_BASE` / `SILICONFLOW_MODEL` 环境变量支持。
- `Nanobot.Web/ModelSettingsStore.cs` 让 `/api/settings/model` 基于当前 active provider 返回动态 provider/model 列表，不再把 WebUI 设置面板硬编码成 DMX-only。
- `Nanobot.CLI onboard` 现在写入与 WebUI 一致的默认模型目录、secrets 和 SiliconFlow-first runtime config。
- 新增 `Nanobot.Tests/ModelSettingsStoreTests.cs` 与 `ConfigTests` 扩展，覆盖 SiliconFlow env 覆盖、默认模板和模型设置存取。
- 新增 `Nanobot.Web/WebChatTurn.cs`，把 WebUI 聊天 turn 的 session 生命周期收拢到单点：
  - 先持久化 user 消息，再驱动 direct/streaming chat；
  - streaming delta 与 reasoning 会边到达边写入 `sessions.json`；
  - interrupted/failed turn 会留下助手侧持久化结果，而不是只剩 user 消息。
- `Nanobot.Web/Program.cs` 的 `/api/agent/stream` 不再在异常路径里二次 `ResolveSessionId`，修复浏览器中断后额外创建空会话的问题。
- `Nanobot.Web/WebSessionStore.cs` 现在支持增量更新已存在消息，并持久化 `reasoning` 字段，供 reload 后恢复 assistant reasoning 面板。
- `Nanobot.Web/wwwroot/app.js` 现在把 assistant message 内容区与 reasoning block 分离渲染，`complete` / `error` 事件不再把 reasoning DOM 覆盖掉；reload 后也会把持久化 reasoning 一起渲染出来。
- 新增 `Nanobot.Tests/WebChatTurnTests.cs`，覆盖 streaming 完成、失败、取消与 direct failure 的会话持久化行为。
- `Nanobot.Core/Sessions/SessionItem.cs` 与 `JsonlSessionStore.cs` 现在会把 runtime event 的 `sessionId` / `runId` / `eventType` / `sequence` / `errorMessage` 等关键字段持久化下来，并支持按 sequence 过滤回放，而不是把整段 thread 历史盲目重放。
- `Nanobot.Web/Program.cs` 的 `/api/events` replay 现在会跨 session 按 sequence 重建 runtime timeline，并统一 live/replay 的 SSE `id:` 为 sequence；不再出现 replay 用 GUID、live 用整数的 `Last-Event-ID` 语义错位。
- `RuntimeEventBus` 现在支持从持久化最大 sequence 继续递增，`NanobotWebRuntime` 启动时会读取历史最大值，避免服务重启后事件序号重新从 1 开始。
- 新增 `Nanobot.Tests/JsonlSessionStoreTests.cs`，覆盖 thread 内 sequence 过滤、跨 session replay 排序、tool failure metadata 持久化，以及持久化最大 sequence 读取。
- `Nanobot.Web/wwwroot/app.js` 的工具时间线现在按 runtime sequence 去重，并只展示当前 active session 的事件；foreign session 的工具事件不再往当前聊天窗追加系统提示。
- 修复 WebUI 前端启动契约缺口：`reloadStatus` / `refreshFiles` 现在显式注册进 `elements` 对象，并且兼容隐藏锚点会在 `/app.js` 加载前出现，不再因为遗漏或解析顺序问题在 `addEventListener` 阶段直接打断整份脚本。
- 清理 `index.html` 兼容隐藏块里的重复 ID，避免 `filePreview`、`memoryPreview` 和 token 统计字段出现多个 DOM 节点，导致 `getElementById(...)` 绑定行为依赖解析顺序。
- 新增 `Nanobot.Tests/WebUiScriptContractTests.cs`，用静态契约测试保证：
  - `app.js` 中每个 `elements.xxx` 都有对应定义；
  - 每个 `document.getElementById(...)` 目标都能在 `index.html` 中找到。
  - 每个 `document.getElementById(...)` 目标都定义在 `/app.js` 加载之前；
  - `index.html` 不允许重复 ID。
- 同步更新 `README.md`、`README.zh-CN.md` 和 `PROJECT_STATE.md` 的验证数据与运行说明。

## Verification

- `dotnet build Nanobot.slnx`: 0 warnings, 0 errors
- `dotnet test`: 130 passed, 0 failed, 0 skipped
- `dotnet run --project Nanobot.Web --urls http://127.0.0.1:8800`
  - `GET /api/runtime/status`: 200
  - `GET /api/system/status`: 200
  - `GET /api/settings/model`: 200
  - `GET /api/sessions`: 200
  - active provider: `siliconflow`
  - active model: `nex-agi/Nex-N2-Pro`
  - live `nong.commandCount`: 126
  - live external tool versions: `nong-chart` / `nong-diagram` / `nong-pdf` / `nong-pptx` / `nong-ocr` / `nong-imaging` all `4.1.0`
  - desktop browser smoke: runtime pill `就绪`, model `siliconflow::nex-agi/Nex-N2-Pro`, provider/model selects populated, send enabled, no console/runtime exceptions
  - narrow browser smoke: runtime pill `就绪`, model `siliconflow::nex-agi/Nex-N2-Pro`, provider/model selects populated, send enabled, no console/runtime exceptions
- Earlier streaming smoke in this stabilization window:
  - normal `POST /api/agent/stream`: `session -> delta* -> complete`, assistant message persisted
  - aborted `POST /api/agent/stream` with `curl --max-time 1`: no new empty WebUI session, interrupted run persisted assistant-side `[已停止]`
  - replay `GET /api/events` with `Last-Event-ID: 2`: returns `id: 3`, `id: 4`, proving restart-safe monotonic SSE replay ids
