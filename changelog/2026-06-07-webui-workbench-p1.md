# 2026-06-07 WebUI Workbench P1

## Goal

Start the visual interface phase with a browser workbench that can be used before a native Windows shell exists.

## Changes

- Added `Nanobot.Web`, an ASP.NET Core WebUI project targeting .NET 8.
- Added runtime status API, agent message API, and server-sent runtime events.
- Added a CodeBuddy-inspired three-column workbench layout with sessions, chat, runtime status, tool events, and memory preview.
- Connected the WebUI backend to `Nanobot.Core`, built-in tools, MCP tools, memory, and the `run_nong` bridge.
- Added degraded startup handling so the WebUI loads even when provider config or MCP startup fails.
- Added WebUI startup command to README and Chinese README.

## Design Notes

- The UI borrows product structure and interaction ideas from modern coding-agent workbenches, but uses original implementation and assets.
- The first milestone is local usability. A native WinUI shell can later wrap or reuse this backend.

## Verification

Local verification:

- `dotnet build`: passed, 0 warnings, 0 errors.
- `dotnet test`: passed, 77 passed, 0 failed, 0 skipped.
- `dotnet run --project Nanobot.Web --self-contained -r win-x64 --urls http://127.0.0.1:8788`: started successfully on a machine without ASP.NET Core Runtime 8 installed.
- `GET /`, `/styles.css`, `/app.js`, and `/api/runtime/status`: returned 200 during smoke.
- `POST /api/agent/message` with an empty message: returned 400 JSON error.
- `GET /favicon.ico`: returned 204.
- Headless Edge screenshot smoke rendered desktop and mobile layouts at `artifacts/webui-smoke.png` and `artifacts/webui-smoke-mobile.png`.
- `GET /api/runtime/status` returned `ready: true`, `model: Ling-2.6-1T`, and `nongEnabled: true` in the local environment.
