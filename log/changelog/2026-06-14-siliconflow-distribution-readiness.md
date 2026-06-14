# 2026-06-14 SiliconFlow 分发就绪收敛

## Changed

- 新建并激活 `log/plans/2026-06-14-siliconflow-distribution-readiness-plan.md`，把当前施工目标收窄到 SiliconFlow 首装和分发路径。
- `DefaultProviderCatalog.CreateModelCatalog()` 和 `CreateSecretsTemplate()` 现在只生成 `siliconflow` 默认 provider，不再在首装模板里预置 DMX。
- SiliconFlow 默认模型清单补齐到与 `models.template.json` 对齐，包括 DeepSeek V3.1 Terminus、GLM-4.7 和 Qwen3 32B。
- `ModelSettingsStore` 的 WebUI 设置路径现在只暴露并保存 SiliconFlow：
  - 旧配置中存在 DMX 时，WebUI 设置页仍只展示 SiliconFlow；
  - 保存模型设置会拒绝非 SiliconFlow provider；
  - 保存后 `fallbackModels` 只保留当前 SiliconFlow 模型；
  - API 地址校验改为校验用户最终提交的 URL。
- WebUI 默认工作台移除 GitCode 登录、登出和 CodingPlan 模型同步控件。
- WebUI 后端默认 API 不再映射 `/api/gitcode/*` 端点；核心层 GitCode 代码暂时保留，后续单独规划。
- 真实 OpenAI-compatible 集成测试改为优先读取 `SILICONFLOW_API_KEY` / `SILICONFLOW_API_BASE` / `SILICONFLOW_MODEL`，并在只提供 SiliconFlow key 时自动使用 `https://api.siliconflow.cn/v1/` 和 `nex-agi/Nex-N2-Pro`。
- README 和中文 README 移除 DMX/多 provider 作为首装分发路径的说明，改为只描述 SiliconFlow。

## Verification

- Targeted tests:
  - `dotnet test --filter "FullyQualifiedName~ModelSettingsStoreTests|FullyQualifiedName~WebUiScriptContractTests|FullyQualifiedName~ConfigTests|FullyQualifiedName~RealIntegrationTests"`: 30 passed
- `dotnet build Nanobot.slnx`: 0 warnings, 0 errors
- `dotnet test`: 136 passed, 0 failed, 0 skipped
- `dotnet run --project Nanobot.Web --urls http://127.0.0.1:8800`
  - `GET /api/runtime/status`: 200, ready true, model `siliconflow::nex-agi/Nex-N2-Pro`
  - `GET /api/settings/model`: 200, active provider `siliconflow`, available providers `1`, environment key `SILICONFLOW_API_KEY`
  - `GET /api/system/status`: 200, live `nong.commandCount`: 126
  - `GET /api/gitcode/auth/status`: 404
  - desktop browser smoke: runtime pill `就绪`, `providerOptions = 1`, send enabled, no console/runtime exceptions
  - narrow browser smoke: runtime pill `就绪`, `providerOptions = 1`, send enabled, no console/runtime exceptions
- MSI packaging smoke:
  - `.\eng\package-msi.ps1 -Version 0.1.0 -Configuration Release -RuntimeIdentifier win-x64`
  - MSI created at `artifacts/installer/NanoBot-0.1.0-win-x64.msi`
  - administrative extraction succeeded
  - extracted `nanobot.exe --help` succeeded
  - extracted `nanobot.exe serve --urls http://127.0.0.1:8801` succeeded
  - `http://127.0.0.1:8801/api/settings/model`: SiliconFlow only
  - `http://127.0.0.1:8801/api/gitcode/auth/status`: 404
