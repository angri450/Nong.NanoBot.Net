# AtomCode / GitCode CodingPlan 吸收方案

调研日期：2026-06-08

参考仓库：

- `C:\Users\Administrator\Documents\Github\atomcode.net`
- `C:\Users\Administrator\Documents\Github\DeepSeek-TUI`

本方案的目标不是把 AtomCode 变成 Nong.NanoBot.Net 的依赖，而是把它的 GitCode 登录、CodingPlan 免费模型领取、动态模型注册和 WebUI 控制面这条链路吃透，再用 .NET 8 在 Nong.NanoBot.Net 里实现一套可维护、可测试、可替换的能力。

## 结论

AtomCode 最值得吸收的是它的“账号登录 -> 免费额度领取 -> 模型目录同步 -> provider 自动注册 -> WebUI/TUI 可切换使用”闭环。

NanoBot 可以吸收的部分：

- GitCode / AtomGit OAuth 登录流程。
- CodingPlan 的 claim、models、status 三类接口。
- 动态 provider/model 注册机制。
- WebUI 账号状态、登录轮询、模型同步、模型选择器和额度展示。
- DeepSeek V4 Flash / Pro 的模型策略和 1M 上下文意识。

NanoBot 不能直接照搬的部分：

- AtomCode 的私有网关签名实现。
- AtomCode / DeepSeek-TUI 的源码、视觉资产、品牌表达。
- 把免费模型当成普通 OpenAI-compatible base URL 硬配。

短期策略：

- NanoBot 默认模型仍保留 DMX `deepseek-v4-pro-guan`，因为当前已经能正常对话。
- GitCode/CodingPlan 作为第二条模型主线接入，先做登录、领取、模型同步、状态展示。
- 只有在网关调用方案合法且可测时，才把 GitCode 免费模型加入可直接对话的 provider。

## AtomCode 链路拆解

### OAuth 登录

AtomCode 使用平台 broker 做 OAuth，客户端不持有 client secret。

关键位置：

- `crates/atomcode-core/src/auth/oauth.rs`
- `crates/atomcode-daemon/src/api_auth.rs`
- `webui/src/components/LoginButton.tsx`

默认 broker：

```text
https://acs.atomgit.com
```

可通过环境变量覆盖：

```text
ATOMCODE_PLATFORM_SERVER
```

核心端点：

```text
GET  /auth/login?provider=atomgit
GET  /auth/check?state=...
GET  /auth/token?state=...
POST /oauth/refresh
```

本地保存：

```text
~/.atomcode/auth.toml
```

登录流程：

1. 调用 `/auth/login?provider=atomgit`，获得 `login_url` 和 `state`。
2. WebUI 或 CLI 打开浏览器。
3. 每 2 秒调用 `/auth/check?state=...` 轮询。
4. 返回 authorized 后调用 `/auth/token?state=...` 换取 token。
5. 保存 `access_token`、`refresh_token`、`expires_in`、`created_at` 和用户信息。
6. token 过期前 5 分钟使用 `/oauth/refresh` 刷新。

NanoBot 可直接学习这个产品流程，但本地文件应落在 `~/.nanobot/`，不要复用 AtomCode 的 `~/.atomcode/`。

建议 NanoBot 本地状态：

```text
~/.nanobot/
  auth/
    gitcode.json
```

### CodingPlan

关键位置：

- `crates/atomcode-core/src/coding_plan/client.rs`
- `crates/atomcode-core/src/coding_plan/setup.rs`
- `crates/atomcode-core/src/coding_plan/types.rs`
- `crates/atomcode-daemon/src/api_codingplan.rs`

默认 API base：

```text
https://api.gitcode.com/api/v5
```

可通过环境变量覆盖：

```text
ATOMCODE_CODINGPLAN_API_BASE
```

核心端点：

```text
POST /coding-plan/claim-v2
GET  /coding-plan/models-v2?plan_type=Max|Pro|Lite
GET  /coding-plan/status-v2
```

领取策略：

```text
Max -> Pro -> Lite
```

先尝试最高档，失败或无资格时向下回退，遇到 success 或 duplicate 就停止。duplicate 在产品上按“已领取/已持有”处理，不当成失败。

`models-v2` 返回的模型条目包含这些关键字段：

```json
{
  "id": 2052994857682014210,
  "display_model_name": "GLM-5.1",
  "base_url": "https://api-ai.gitcode.com/v1",
  "type": "openai",
  "context_window": 64000,
  "plan_available": true,
  "is_infinity": 2,
  "is_atomcode_exclusive": 1
}
```

NanoBot 不应该硬编码“免费模型一定是 1M 上下文”。正确策略是优先相信服务端返回的 `context_window`，缺失时才使用保守默认值。

### Provider 注册

AtomCode 的注册规则：

- 先清理所有旧的 `AtomGit*` provider。
- 单模型时 provider 名为 `AtomGit`。
- 多模型时 provider 名为 `AtomGit-{display_model_name}`，其中 `/` 替换为 `-`。
- 默认 provider 设置为服务端返回的第一个可用模型。
- `plan_available=false` 的模型只展示，不注册为可切换 provider。
- `api_key` 不写入 provider config，运行时从 OAuth auth 文件读取 token。

AtomCode 的 fallback：

```text
base_url: https://pre-llm-api-cce.atomgit.com/v1
type: openai
context_window: 64000
```

NanoBot 对应设计：

```json
{
  "providers": {
    "gitcode": {
      "kind": "gitcode-codingplan",
      "enabled": true,
      "settings": {
        "apiBase": "https://api.gitcode.com/api/v5",
        "platformBase": "https://acs.atomgit.com"
      },
      "models": []
    }
  }
}
```

`gitcode-codingplan` 不等价于普通 `openai-compatible`，因为它需要 OAuth token、模型目录同步、额度状态，以及可能的请求签名。

### LLM 网关与签名

AtomCode 将 CodingPlan 模型最终打到 OpenAI-compatible 的 chat completions 网关，但请求不是普通 API key 模式。

关键位置：

- `crates/atomcode-core/src/provider/openai.rs`
- `crates/atomcode-core/src/coding_plan/crypto.rs`
- `crates/atomcode-codingplan-crypto/src/lib.rs`

发送请求时会做这些事：

- `Authorization: Bearer {access_token}`
- `Content-Type: application/json`
- 可选 `x-atomcode-session-id`
- 对 GitCode/AtomGit 网关生成 `X-AtomCode-*` 签名头

签名头形状：

```text
X-AtomCode-Sig
X-AtomCode-Ts
X-AtomCode-Nonce
X-AtomCode-Alg
X-AtomCode-Ver
```

重要风险：

- AtomCode 开源默认构建没有签名实现。
- `codingplan-crypto` 是 optional feature。
- `crates/atomcode-codingplan-crypto` 在公开仓库中是 proprietary stub，缺少 `master.rs`、`kdf.rs`、`versions/v1.rs` 等实际实现文件。
- 官方构建可能用私有 overlay 替换该目录。

因此，NanoBot 不能复制或逆向这部分私有签名实现。可行路径只有三类：

1. GitCode/AtomGit 提供公开、授权、可实现的签名协议。
2. NanoBot 调用用户本机已安装的官方 AtomCode binary/daemon 作为桥接。
3. NanoBot 只做登录、模型同步、状态展示，实际对话继续走 DMX 或用户自己的 API key。

另外，当前 AtomCode 源码中存在一个需要实测核对的不一致：

- 注释和测试认为 `pre-llm-api-cce.atomgit.com` 是签名网关。
- `is_atomgit_gateway` 当前实现只匹配 `llm-api.atomgit.com` 和 `api-ai.gitcode.com`。

NanoBot 实现前必须用真实登录态做网关 host 验证，不要盲目照搬任何一个字符串。

## AtomCode WebUI/Daemon 可吸收点

AtomCode daemon 暴露了这些 API：

```text
GET    /auth/status
POST   /auth/login/start
POST   /auth/login/:login_id/poll
DELETE /auth/login/:login_id
POST   /auth/logout
POST   /codingplan/setup
GET    /providers
POST   /providers
PATCH  /providers/:name
DELETE /providers/:name
POST   /providers/:name/default
GET    /config
POST   /config/reload
```

WebUI 的登录按钮负责：

- 读取账号状态。
- 发起登录。
- 打开浏览器。
- 2 秒轮询登录状态。
- 展示用户名/头像。
- 支持退出登录。

`/codingplan/setup` 在 daemon 已经存在，但 WebUI 中未搜到直接调用点。NanoBot 可以在这个地方做得更顺：登录完成后给一个明确的“同步 GitCode 免费模型”按钮，或者在 onboarding 中把“登录并同步模型”做成一步。

建议 NanoBot WebUI 增加：

```text
账号：未登录 / 已登录
按钮：登录 GitCode
按钮：同步 CodingPlan 免费模型
状态：领取套餐、模型列表、额度、过期时间
模型：显示 GitCode 免费模型、context_window、是否可用
风险提示：网关调用需要官方签名/桥接支持
```

## DeepSeek-TUI 模型策略可吸收点

参考仓库：

- `C:\Users\Administrator\Documents\Github\DeepSeek-TUI`
- commit `a970117 DeepSeek-TUI`

DeepSeek-TUI 与 AtomCode 的价值不同。它不是 GitCode 登录来源，而是 DeepSeek V4 模型策略参考。

它的模型注册里包含：

- `deepseek-v4-pro`
- `deepseek-v4-flash`
- `deepseek-ai/deepseek-v4-pro`
- `deepseek-ai/deepseek-v4-flash`
- OpenRouter / Novita / Fireworks / SGLang / vLLM 等映射

它把 legacy aliases 映射到 Flash：

```text
deepseek-chat
deepseek-reasoner
deepseek-r1
deepseek-v3
deepseek-v3.2
```

它的 README 把 V4 Pro / Flash 都按 1M context 设计，并有 auto mode：

- 简单任务走 Flash。
- 编码、调试、架构、安全审查、复杂多步任务升 Pro。
- thinking 支持 `off`、`high`、`max`。
- 路由本身用一次小的 Flash 调用完成。

NanoBot 可以吸收：

- `ModelRegistry` 概念：模型 ID、provider、aliases、supportsTools、supportsReasoning、contextWindow。
- `auto` 模型模式：本地先做启发式，后续再做小模型路由。
- `reasoning_effort` 控制：仅对 DeepSeek V4 系列启用。
- 1M 上下文 UI 认知：展示 token 窗口、压缩状态、缓存命中/成本。

价格信息不要直接照搬。DeepSeek-TUI README 中的 Pro 折扣截至 2026-05-31，已经不是可长期依赖的当前价格来源。NanoBot 若要展示价格，必须单独实时或配置化获取。

## NanoBot 接入方案

### P4.1：GitCode Auth Service

新增服务：

```text
Nanobot.Core/Auth/GitCodeAuthService.cs
Nanobot.Core/Auth/GitCodeAuthStore.cs
Nanobot.Core/Auth/GitCodeAuthModels.cs
```

职责：

- `StartLoginAsync`
- `PollLoginAsync`
- `FinishLoginAsync`
- `RefreshTokenAsync`
- `GetValidAccessTokenAsync`
- `LogoutAsync`

本地保存：

```text
~/.nanobot/auth/gitcode.json
```

Web API：

```text
GET    /api/gitcode/auth/status
POST   /api/gitcode/auth/login/start
POST   /api/gitcode/auth/login/{loginId}/poll
DELETE /api/gitcode/auth/login/{loginId}
POST   /api/gitcode/auth/logout
```

安全要求：

- API 不返回 access token / refresh token。
- UI 只看到用户名、头像、token 是否存在、是否快过期。
- auth 文件加入用户本地数据，不进入仓库。

### P4.2：CodingPlan Service

新增服务：

```text
Nanobot.Core/CodingPlan/GitCodeCodingPlanClient.cs
Nanobot.Core/CodingPlan/GitCodeCodingPlanService.cs
Nanobot.Core/CodingPlan/GitCodeCodingPlanModels.cs
```

职责：

- `ClaimAsync(planType)`
- `ClaimCascadeAsync(Max, Pro, Lite)`
- `ListModelsAsync(planType)`
- `GetStatusAsync`
- `SyncProvidersAsync`

Web API：

```text
POST /api/gitcode/codingplan/setup
GET  /api/gitcode/codingplan/status
GET  /api/gitcode/codingplan/models
```

响应应该包含结构化 step：

```json
{
  "success": true,
  "steps": {
    "login": { "status": "skipped", "message": "already logged in" },
    "claim": { "status": "ok", "message": "CodingPlan Pro active" },
    "models": { "status": "ok", "message": "3 models synced" },
    "status": { "status": "ok", "message": "current usage ..." }
  },
  "defaultProvider": "gitcode",
  "models": []
}
```

### P4.3：Provider / Model 扩展

当前 NanoBot 的 `ProviderSettings` 已经有 `Kind`、`ApiBase`、`DefaultModel`、`Models`、`Capabilities`、`Settings`，可以承接，但 `ModelSettings` 需要扩展元数据。

建议新增字段：

```text
ModelSettings.ContextWindow
ModelSettings.SupportsReasoning
ModelSettings.ReasoningEffort
ModelSettings.PlanAvailable
ModelSettings.DisplayName
ModelSettings.BaseUrl
```

新增 provider kind：

```text
gitcode-codingplan
```

注册策略：

- `gitcode-codingplan` provider 不要求 `ApiKey`。
- 运行时从 `GitCodeAuthStore` 取有效 access token。
- 模型列表由 `CodingPlanService` 同步写入本地 config。
- `plan_available=false` 的模型可以进入 UI 展示，但不能进入可选模型。

### P4.4：网关调用策略

短期不要承诺直连免费网关。

先实现三态：

```text
NotConfigured: 未登录或未同步模型
CatalogOnly: 已登录并同步模型，但缺少可用网关调用方式
Callable: 已具备合法签名/桥接能力，可以直接对话
```

可调用路径优先级：

1. 公开授权的 GitCode/CodingPlan 签名协议。
2. 官方 AtomCode daemon/binary 桥接。
3. DMX fallback。

如果采用 AtomCode 桥接，要保持可选外部依赖：

- NanoBot 不随 MSI 打包 AtomCode。
- WebUI 提供“检测本机 AtomCode”。
- 用户自己安装后，NanoBot 可调用其 daemon 或 CLI。
- 桥接失败时不影响 DMX、Nong、Nong.Toolkit.Net 主线。

### P4.5：WebUI

新增“模型与账号”页面或设置面板分组：

```text
DMX DeepSeek V4 Pro
  API Base
  API Key
  Model

GitCode CodingPlan
  登录状态
  同步免费模型
  额度状态
  可用模型
  网关能力状态

Auto Model
  off / flash / pro / auto
  thinking: off / high / max
```

中文优先：

- “登录 GitCode”
- “同步免费模型”
- “已领取”
- “额度已满”
- “模型列表已更新”
- “当前仅同步模型目录，暂不能直连免费网关”

### P4.6：DeepSeek V4 Auto Mode

第一版可以不用再调用一个路由模型，先做本地启发式：

Flash：

- 简短问答
- 总结
- 翻译
- 小范围解释
- 无工具或少量工具

Pro / high thinking：

- 多文件修改
- 调试失败
- 架构方案
- 安全审查
- 长上下文总结
- 连续工具调用超过阈值

后续再加“小 Flash 路由调用”，但必须可关闭，避免路由调用本身消耗免费额度或增加延迟。

## 验收标准

第一阶段完成时应满足：

1. WebUI 可以发起 GitCode 登录并显示账号状态。
2. NanoBot 可以保存和刷新 GitCode token，但 API 不泄露 token。
3. WebUI 可以调用“同步 CodingPlan 免费模型”。
4. 本地 config 可以出现 `gitcode-codingplan` provider 和服务端返回的模型目录。
5. UI 能区分“模型已同步”和“模型可调用”。
6. 未具备签名/桥接能力时，聊天仍能走 DMX fallback。
7. 所有 HTTP client 用可注入 handler 做单元测试，不依赖真实 token。
8. 不提交任何用户 token、API key、auth 文件。

第二阶段完成时应满足：

1. 有合法可测的免费网关调用路径。
2. GitCode 模型可以在模型选择器中发起真实流式对话。
3. 401 时能 refresh token 并重试一次。
4. 额度耗尽时显示明确中文提示并自动回退 DMX。
5. DeepSeek V4 Flash / Pro 的 context window、reasoning、tools 能力正确显示。

## 当前建议

下一步不建议直接写网关调用。先做 P4.1 和 P4.2：

```text
GitCode 登录
CodingPlan 领取/同步
模型目录进入 NanoBot config
WebUI 可见状态
```

这样即使私有签名短期拿不到，NanoBot 也已经把 GitCode 免费模型体系的入口吃进来了。后续只需要补“可调用 provider”这一段，而不是重新设计账号、模型和 UI。
