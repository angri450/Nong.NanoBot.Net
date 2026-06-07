# NanoBot.net 开发施工方案

本文是 NanoBot.net 的主施工方案。DeepSeek-GUI.net、CodeBuddy、DeepSeek-TUI、DeepSeek-GUI 等项目只作为 UI/交互/信息架构参考；真正要开发和沉淀能力的主仓库是 `NanoBot.net`。

## 方向判断

NanoBot.net 继续作为 GroundPA / Nong agent-runtime 层主线，目标是做成一个 .NET 8、本地优先、可视化可用、工具调用可审计的 GroundPA host。

这里的关键判断是：NanoBot 出场不直接打包 GroundPA-Toolkit / Nong 的完整负载，但 NanoBot 必须内置一套“技能包 / plugin”安装部署方案。用户拿到 NanoBot 后，可以像 Claude Code plugin marketplace 那样，从带 `plugin.json` 的仓库一键安装、部署、升级能力包。GroundPA-Toolkit 和 Nong 都通过这套机制接入，而不是和 NanoBot 发行包绑死。

当前不把重点放在重写 DeepSeek-GUI，不做第二个运行时项目。GUI 需要服务 NanoBot：先把 NanoBot 的底层能力通过稳定 API 暴露出来，再让 WebUI 或未来桌面壳调用这些能力。

## 当前基线

- Runtime：`Nanobot.Core`
- CLI：`Nanobot.CLI`
- WebUI：`Nanobot.Web`
- 默认框架：.NET 8
- License：Apache-2.0
- 默认模型：DMX 中转 DeepSeek V4 Pro，模型 ID `deepseek-v4-pro-guan`
- 默认 provider：`dmx`
- 本地配置：`~/.nanobot/config.json`
- 本地工作区：`~/.nanobot/workspace`
- 本地记忆：workspace 下的 `MEMORY.md`、`history.jsonl` 等文件
- 技能包 / plugin：NanoBot 自带安装、部署、升级、检测框架，支持从带 `plugin.json` 的仓库安装能力包
- GroundPA-Toolkit / Nong：不随 NanoBot 二进制打包完整负载，由技能包机制在初始化或运行时安装、更新和健康检查

## 总体架构

```text
NanoBot UI
  WebUI first, optional desktop shell later
        |
        | HTTP / SSE / WebSocket
        v
Nanobot.Web / Runtime Host
  sessions, streaming, file tree, tool events, approvals
  plugin / GroundPA bootstrap status and background deployment
        |
        v
Nanobot.Core
  Agent loop
  Provider routing
  Tool registry
  Runtime events
  Memory
  MCP
  Nong bridge
  Plugin installer/updater
  GroundPA installer/updater
        |
        v
Local workspace + installed skill packs + installed GroundPA toolkit + external model/tool APIs
```

开发顺序必须从 NanoBot 自己的 runtime API 出发，而不是先改外部 GUI：

1. `Nanobot.Core` 先有稳定能力和事件。
2. `Nanobot.Web` 把能力暴露成可视化工作台。
3. 后续桌面壳只调用 NanoBot API，不复制一套 agent loop。

## 要调用出来的底层能力

### Agent Loop

- 普通对话
- 流式输出
- 多轮工具调用
- 工具调用失败后的模型恢复
- fallback provider chain
- 每轮 run/session/thread 标识

### Provider / Model

- 默认 `dmx::deepseek-v4-pro-guan`
- OpenAI-compatible provider 继续保留，作为兼容层
- Anthropic、Azure OpenAI 继续保留为 fallback 能力
- API key 只能来自环境变量或用户本地配置，不能进入仓库
- 后续补 usage/token/model 统计，给 UI 展示成本和调用状态

### Tools

- 文件读写编辑
- workspace 内 shell
- Web search / fetch
- GitHub
- Weather / stock
- Memory
- Summarize
- MCP stdio / HTTP / SSE
- Nong bridge

Nong bridge 是重点能力，必须保持 argument-array 调用、workspace 边界、root command allowlist、timeout、输出截断和结构化错误。

### Skill Pack / Plugin 机制

NanoBot 要内置一套通用技能包方案，而不是只给 GroundPA 写死安装逻辑。目标是复用并融合 Claude Code plugin marketplace 的体验：只要一个仓库带 `plugin.json`，NanoBot 就能识别、安装、部署和更新。

参考体验：

```powershell
claude plugin marketplace add https://gitcode.com/angri450/GroundPA-Toolkit.git
claude plugin install groundpa-toolkit@angri450
```

NanoBot 对应的命令建议是：

```powershell
nanobot plugin marketplace add https://gitcode.com/angri450/GroundPA-Toolkit.git
nanobot plugin install groundpa-toolkit@angri450
nanobot plugin status
nanobot plugin update
```

设计原则：

- plugin 仓库根目录必须有 `plugin.json`。
- `plugin.json` 描述插件 ID、版本、入口、安装脚本、工具能力、依赖、健康检查和更新策略。
- NanoBot 可以先支持自己的 plugin manifest，再做 Claude Code plugin manifest 的兼容映射。
- plugin 安装目录与 NanoBot 主程序分离，便于独立升级、回滚和卸载。
- plugin 安装可以后台继续执行，不阻塞 WebUI 启动和基础聊天。
- plugin 能力统一进入 capability catalog，供 WebUI、agent prompt、工具注册和审批策略使用。

建议本地状态：

```text
~/.nanobot/
  plugins/
    marketplaces.json
    installed.json
    cache/
    deployments/
    logs/
  tools/
    nong/
```

`plugin.json` 第一版字段建议：

```json
{
  "id": "groundpa-toolkit",
  "publisher": "angri450",
  "version": "0.1.0",
  "displayName": "GroundPA Toolkit",
  "description": "GroundPA deterministic tool skills for NanoBot.",
  "install": {
    "commands": []
  },
  "tools": [
    {
      "id": "nong",
      "kind": "cli",
      "command": "nong",
      "healthCheck": ["commands", "--json"]
    }
  ],
  "capabilities": [],
  "permissions": {
    "workspace": "required",
    "network": "install-only"
  }
}
```

第一阶段不需要把 manifest 做复杂，先保证四件事：

1. 能从 marketplace 源发现带 `plugin.json` 的仓库。
2. 能安装到 `~/.nanobot/plugins/deployments/`。
3. 能执行健康检查并写入状态。
4. 能把插件声明的工具能力注册给 NanoBot runtime。

### GroundPA Bootstrap

NanoBot 的 GroundPA 策略是“出场不自带，安装能力自带”：

- NanoBot 发行包不直接塞入完整 GroundPA-Toolkit / Nong 包。
- `onboard` 负责创建本地目录、配置 plugin marketplace 源、写入状态文件，并启动可恢复的安装流程。
- WebUI / runtime 启动后可以边运行边继续部署 Nong 包，不因为工具包未完成安装而阻塞整个 UI。
- 安装、升级、健康检查、失败重试都应有结构化状态，并在 WebUI 顶部或右侧状态面板展示。
- 模型看到的是稳定的 `run_nong` / GroundPA capability，不需要知道安装过程细节。

GroundPA-Toolkit 是 NanoBot plugin 机制的第一块核心样板。命令可以保留 GroundPA 语义糖，但底层走同一套 plugin 安装系统：

```powershell
nanobot groundpa marketplace add https://gitcode.com/angri450/GroundPA-Toolkit.git
nanobot groundpa install groundpa-toolkit@angri450
nanobot groundpa status
nanobot groundpa update
```

内部状态建议落在：

```text
~/.nanobot/
  groundpa/
    sources.json
    installed.json
    deployments/
    logs/
  tools/
    nong/
```

其中 `groundpa/` 可以视为 plugin 状态的聚合视图，真实安装记录仍应回写 `plugins/installed.json`，避免 GroundPA 形成一条孤立的安装体系。

第一阶段可以先实现最小闭环：

1. 检测本机是否已有 `nong` 命令。
2. 若没有，记录 `missing` 状态并给出安装入口。
3. 支持从配置的 GroundPA-Toolkit 源安装或更新。
4. 安装完成后执行 `nong commands --json` 做能力发现。
5. 将发现结果转成 WebUI 可展示的 capability catalog。
6. `run_nong` 继续只接受 argument array，不接受 shell command string。

### Memory

- `FileMemoryStore`
- `MEMORY.md`
- per-session history
- Dream consolidation
- WebUI 展示记忆状态和最近更新
- 后续增加记忆编辑/冻结/回滚入口

### Runtime Events

UI 不应该靠解析最终回答来猜测状态。NanoBot runtime 要输出结构化事件：

- `run.started`
- `content.delta`
- `content.completed`
- `tool.started`
- `tool.completed`
- `tool.failed`
- `memory.updated`
- `approval.requested`
- `user_input.requested`
- `run.failed`

这些事件是 WebUI、未来桌面壳、日志、调试面板的共同协议。

## WebUI 施工路线

WebUI 是当前第一优先级视觉入口。要求：

- 中文优先，英文可切换
- 深色/浅色主题都完整
- 不做营销 landing page，第一屏就是可用工作台
- 布局参考 CodeBuddy / DeepSeek GUI 的密度和质感，但不复制源码、素材、品牌或专有命名
- 工作台核心区包括会话、聊天、文件树、工具详情、runtime 状态、记忆和配置反馈

### P1：工作台可用

已完成/接近完成：

- WebUI shell
- 会话创建与切换
- 基础聊天
- runtime 状态
- 中文界面

### P2：真实 runtime 能力

已完成/接近完成：

- 流式输出
- 会话持久化
- workspace 文件树
- 工具调用详情

### P3：视觉与交互强化

当前正在施工：

- 参考 DeepSeek-GUI 的浅色质感和工作台布局
- 深色/浅色主题统一
- 聊天画布、侧边栏、右侧详情面板优化
- 工具详情可读性增强
- 移动/窄屏基本可用

### P4：Plugin / GroundPA Bootstrap 与 Agent 操作面板

下一阶段：

- NanoBot plugin manifest 设计，核心字段为 `plugin.json`
- plugin marketplace add/install/status/update
- 支持安装带 `plugin.json` 的仓库
- GroundPA-Toolkit 作为第一块核心 plugin 样板
- GroundPA-Toolkit marketplace/source 配置
- Nong 安装、更新、部署、健康检查
- runtime 启动后后台继续部署 Nong 包
- WebUI 展示 GroundPA/Nong ready、installing、failed、update available 状态
- `nong commands --json` capability discovery
- plan / todo 面板
- tool approval / deny
- user input gate
- run interrupt
- 会话搜索、归档、fork、resume
- memory 查看与编辑

### P5：桌面壳

等 WebUI API 稳定后再做：

- Electron 或 WinUI 作为 NanoBot 桌面壳
- 复用 NanoBot HTTP/SSE API
- 复用 NanoBot 配置和 workspace
- 不在桌面壳内再实现一套 agent loop

DeepSeek-GUI.net 可作为桌面壳参考，但不是 NanoBot 主施工仓库。

## API 边界

建议 NanoBot runtime API 逐步稳定为：

```text
GET  /api/status
GET  /api/sessions
POST /api/sessions
GET  /api/sessions/{id}
POST /api/chat
POST /api/chat/stream
GET  /api/events?sessionId=&since=
GET  /api/workspace/tree?path=
GET  /api/workspace/file?path=
GET  /api/tools
GET  /api/plugins
POST /api/plugins/install
POST /api/plugins/update
GET  /api/plugins/{id}/status
GET  /api/groundpa/status
POST /api/groundpa/install
POST /api/groundpa/update
GET  /api/groundpa/capabilities
GET  /api/memory
POST /api/approvals/{id}
POST /api/user-inputs/{id}
POST /api/runs/{id}/interrupt
```

现有 API 可以先保持兼容；新增能力优先围绕这条边界扩展，避免 UI 直接碰 `AgentLoop` 内部。

## 模型配置方案

默认配置：

```json
{
  "providers": {
    "dmx": {
      "kind": "openai-compatible",
      "apiKey": "",
      "apiBase": "https://www.dmxapi.cn/v1/",
      "defaultModel": "deepseek-v4-pro-guan",
      "models": [
        {
          "id": "deepseek-v4-pro-guan",
          "apiModelId": "deepseek-v4-pro-guan",
          "supportsStreaming": true,
          "supportsTools": true
        }
      ]
    }
  },
  "agents": {
    "defaults": {
      "model": "dmx::deepseek-v4-pro-guan",
      "fallbackModels": ["dmx::deepseek-v4-pro-guan"]
    }
  }
}
```

本地测试使用：

```powershell
$env:DMX_API_KEY = "<local-secret>"
$env:DMX_API_BASE = "https://www.dmxapi.cn/v1/"
$env:DMX_MODEL = "deepseek-v4-pro-guan"
```

注意：OpenAI-compatible SDK 会自己拼接 `chat/completions`，所以 `apiBase` 必须写 `https://www.dmxapi.cn/v1/`，不要写完整的 `/chat/completions` endpoint。

## 近期执行清单

1. 固定 plugin 策略：出场不自带完整能力包，但内置 marketplace、安装、升级、检测和后台部署。
2. 设计 NanoBot `plugin.json` manifest，并预留 Claude Code plugin manifest 兼容映射。
3. 为 `nanobot plugin marketplace add/install/status/update` 设计 CLI contract。
4. 将 `nanobot groundpa ...` 做成 GroundPA plugin 的语义糖。
5. 增加 plugin / GroundPA / Nong 状态 API：ready、missing、installing、failed、update available。
6. 在 WebUI 展示 plugin、GroundPA-Toolkit、Nong 的安装进度和健康状态。
7. 通过 `nong commands --json` 生成 capability catalog。
8. 把 runtime events 整理成稳定 DTO，减少 WebUI 对内部字段的猜测。
9. 增加 approval / user input gate 的 core contract 和 WebUI 展示。
10. 增强 memory 面板：展示、编辑、刷新、Dream 状态。
11. 增强 Nong tool detail：展示 command、args、cwd、exit code、stdout/stderr、截断状态。
12. 增加 usage/model 状态：当前 provider、模型、streaming、token usage、错误原因。
13. 再评估 Electron/WinUI 桌面壳，前提是 NanoBot Web API 足够稳定。

## 验证标准

每个有意义的阶段至少执行：

```powershell
dotnet test
dotnet build
```

涉及 WebUI 时额外检查：

- 桌面宽屏
- 窄屏/移动宽度
- 深色主题
- 浅色主题
- 无 API key 时的错误提示
- DMX key 配好后的流式输出
- 工具调用详情
- workspace 文件树
- 带 `plugin.json` 的仓库安装
- plugin 安装中、失败、更新状态
- GroundPA/Nong 缺失时的安装入口
- GroundPA/Nong 安装中状态
- `nong commands --json` capability discovery

真实模型集成只用本机环境变量，不写入仓库：

```powershell
$env:NANOBOT_RUN_INTEGRATION_TESTS = "1"
$env:DMX_API_KEY = "<local-secret>"
dotnet test --filter RealIntegrationTests
```

## 仓库纪律

- 主施工仓库是 `NanoBot.net`。
- 外部 GUI 仓库只做参考和对比，不承载 NanoBot 的主方案。
- GroundPA-Toolkit / Nong 不作为完整负载打包进 NanoBot 发行物；NanoBot 内置的是 plugin marketplace、安装、部署、升级、检测和运行桥接能力。
- GroundPA-Toolkit、Nong 和其他技能包应独立更新，NanoBot 只负责安装编排、状态管理、capability catalog 和 runtime bridge。
- 不复制外部 UI 源码或资产。
- 不提交真实 key。
- 不把已有 WebUI、CLI、Core 能力拆成平行 runtime。
- 新能力先沉淀到 `Nanobot.Core` / `Nanobot.Web`，再考虑桌面壳。
