# 2026-06-07 WebUI Workbench P2

## Goal

Move the browser workbench from a status/chat shell into a usable local Agent workspace.

## Changes

- Added `POST /api/agent/stream` with newline-delimited JSON streaming events.
- Added WebUI session APIs for listing, creating, and loading persisted sessions.
- Persisted WebUI sessions under `~/.nanobot/workspace/.webui/sessions.json`.
- Added workspace file APIs for bounded directory listing and text file preview.
- Hid internal `.webui` files from the workspace browser and rejected paths outside the workspace.
- Reworked the frontend as a Chinese-default Agent workbench with an optional English toggle.
- Added dark and light theme support from the WebUI header.
- Added streaming chat rendering, session list, workspace file tree, file preview, runtime status, memory preview, tool timeline, and tool detail panels.
- Added root `AGENTS.md` to lock the repository direction: independent .NET 8 runtime, Nong/Nong.Toolkit.Net alignment, Apache-2.0, Chinese-first WebUI, and CodeBuddy as UX reference only.

## Design Notes

- CodeBuddy remains a UI reference for product structure, bilingual support, and theme quality. This implementation uses original code and assets.
- WebUI stays no-build for this phase: ASP.NET Core backend plus static HTML/CSS/JS.
- Chinese is the default language because the owner prefers to operate the tool in Chinese.
- The light theme is a first-class mode and should be checked with the dark theme before future UI commits.

## Verification

Local verification:

- `dotnet build`: passed, 0 warnings, 0 errors.
- `dotnet test`: passed, 77 passed, 0 failed, 0 skipped.
- Self-contained WebUI run on Windows when ASP.NET Core Runtime 8 is not installed: started successfully.

```text
dotnet run --project Nanobot.Web --self-contained -r win-x64 --urls http://127.0.0.1:8788
```

- API smoke:
  - `GET /api/sessions`: returned 200.
  - `GET /api/workspace/files`: returned 200.
  - `GET /api/workspace/file?path=..%2Fconfig.json`: returned 400 and rejected the path as outside the workspace.
  - Empty `POST /api/agent/stream`: returned 400.
  - Real `POST /api/agent/stream`: returned 200 with `session`, `delta`, and `complete` NDJSON events.
- Screenshot smoke:
  - desktop dark Chinese: rendered.
  - desktop light Chinese: rendered.
  - desktop light English: rendered.
  - mobile/narrow layout: rendered with a compact fixed mobile session title.
- Shutdown smoke:
  - `http://127.0.0.1:8788/api/runtime/status` refused connection after Ctrl+C, confirming no WebUI process was left running.
