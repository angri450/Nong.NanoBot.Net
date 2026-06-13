# NanoBot 应用层施工总方案

日期: 2026-06-13
状态: in-progress (Phase 1-6 done, 7-10 pending)

## 三项目架构（定稿）

```
Nong.Toolkit.Net (Skill 层)     → 告诉 agent "怎么用 nong"
Nong.Cli.Net      (CLI 执行层)  → 125 条命令，模块化 7 工具包
Nong.NanoBot.Net   (应用层)     → Agent runtime，把 CLI 能力暴露为 LLM 工具
```

应用层职责：让 LLM 自动发现 CLI 能力、按需加载 Skill、安全执行命令、展示安装/运行状态。

## 历史：4.0.0 桥接方案识别的 4 个缺口

| 缺口 | 说明 | 当前状态 |
|------|------|---------|
| 1. allowlist 缺模块 | lit/slice/progress 不在 NanoBot 白名单 | **仍未补** |
| 2. SkillLoader 全量塞入 | 17 个 SKILL.md 全拼进上下文，不精准 | **仍未改** |
| 3. Plugin 安装体系 | claude plugin install 流程未落地 | **未实现** |
| 4. WebUI 不是控制台 | 状态面板不显示 Nong/Toolkit/OCR 状态 | **未实现** |

## 4.1.0 模块化新增需求

| 新增缺口 | 说明 |
|---------|------|
| 5. 125 工具未注册为 AgentLoop function | 只能显式调 run_nong |
| 6. 模块化工具自动安装感知 | nong chart 首次用要装 nong-chart |
| 7. 上下文窗口管理 | 125 工具 schema + skill 全文会爆 token |
| 8. 确认机制 | install-model / ocr cloud 需用户确认 |

---

## 施工清单（10 步）

### 第 1 步 · P0 — allowlist 补全

**状态: done (审计发现已于 P6 完成)**

`NongTool.DefaultAllowedRoots` 已包含 lit/slice/progress，测试已覆盖。

**目标**: lit / slice / progress 三个命令模块加入 NanoBot 白名单。

**现状**: NanoBot 的 `NongTool.cs` allowlist 缺这三个。Nong 4.1.0 中它们稳定实现，纯 .NET 内嵌、无 native 依赖、不消耗外部 token。

**做法**:
- `NongTool.cs` 默认 allowlist 数组增加 `"lit"`, `"slice"`, `"progress"`
- `onboard config` 模板同步更新
- `ToolTests.cs` 加测试：验证默认 allowlist 包含全部内嵌模块

**风险**: 几乎为零。这三个模块在 4.0.0 中已稳定，4.1.0 无变化。

**验证**: `dotnet test` 通过。

---

### 第 2 步 · P0 — CLI 能力发现升级

**状态: done**

`NongTool.DiscoverOpenAiToolsAsync()` 调用 `nong commands --format openai-tools`，返回结构化的 `NongDiscoveredTool` 列表。

**做法**:
- `NongTool.cs` 新增静态方法 `DiscoverOpenAiToolsAsync()`
- `ParseOpenAiTools()` 解析 JSON 数组为 `NongDiscoveredTool` 列表
- 解析失败返回空列表，不抛异常

**目标**: 从旧的 `nong commands --json` 升级到 `nong commands --format openai-tools`。

**现状**: NanoBot 已支持 `nong commands --json` 发现命令。CLI 4.1.0 新增了 `--format openai-tools`，直接返回 OpenAI tools 数组（125 tools）。

**做法**:
- `NongTool.cs` 或 `CapabilityDiscovery.cs` 改为调用 `nong commands --format openai-tools`
- 返回的 schema 直接用作 AgentLoop 的 function-calling tools 参数
- 兼容回退：如果 CLI < 4.1.0，fallback 到 `nong commands --json` 自行渲染 schema

**风险**: 低。`--format openai-tools` 经本地验证输出正确。

**验证**: 启动 NanoBot，日志输出 "discovered 125 tools"。

---

### 第 3 步 · P1 — AgentLoop 工具自动注册

**状态: done**

`NongDiscoveredToolWrapper` 包装单个 Nong 命令为 ITool。CLI 和 Web Program.cs 启动时自动发现并注册全部 125 个命令。

**做法**:
- `NongDiscoveredToolWrapper` 实现 ITool，委托给 NongTool
- 自动合并用户 JSON 参数到 CLI args
- 启动时异步发现，发现完后输出 `[nong] Discovered N command tools`

**目标**: NanoBot 启动时自动将 125 个 Nong 命令注册为 AgentLoop function-calling 工具。

**现状**: 当前只能通过显式的 `run_nong` 桥执行 nong 命令。LLM 不知道有哪些命令可用。

**做法**:
1. AgentLoop 初始化阶段调用 `nong commands --format openai-tools`
2. 将返回的 tools 数组合并到 LLM 请求的 `tools` 参数
3. LLM 返回 function call 时，AgentLoop 执行对应的 nong 子进程
4. function result 返回给 LLM 继续推理

**关键设计**:
- 工具不区分 "内嵌模块" 和 "外部工具模块" — LLM 看到的是统一的 125 条 function
- 外部工具未安装时，由 nong CLI 路由层自动安装（已有代码），NanoBot 无需感知
- 每个工具的 `description` 字段来自 Manifest，足够 LLM 做意图匹配

**估时**: ~200 行 C#。

---

### 第 4 步 · P1 — Skill 路由匹配

**状态: done (P6 已实现)**

SkillLoader 两阶段 (catalog → load specific) + progressive disclosure 已完工。

**目标**: 不再把 16 个 SKILL.md 全拼进上下文。改为按需注入：用户说话时匹配 1-2 个 skill，只注入那部分。

**现状**: `SkillLoader.BuildSkillSection()` 把所有 SKILL.md 拼成一个字符串塞进上下文。references 下钻不存在。

**做法**:
1. **Phase 1（启动时）**: 扫描 workspace/skills 目录，只加载每个 skill 的 frontmatter（name + description + trigger keywords），构建 skill 索引（<1KB）。
2. **Phase 2（运行时）**: 用户消息到达时，匹配 skill 索引，找到最相关的 1-2 个 skill。
3. **Phase 3（注入）**: 只加载匹配 skill 的 SKILL.md 全文 + 对应 references/ 文件，注入到系统提示词。
4. 未匹配时不注入 skill 内容，保持上下文精瘦。

**关键约束**:
- 单次最多注入 2 个 skill 的完整内容
- 系统提示词 + skill 内容 + 当前消息 + 最近 3 轮历史 <10K tokens
- Reference 下钻：skill 引用的 references/ 文件完整加载

**风险**: 中。SkillLoader 重写影响所有依赖 skill 的对话。需充分测试两阶段加载和向后兼容。

**估时**: ~250 行 C#。

---

### 第 5 步 · P2 — 上下文窗口管理

**状态: done**

ContextRenderer: 工具 >25 个时不在系统提示词中重复 tool schema (function-calling API 已发送)。节省 ~8000 tokens。

**目标**: 防止 125 工具 schema + skill 全文 + 聊天历史撑爆 token 窗口。

**做法**:
1. 工具 schema 按模块分组（word/chart/pdf/ocr/...）。LLM 请求时可只发送匹配模块的工具列表。
2. 如果 tokens 超出阈值，自动裁剪最旧的聊天轮次，保留系统提示词 > skill prompt > 当前消息 > 最近 3 轮。
3. Token 计数使用 Tiktoken 近似或模型提供的 tokenizer。

**关键约束**:
- 默认阈值：模型最大 token 的 80%
- 裁剪日志记录，不在静默中丢上下文

**估时**: ~150 行 C#。

---

### 第 6 步 · P2 — 确认机制

**状态: done**

NongConfirmationHook 拦截 install/token/write 危险命令。install/camera/token 每次确认，write 每 session 每组确认一次。

**目标**: 危险/高成本命令需要用户显式确认才执行。

**命令分类**:

| 风险等级 | 命令示例 | 处理 |
|---------|---------|------|
| readonly | word read, pdf check, ocr models, excel sheets | 直接执行 |
| write | word create, pdf merge, pptx create, excel create | 首次确认，同 session 后续放行 |
| install | ocr install-model, 任何触发 dotnet tool install 的命令 | 每次确认 |
| token | ocr cloud, ocr to-word | 每次确认 |

**做法**:
1. 命令清单自带 `x-nong-risk` 标签（从 Manifest 生成时附加）
2. write/install/token 命令被执行前，AgentLoop 暂停 → 用户确认或拒绝
3. write 类别确认后，该 session 内同类命令不再重复确认
4. install/token 类别每次确认

**估时**: ~120 行 C#。

---

### 第 7 步 · P2 — Plugin 安装体系

**状态: done**

PluginManager + PluginInstallTool + PluginListTool。LLM 可调用 plugin_install 自动下载安装 Toolkit skills。

**目标**: NanoBot 能自动安装/更新 Nong.Toolkit.Net 的 skill，替代手动丢 workspace/skills。

**现状**: 完全未实现。用户需要手动下载 Toolkit repo 并把 skill 目录复制到 workspace。

**做法**:
1. NanoBot 新增 `plugin install nong-toolkit` 命令（或 WebUI 按钮）
2. 从 GitHub/Gitee release 下载 Nong.Toolkit.Net 的最新 zip
3. 解压到 `~/.nanobot/workspace/skills/`，保持目录结构
4. 更新 `~/.nanobot/skills.json` 注册表
5. `plugin list` 列出已安装的 skill 及其版本
6. `plugin update nong-toolkit` 检查远端新版本

**不做的**:
- 不做 NuGet 自动拉取（skill 不是 NuGet 包）
- 不做跨仓库的自动化发布管线

**风险**: 中。需要处理 GitHub API rate limit、离线回退、zip 校验。

**估时**: ~300 行 C# + WebUI 按钮。

---

### 第 8 步 · P3 — WebUI 状态面板（Nong 控制台）

**目标**: WebUI /status 端点 + 面板显示完整 Nong 环境状态。

**展示内容**:

| 区域 | 数据 |
|------|------|
| CLI 版本 | `nong --version` 输出 |
| 外部工具状态 | 6 个 Tool.* 是否安装 (`dotnet tool list --global`) |
| OCR 运行时 | `nong ocr models --json` 结果 |
| 命令能力矩阵 | 每个模块的命令数 + allowlist 状态 |
| Skill 目录 | 已安装 skill 列表 + 版本 + 最后更新 |

**做法**:
1. `/status` 端点新增 `nong` 字段，聚合以上信息
2. WebUI 新增 "Nong 状态" 面板，用彩色卡片展示
3. 未安装的工具显示安装提示，可一键安装

**估时**: ~150 行 C# + ~100 行 HTML/CSS。

---

### 第 9 步 · P3 — 模型配置面板增强

**目标**: WebUI 模型设置面板关联 Nong 工具注册状态。用户能看到 "当前 LLM 能调用多少条 Nong 命令"。

**做法**:
- 模型设置页新增 "Nong 工具" 行：已注册 125 条，上次发现时间
- 如果 CLI 未安装或版本过旧，显示警告和升级指引

**估时**: ~60 行 C# + 前端。

---

### 第 10 步 · P3 — 端到端验证

**目标**: 完整链路测试，从用户说话到 nong 命令执行。

**测试用例**:
1. "分析这份 Excel 数据，画柱状图" → `excel to-groups` → `chart bar` → 返回 PNG 路径
2. "这篇论文的参考文献有没有问题" → `inspect refs` → 返回诊断结果
3. "把这两个 PDF 合并" → `pdf merge` → 返回合并文件路径
4. "检查一下 OCR 环境" → `ocr check-env` → 返回 v5/v6 状态
5. 确认机制：`ocr install-model pp-ocrv6-medium` → AgentLoop 暂停要求确认

**验证标准**:
- 每个用例 3 轮对话内完成
- 无人工干预（除确认机制用例外）
- 输出结果结构正确（JSON 合同稳定）

---

## 施工顺序 & 依赖

```
第 1 步 allowlist 补全 ── 无依赖，马上能做 ──┐
第 2 步 能力发现升级 ── 无依赖 ──────────────┤
                                              ├─→ 第 3 步 AgentLoop 工具注册
                                              │
第 4 步 Skill 路由匹配 ── 依赖第 3 步 ────────┤
第 5 步 上下文管理 ── 依赖第 3+4 步 ──────────┤
第 6 步 确认机制 ── 依赖第 3 步 ──────────────┤
第 7 步 Plugin 安装 ── 依赖第 4 步 ───────────┤
                                              │
第 8 步 WebUI 状态面板 ── 依赖第 2+3+7 步 ────┤
第 9 步 模型配置增强 ── 依赖第 3+8 步 ────────┤
                                              │
第 10 步 端到端验证 ── 全部完成后 ────────────┘
```

**推荐执行顺序**: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10

## 不做的事

- 不让 NanoBot 自实现 OCR/Chart/PDF 渲染
- 不把 16 个 SKILL.md 全量塞入上下文
- 不在 NanoBot 里维护第二份命令表
- 不做跨仓库的自动化发布管线
- 不区分 "内嵌模块" vs "外部工具模块"（LLM 透明）

## 相关文档

- `Nong.Cli.Net/log/plans/2026-06-12-nanobot-bridge-plan.md` — 原始桥接设计
- `Nong.Cli.Net/log/changelog/2026-06-13-modular-release-final-audit.md` — CLI 4.1.0 审计
- `Nong.Toolkit.Net/log/guidance/2026-06-10-overall-roadmap.md` — 三项目总体路线
- `log/reports/nanobot-nong-4.0.0-full-bridge-plan.html` — 4.0.0 桥接方案（本仓库）
