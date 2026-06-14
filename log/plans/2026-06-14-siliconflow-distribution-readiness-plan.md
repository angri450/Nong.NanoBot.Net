# SiliconFlow Distribution Readiness Plan

Date: 2026-06-14
Status: active

## Objective

Make the first distributable Nong.NanoBot.Net path focus on SiliconFlow only:

- onboarding creates a SiliconFlow-first local profile;
- WebUI model settings expose SiliconFlow as the only first-class setup path;
- model/API key handling is clear, local-only, and verified;
- GitCode/CodingPlan login and model sync are moved out of the main workbench path for now;
- distribution docs describe the SiliconFlow path without implying other providers are equally polished.

This is not a provider framework rewrite. Existing legacy/provider internals can remain where they do not affect the first-run distribution path.

## Scope

1. Keep `siliconflow` as the default provider and `nex-agi/Nex-N2-Pro` as the default model.
2. Align generated `models.json`, `secrets.json`, WebUI model settings, README, and Chinese README around SiliconFlow.
3. Remove GitCode login/sync controls from the default WebUI surface.
4. Add tests for the SiliconFlow-only WebUI settings contract and default template shape.
5. Verify build, tests, WebUI API smoke, and desktop/narrow browser smoke.

## Non-Goals

- Do not polish GitCode login, CodingPlan, or free-model sync in this pass.
- Do not publish NuGet packages, MSI installers, GitHub releases, or any remote release artifacts.
- Do not introduce Electron/WebView2.
- Do not remove legacy provider code solely for cleanup if it is not visible in the distribution path.

## Acceptance

- `nanobot onboard` templates do not seed DMX into the default distributable config/secrets/model catalog.
- `/api/settings/model` returns SiliconFlow as the only default available provider for generated config.
- Saving model settings refuses non-SiliconFlow providers through the WebUI API.
- WebUI starts without GitCode account controls on the default workbench.
- `dotnet build Nanobot.slnx` and `dotnet test` pass.
- WebUI API and browser smoke prove runtime/model settings still load.
