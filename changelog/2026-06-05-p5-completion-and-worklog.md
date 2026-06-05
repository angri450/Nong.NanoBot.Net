# NanoBot.net P5 完成记录与工作量

> 记录日期：2026-06-05
> 背景文档：`changelog/2026-06-05-p4-completion-and-worklog.md`
> 状态：P5 已施工完成，工作量已固化。

---

## 一、P5 定义

P5 被定义为“CI、配置化、release、streaming、gateway 认证和真实集成测试”。本阶段的重点不是继续堆工具，而是把项目推进到可配置、可发布、可真实集成验证的成熟开发基线。

用户特别指出模型配置非常容易出错，因此本阶段参考了 cherry-studio 的 provider/model 配置设计：

- 模型身份不是自由字符串，而是稳定的 `providerId::modelId`。
- Provider 配置与模型选择分离。
- 模型可以有面向用户的 `id`，也可以有真正请求 provider 的 `apiModelId`。
- 默认模型和 fallback chain 都通过稳定模型引用解析。
- 环境变量仍然优先，但不再分散在 CLI 里手写。

---

## 二、工作包与交付物

### P5-0：配置模型调研与迁移策略

交付：

- 对照 cherry-studio 的 provider/model 分离设计。
- 在 NanoBot.net 中确立 `providerId::modelId` 为稳定模型身份。
- 保留旧配置兼容：裸模型名默认解释为 `openai::<model>`。
- 明确 provider 配置、模型选择、fallback chain 的边界。

验收：

- `openai::gpt-4o` 可解析为 provider/model。
- `gpt-4o` 旧写法仍兼容为 OpenAI 默认 provider。
- 空 provider、空 model、非法 model id 会抛出清晰错误。

### P5-1：配置化 Provider 与 Fallback Chain

交付：

- 新增 `ModelReference`。
- 扩展 `AppConfig`：
  - `providers.*.kind/type/enabled/apiKey/apiBase/baseUrl/endpoint/deployment/apiVersion/defaultModel`
  - `providers.*.models[].id/apiModelId/supportsStreaming/supportsTools`
  - `agents.defaults.provider/model/fallbackModels`
  - `streaming.enabled`
  - `gateway.webSocket.prefix/token`
- 新增 `ProviderConfigurationFactory`，集中处理 config/env/provider/fallback。
- 新增 `ModelBoundLLMProvider`，让 fallback chain 中每一项绑定自己的真实 API model。
- CLI 改为使用统一配置工厂，不再手写 OpenAI provider 解析。

验收：

- 环境变量覆盖 config。
- 旧版 `agents.defaults.model = "gpt-legacy"` 仍能运行。
- `apiModelId` 映射生效。
- fallback chain 顺序稳定。
- 未使用的空 provider 不会拖死启动；被引用的 provider 会严格校验。

### P5-2：Provider/Agent/CLI Streaming

交付：

- 新增 `LLMStreamChunk`。
- 新增 `IStreamingLLMProvider`。
- `OpenAICompatibleProvider` 接入 `CompleteChatStreamingAsync`。
- OpenAI-compatible streaming 会累积最终 `LLMResponse`，包括 content、finish reason、usage、tool calls。
- `FallbackLLMProvider` 与 `ModelBoundLLMProvider` 支持 streaming 接口。
- `AgentRunner` 新增 `RunStreamingAsync`，保持原有工具循环和 hook/event 行为。
- `AgentLoop` 与 `Agent` 暴露 streaming API。
- CLI 交互聊天和 `agent -m` 在 streaming enabled 时流式输出。

验收：

- 单 provider OpenAI-compatible 可以真流式输出文本。
- 非 streaming provider 自动回退到原非流式路径。
- streaming fake provider 单元测试覆盖 delta 回调与最终结果。
- 工具调用循环仍复用最终 `LLMResponse`。

### P5-3：WebSocket Gateway 认证与流式 Delta

交付：

- 新增 `WebSocketGatewayAuth`。
- WebSocket gateway 支持 `Authorization: Bearer <token>`。
- WebSocket gateway 支持 `?token=<token>`。
- `gateway.webSocket.token` 与 `NANOBOT_WS_TOKEN` 均可配置 token。
- 未配置 token 时保留本地开发兼容模式，并在 CLI 启动时提示。
- WebSocket protocol 新增 `delta` 消息。
- Gateway 在 streaming enabled 时通过 WebSocket 发送 delta，再发送最终 response。

验收：

- bearer token 和 query token 都能通过。
- 缺失、错误、非 bearer token 会被拒绝。
- protocol 单元测试覆盖 `delta`。

### P5-4：CI、Release 与真实集成测试入口

交付：

- 新增 `.github/workflows/ci.yml`：
  - checkout
  - setup .NET 10
  - restore
  - Release build
  - Release test
- 新增 `.github/workflows/integration.yml`：
  - 手动触发
  - `NANOBOT_RUN_INTEGRATION_TESTS=1`
  - 使用 `OPENAI_API_KEY` secret 跑真实 OpenAI 非流式/流式冒烟测试
  - 跑本地 WebSocket gateway 集成冒烟测试
- 新增 `.github/workflows/release.yml`：
  - tag `v*` 触发
  - 发布 `win-x64`、`linux-x64`、`osx-arm64`
  - 生成 zip artifact
  - 附加到 GitHub Release
- 新增 `RealIntegrationTests`，默认不访问外部服务，只有 env gate 开启时执行真实调用。

验收：

- 默认 `dotnet test` 不需要网络和 API key。
- 手动开启 `NANOBOT_RUN_INTEGRATION_TESTS=1` 时，会要求 `OPENAI_API_KEY`。
- Release workflow 不依赖本地机器配置。

### P5-5：README 英文版与中文版重做

交付：

- `README.md` 整体重写。
- `README.zh-CN.md` 整体重写。
- 文档覆盖：
  - 当前成熟度判断
  - `providerId::modelId`
  - provider/model/fallback 配置示例
  - streaming
  - WebSocket auth
  - CI/release/integration workflows
  - 安全模型
  - 已知边界

验收：

- 旧的“streaming/gateway auth 未完成”描述已移除。
- 英文和中文 README 内容同步。
- README 不夸大生产成熟度，明确当前是 integration-ready development baseline。

---

## 三、测试与验证

新增/更新测试主题：

1. `ModelReference` 解析与非法输入。
2. 配置工厂环境变量覆盖。
3. 旧裸模型配置兼容。
4. `apiModelId` 映射。
5. fallback chain 顺序。
6. 未使用空 provider 跳过。
7. Agent streaming delta 回调。
8. WebSocket delta protocol。
9. WebSocket token auth。
10. 真实 OpenAI 非流式/流式集成入口。
11. 本地 WebSocket gateway 真实集成入口。

测试总数：

```text
P4：39
P5：55
```

验收命令：

```bash
dotnet build
dotnet test
```

真实集成测试入口：

```bash
NANOBOT_RUN_INTEGRATION_TESTS=1 OPENAI_API_KEY=... dotnet test --filter FullyQualifiedName~RealIntegrationTests
```

---

## 四、工作量锁定

本次 P5 工作量按 6 个主题锁定：

1. cherry-studio 风格配置模型调研与迁移策略。
2. Provider/model/fallback 配置化。
3. Provider、Agent、CLI streaming。
4. WebSocket gateway token 认证与 delta 输出。
5. CI、release、真实集成测试入口。
6. README 英文版、中文版与 changelog 固化。

不纳入 P5 的内容：

- Azure OpenAI AAD 认证。
- Anthropic 和 Azure provider 真 streaming。
- 远程 MCP、MCP OAuth、MCP reconnect。
- WebSocket 细粒度授权、事件过滤、完整 WebUI。
- 多租户部署、限流、审计、密钥管理。
- Python 原版 Dream memory、session compaction、全部 channel 对齐。

---

## 五、当前结论

P0-0、P2、P3、P4、P5 已完成。当前项目已经从“能跑的迁移原型”提升到“可配置、可发布、可集成验证的成熟开发基线”。

它现在可以用于：

- 本地 CLI 个人 agent。
- 内部工具链集成。
- OpenAI-compatible provider 的 streaming 验证。
- WebSocket gateway 原型接入。
- CI/release 分支基线。

它还不应该被描述为：

- 完整生产 SaaS。
- 可直接公网暴露的安全服务。
- Python upstream 的完全复刻。
- 多 provider 全部能力齐备的商业级客户端。

若后续继续推进，建议进入 P6，主题应是“生产部署硬化与多 provider 深水区”，包括授权、限流、审计、provider capability matrix、Anthropic/Azure streaming、远程 MCP 生命周期和 WebUI。
