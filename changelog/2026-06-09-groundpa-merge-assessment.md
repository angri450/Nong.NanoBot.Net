# Nong.NanoBot.Net 合并方案评估

## 时间
2026-06-09

## 用户设想

把 Nong.Toolkit.Net 和 Nong.Cli.Net 全部合并进 Nong.NanoBot.Net，用一个仓库解决三个仓库的版本不对齐、职责模糊、开发流程脱节的问题。

## Nong.NanoBot.Net 现状

**定位**: 个人 Agent 运行时。.NET 8 / C# 12，本地优先，CLI + WebUI。

**核心能力**:
- Agent loop（Nanobot.Core）
- 多 provider 路由（DMX / Anthropic / Azure OpenAI）
- 多 channel gateway（Telegram / Slack / Discord / Feishu）
- MCP 传输（stdio / SSE / streamable HTTP）
- Memory（history.jsonl / MEMORY.md / Dream 整合）
- Skills 加载器（Nanobot.Core/Skills/SkillLoader.cs）
- Cron 调度、Heartbeat、CodingPlan
- WebUI 工作台（Nanobot.Web）

**与 Nong.Toolkit.Net / Nong.Cli.Net 的关系**（来自 DEVELOPMENT_PLAN.zh-CN.md）:

```
NanoBot 出场不直接打包 Nong.Toolkit.Net / Nong 的完整负载
但 NanoBot 必须内置一套"技能包 / plugin"安装部署方案
Nong.Toolkit.Net 和 Nong 都通过这套机制接入
```

即：NanoBot 是宿主，Nong.Toolkit.Net 是插件包，Nong 是执行引擎。三个是明确的宿主/插件/引擎关系。

## 合并分析

### Nong.Toolkit.Net 合并到 Nong.NanoBot.Net — 合理

**理由**:
1. Nong.Toolkit.Net 是纯文档（11 个 SKILL.md + references + formats + scripts），0 行 C# 代码，合并成本极低
2. Nong.NanoBot.Net 已有 SkillLoader.cs，但技能目录是空的——Nong.Toolkit.Net 正是它缺少的"第一方技能包"
3. `Nanobot.Core/Skills/` 放 SKILL.md 目录，技能版本和 Agent 运行时版本天然同步（同一个 git commit）
4. skill-manager 可以直接引用运行时代码，不再有"孤立"问题
5. 解决了 Nong.Toolkit.Net 缺少 CLAUDE.md / AGENTS.md 的问题——Nong.NanoBot.Net 已经有完善的开发指导

**合并后结构**:
```
Nong.NanoBot.Net/
├── Nanobot.Core/
│   └── Skills/              ← 现有的 SkillLoader
├── skills/                  ← 原 Nong.Toolkit.Net 的 11 个 skill 目录
│   ├── word/
│   ├── chart/
│   ├── ...
│   └── progress-report/     ← 直接放在这里
├── .claude-plugin/
│   └── plugin.json          ← moved from Nong.Toolkit.Net
├── skills.sh.json           ← moved from Nong.Toolkit.Net
├── CLAUDE.md                ← 补充 Nong.Toolkit.Net 开发约束
└── AGENTS.md                ← 已有 NanoBot + Nong.Toolkit.Net 统一路线
```

### Angri450.Nong 合并到 Nong.NanoBot.Net — 不好

**理由**:
1. Nong 是独立分发的 NuGet CLI 工具（`dotnet tool install --global Angri450.Nong.Cli`），合并源码不影响这个分发模型，但会让 Nong.NanoBot.Net 体积爆炸（3200+ 文件，15 个 vendored 第三方库全部源码入仓库）
2. Nong 的用户不一定需要 NanoBot，NanoBot 的用户不一定需要编译 Nong 源码
3. NanoBot 通过 `nong` CLI 进程调用 Nong（Nong bridge），不是通过源码引用
4. Nong 有自己的发布周期、NuGet 包规范、ThirdParty 管理——这些和 NanoBot 的 Agent 运行时逻辑无关

**正确的关系**（AGENTS.md 已写）:
```
Nong.NanoBot.Net
  ├── 内置 Skills/SkillLoader（加载 SKILL.md）
  ├── 内置 Nong bridge（调用 nong 命令）
  │
  └──→ 外部依赖:
        ├── Angri450.Nong.Cli（dotnet tool install）
        └── Nong.Toolkit.Net 技能包（现在合并进 Nong.NanoBot.Net 自身）
```

### 但有一个关键点要改

AGENTS.md 现在写的是 "Nong.Toolkit.Net 和 Nong 都通过 plugin bootstrap 机制接入"——这是说 Nong.Toolkit.Net 作为**外部插件**安装。如果 Nong.Toolkit.Net 合并进来，它就变成了**内置第一方技能包**，不再需要"安装"这一步骤。这更符合实际：用户拿到 NanoBot 就应该有 Word / Chart / PDF 等基本技能可用。

## 结论

**合并 Nong.Toolkit.Net 进 Nong.NanoBot.Net — 应该做**。理由充分，成本极低，一举解决版本对齐、开发流程、skill 管理三个核心问题。

**合并 Nong.Cli.Net 进 Nong.NanoBot.Net — 不应该做**。Nong 是独立 CLI 工具，保持独立仓库+独立分发是最优模型。NanoBot 通过进程调用来消费它，不通过源码合并。

## 合并后三方关系

```
Nong.NanoBot.Net（主仓库）
├── Nanobot.Core/Agent/     ← Agent 运行时
├── Nanobot.Web/            ← WebUI
├── skills/                 ← 11+ 个 SKILL.md 技能（原 Nong.Toolkit.Net）
├── .claude-plugin/         ← Claude Code 插件元数据
│   插件安装 → 用户可以用 claude plugin install 安装这个仓库
└── Nong bridge             ← 通过 nong CLI 进程调用外部 Nong

Nong.Cli.Net（独立仓库，不变）
├── Cli/NongCli.csproj      ← nong CLI 工具
├── Docx/ Chart/ Excel/ ... ← 各功能库
├── ThirdParty/             ← 第三方源码
└── 分发方式: dotnet tool install --global Angri450.Nong.Cli
```

这样三个仓库变成两个，而且是自然分层：
- **Nong.NanoBot.Net** = 运行时的家 + 技能的家（一个仓库）
- **Angri450.Nong** = 执行引擎的家（独立仓库，独立分发）

版本对齐不再是问题：skill 引用的 nong 版本号写在一个地方（skills.sh.json 或共享 reference），改一次即可。
