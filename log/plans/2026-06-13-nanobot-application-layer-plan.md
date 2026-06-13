# NanoBot 应用层施工总方案

日期: 2026-06-13
状态: plan
来源: 汇总自 Nong.Cli.Net + Nong.Toolkit.Net 的 NanoBot 相关 plan

## 三项目架构（定稿）

```
Nong.Toolkit.Net (Skill 层)     → 告诉 agent "怎么用 nong"
Nong.Cli.Net      (CLI 执行层)  → 125 条命令，模块化 7 工具包
Nong.NanoBot.Net   (应用层)     → Agent runtime，把 CLI 能力暴露为 LLM 工具
```

应用层的职责：让 LLM 自动发现 CLI 能力、按需加载 Skill、安全执行命令。

## 当前基线

### CLI 侧 — 已就绪 (4.1.0)

| 交付物 | 状态 |
|--------|------|
| 模块化拆分 (7 包, 轻路由 + 6 外部工具) | done |
| 包身份分离 (Angri450.Nong.Tool.* vs 核心库) | done |
| 体积闸门 (全部 7 包 ≤50MB) | done |
| 125 条命令, 154 测试通过 | done |
| `nong commands --format openai-tools` | done (125 tools, 113 带 params) |
| 6 独立工具 NuGet 发布 | done |
| 本地 7 工具部署 | done |

### Toolkit 侧 — 已同步 (4.1.0)

| 交付物 | 状态 |
|--------|------|
| 16 个 skill, 版本全迁 4.1.0 | done |
| 6 个外部技能 SKILL.md 加模块化路由说明 | done |
| nong-cli-preflight.md 更新 (自动安装指引) | done |
| marketplace.json / plugin.json 版本同步 | done |

### NanoBot 侧 — 当前状态

| 组件 | 来源 | 状态 |
|------|------|------|
| SkillLoader 二段式 (catalog → skill → reference) | P6 | done |
| run_nong bridge (workspace, allowlist, timeout) | P6 | done |
| nong commands --json 发现 | P6 | done |
| system status 端点 (/status) | P6 | done |
| MCP stdio/SSE/HTTP | P6 | done |
| 多通道 gateway (Telegram/Slack/Discord/Feishu) | P6 | done |
| Memory write path + Dream | P6 | done |

### 缺口

| 缺口 | 影响 | 所在仓库 |
|------|------|---------|
| 125 条 CLI 命令未注册为 AgentLoop 工具 | 只能显式调 run_nong | **NanoBot** |
| Skill trigger 匹配未接入 AgentLoop | catalog 加载了但没用上 | **NanoBot** |
| 上下文窗口管理 | 16 个 skill 全量注入会爆 token | **NanoBot** |
| 确认机制 (token 消耗型命令) | install-model / ocr cloud 需用户确认 | **NanoBot** |
| 模块化工具自动安装感知 | nong chart 首次用要装 nong-chart | **NanoBot** |

## 施工计划

### Phase 1 — CLI 侧: `nong commands --format openai-tools`

**状态: done (Nong.Cli.Net `e3fad98`)**

产出：125 条命令的 OpenAI function-calling schema。NanoBot 直接 `nong commands --format openai-tools` 拿到已格式好的 tools 数组，不需要自己做 schema 渲染。

### Phase 2 — NanoBot: AgentLoop 工具自动注册 + Skill 路由匹配

**状态: 待施工 (本仓库)**

**目标**: AgentLoop 启动时自动注册全部 125 个 Nong 工具，根据用户输入自动匹配并注入对应 Skill prompt。

**做法**:

1. AgentLoop 初始化阶段调用 `nong commands --format openai-tools`，将返回的 JSON 数组直接作为 LLM 请求的 `tools` 参数。
2. 同时加载 `Nong.Toolkit.Net` 的 `marketplace.json`，解析出 16 个 skill 的 trigger keywords / descriptions。
3. 用户消息到达时，匹配 trigger keywords，注入 1-2 个对应 skill 的 SKILL.md + references 到系统提示词。
4. 未匹配时不注入 skill 内容，保持上下文精瘦。

**关键约束**:
- 不在 AgentLoop 启动时加载 16 个 SKILL.md 全文，只加载 trigger 元数据（~1KB）。
- Skill 内容按需注入，单次最多注入 2 个 skill（<10K tokens）。
- 工具注册不区分 "内嵌模块" 和 "外部工具模块" — LLM 看到的是统一的 125 条 function 列表，由 nong 路由层处理实际调度。

**估时**: ~200 行 C#，3-4h。

### Phase 3 — NanoBot: 上下文窗口管理

**状态: 待施工 (本仓库)**

**目标**: 防止 125 工具 schema + 多个 skill 全文 + 聊天历史撑爆 token 窗口。

**做法**:

1. 工具 schema 按模块分组 (word / chart / pdf / ocr / ...)，LLM 请求时只发送匹配模块的工具列表。
2. Skill prompt 注入后，如果 tokens 超出阈值，自动裁剪最旧的聊天轮次。
3. 模块化工具安装状态感知：如果 `nong-chart` 未安装，chart 组的 11 个工具仍然注册，但执行时会走自动安装流程。

**关键约束**:
- Token 计数使用模型提供的 tokenizer（或 Tiktoken 近似）。
- 裁剪策略：优先保留系统提示词 > 当前用户消息 > skill prompt > 最近 3 轮对话。

**估时**: ~150 行 C#，2-3h。

### Phase 4 — NanoBot: 确认机制

**状态: 待施工 (本仓库)**

**目标**: 危险/高成本命令执行前需要用户显式确认。

**高危命令分类**:

| 类别 | 命令示例 | 原因 |
|------|---------|------|
| 模型下载 | `ocr install-model` | 下载大文件 |
| API 消耗 | `ocr cloud`, `ocr to-word` | 消耗 PaddleOCR token |
| 文件写入 | `word create`, `pdf merge`, `pptx create` | 可能覆盖文件 |
| 外部安装 | 任何自动触发 `dotnet tool install` 的命令 | 全局状态变更 |

**做法**:

1. 每个工具 schema 附带 `x-nong-risk: "readonly" | "write" | "install" | "token"` 标记。
2. `write` / `install` / `token` 类命令执行前，AgentLoop 暂停，向用户发出确认提示。
3. 用户在同一个 session 内确认后，该 session 内同类命令不再重复确认（如 "允许 word 写入"）。
4. `readonly` 类命令直接执行，无需确认。

**估时**: ~120 行 C#，2-3h。

### Phase 5 — NanoBot: 安装状态仪表盘

**状态: 待施工 (本仓库)**

**目标**: WebUI /status 端点展示 Nong 安装状态，包括主 CLI + 6 外部工具 + OCR runtime。

**做法**:

1. `/status` 端点新增 `nong_modules` 字段：
   ```json
   {
     "cli": { "version": "4.1.0", "path": "..." },
     "external_tools": {
       "chart": "installed",
       "diagram": "installed",
       "pdf": "installed",
       "pptx": "installed",
       "ocr": "installed",
       "imaging": "installed"
     },
     "ocr_runtime": { "v6": "ok" }
   }
   ```
2. 通过 `dotnet tool list --global` + `nong ocr models --json` 收集状态。

**估时**: ~80 行 C#，1-2h。

## 施工顺序

```
Phase 1 (CLI 侧) [DONE]
  │
Phase 2 (AgentLoop 工具注册 + Skill 路由) [NEXT, 3-4h]
  │
Phase 3 (上下文管理) [2-3h]
  │
Phase 4 (确认机制) [2-3h]
  │
Phase 5 (仪表盘) [1-2h]
```

Phase 2 是核心：做完它，NanoBot 就能真正 "理解" 125 条 Nong 命令并按需调度。Phase 3-5 是稳固和用户体验层。

## 不做的事

- NanoBot 不自己实现 OCR / Chart / PDF 渲染 — 全部走 nong CLI 子进程。
- 不把 16 个 SKILL.md 全量塞进 LLM 上下文 — 按需注入。
- 不在 NanoBot 里维护第二份命令表 — 来源唯一: `nong commands --format openai-tools`。
- 不区分 "内嵌模块" 和 "外部工具模块" — 对 LLM 透明，由 CLI 路由层处理。

## 验证

- Phase 2: NanoBot 启动后调用 LLM，用户说 "分析这个 Excel 的数据并画柱状图"，AgentLoop 能自动调用 `excel to-groups` → `chart bar`。
- Phase 3: 125 工具全注册 + 2 个 skill 注入时，总 prompt <20K tokens。
- Phase 4: 用户说 "装 OCR 模型"，AgentLoop 暂停并提示确认。
- Phase 5: WebUI 仪表盘显示 7/7 工具 + OCR runtime 就绪。

## 相关文档

- `Nong.Cli.Net/log/plans/2026-06-12-nanobot-bridge-plan.md` — 原始桥接设计
- `Nong.Cli.Net/log/changelog/2026-06-13-modular-release-final-audit.md` — CLI 4.1.0 最终审计
- `Nong.Toolkit.Net/log/guidance/2026-06-10-overall-roadmap.md` — 三项目总体路线
- `Nong.Cli.Net/log/reports/nong-status-4.1.0.html` — CLI 项目状态面板
