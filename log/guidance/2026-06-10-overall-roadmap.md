# 2026-06-10 Toolkit + CLI 总体施工路线图

## 背景

这是 Nong.Toolkit.Net 2.4.0-dev 和 Nong.Cli.Net 4.0.0 的全量施工指导。基于 2026-06-10 的四份审计报告和全量 log 通读。

## 总当前状态

- Nong.Cli.Net 4.0.0：96 命令，15 模块，100% 实现，0 stub，14 NuGet 包已发布。阶段一（5 个 P1 alias）DONE。
- Nong.Toolkit.Net 2.4.0-dev：15 skill，全部与 CLI 4.0.0 对齐。阶段二（4 个 C 类 skill 重写）DONE。
- 架构共识：一刀三流（dissect → 统一 slice 包 → content/structure/format/assets/diagnostics 流）
- P0 阻断级问题：无

## 三个阶段 + 后续

```
现在（2026-06-10）
 │
 ├─ 阶段一：DONE（CLI P1 alias）
 │   guidance: Nong.Cli.Net/log/guidance/2026-06-10-phase1-p1-alias-full-plan.md
 │   改动：5 个命令补 alias，Manifest 更新，回归测试
 │   不改：命令逻辑、包依赖、SkillManagerCore
 │   估时：1-2h
 │   产出：5 个新 alias 出现在 nong commands --json
 │
 ├─ 阶段二：DONE（Toolkit C 类 skill 重写）
 │   guidance: Nong.Toolkit.Net/log/guidance/2026-06-10-phase2-c-class-skill-rewrite-plan.md
 │   改动：multimodal→ocr 改名，skill-manager/progress-report/icons 重写
 │   不改：CLI，A/B 类 skill
 │   估时：2-3h
 │   依赖：无（独立施工）
 │   产出：4 个 C 类 skill 全部有 references + examples
 │
 ├─ 阶段三：DONE（A/B 类 skill examples + references）
 │
 └─ 阶段四+：CLI 功能缺口（Nong.Cli.Net 侧施工，按模块拆独立 plan）
 │   guidance: Nong.Toolkit.Net/log/guidance/2026-06-10-phase3-examples-and-references-plan.md
 │   改动：11 个 A/B 类 skill 补 32 个 examples + 6 个 references
 │   不改：SKILL.md 主体结构，CLI，C 类 skill
 │   估时：5.5h（分 4 批）
 │   依赖：阶段二完成（C 类的 references/examples 结构是模板）
 │   产出：15 个 skill 全部有至少 1 个 example
 │
 └─ 阶段四+：CLI 功能缺口（Nong.Cli.Net 侧施工，按模块拆独立 plan）
     guidance: Nong.Cli.Net/log/guidance/2026-06-10-phase2-cli-feature-gaps-roadmap.md
     缺口：PPTX 写入、Excel 高级编辑、Chart 新图种、PDF 编辑、Inspect 扩展、Word 高级审阅
     每个缺口走独立 plan（审底层 → 补 CLI → 写测试 → 更新 Manifest → 同步 Toolkit）
     先做：Chart boxplot+histogram（ROI 最高，ScottPlot 已有底层支持）
```

## 三阶段的依赖关系

```
阶段一（CLI alias）──独立──→ 不影响阶段二和三
阶段二（C 类 skill）──独立──→ 不阻塞阶段一
阶段三（A/B 类 examples）──依赖──→ 阶段二的 references/examples 结构作为模板
```

阶段一和阶段二没有互锁依赖，可以并行。但实际上建议先做阶段一（改动最小，马上见效），再做阶段二（改动面中等），最后做阶段三（改动面最大但风险最低）。

## 不做的（显式排除）

1. 不重命名 CLI 命令（alias 是新增入口，老命令保留兼容）
2. 不改变 CLI 和 Toolkit 之间的版本依赖关系
3. 不引入新 NuGet 包或新 .csproj
4. 不回到旧 GroundPA / GroundPA-Toolkit 命名
5. 不把功能缺口（PPTX 写入等）混进当前阶段

## CLI 和 Toolkit 的共享文档

- 共享 CLI 预检：`Nong.Toolkit.Net/references/shared/nong-cli-preflight.md`
- 包依赖全景：`Nong.Cli.Net/log/guidance/2026-06-10-package-dependency-map.md`
- 命名审计结论：`Nong.Cli.Net/log/reports/toolkit-cli-command-naming-audit.html`
- 最终蓝图：`Nong.Cli.Net/log/reports/nong-final-blueprint.html`
- Skill/CLI/Package 对照：`Nong.Toolkit.Net/log/reports/toolkit-skill-cli-package-map.html`
- Skill 系统审计：`Nong.Toolkit.Net/log/reports/toolkit-skill-system-audit.html`

## 每个阶段完工后的验证

### 阶段一验收
```powershell
dotnet test -c Release
.\Cli\bin\Release\net8.0\nong.exe commands --json | findstr "diagnose clean-styles references variables data-requirements"
```

### 阶段二验收
```powershell
..\Nong.Cli.Net\Cli\bin\Release\net8.0\nong.exe skill validate .\ocr --json
..\Nong.Cli.Net\Cli\bin\Release\net8.0\nong.exe skill validate .\skill-manager --json
..\Nong.Cli.Net\Cli\bin\Release\net8.0\nong.exe skill validate .\progress-report --json
..\Nong.Cli.Net\Cli\bin\Release\net8.0\nong.exe skill validate .\icons --json
..\Nong.Cli.Net\Cli\bin\Release\net8.0\nong.exe skill inventory . --json
..\Nong.Cli.Net\Cli\bin\Release\net8.0\nong.exe skill scan . --json
..\Nong.Cli.Net\Cli\bin\Release\net8.0\nong.exe skill package . --json
```

### 阶段三验收
同上，15 个 skill 全部 validate 通过，scan 0 findings。

## 和 NanoBot 的关系

NanoBot 的施工规划在 `Nong.NanoBot.Net/log/reports/nanobot-nong-4.0.0-full-bridge-plan.html`。CLI/Toolkit 的基础打牢后，NanoBot 才能：
- 用完整的 CLI allowlist（14 个命令模块，含 lit/slice/progress）
- 用 capability discovery（nong commands --json）
- 用精准触发的 skill 加载（不再全量塞入上下文）
- 在 WebUI 状态面板看到 CLI/Toolkit/OCR 安装状态

## 状态

reference — 随施工进度更新。当前全部三个阶段的 guidance 为 plan 状态，待开工。
