# 2026-06-07 WebUI Visual Refresh P3

## Goal

Upgrade the NanoBot WebUI from a functional P2 workbench into a cleaner desktop-style agent workspace.

## Reference

- DeepSeek GUI was used as a UX reference for the light desktop shell, calm sidebar, large chat canvas, floating composer, and right-side assistant panels.
- DeepSeek TUI was used as a UX reference for visible runtime state, plan, todos, tasks, and agent activity.
- No source code, bundled assets, names, or media were copied from either project.

## Changes

- Reworked the WebUI layout into a cleaner desktop workbench with a light-first visual feel and a matching dark mode.
- Added mode tabs and quick actions in the sidebar.
- Added session count and local workspace labels.
- Added topbar runtime chips for readiness, model, and Nong status.
- Rebuilt the composer as a floating card with model/context hints.
- Added right-side Plan and Todo panels above tool timeline, tool detail, file preview, and memory preview.
- Retuned chat bubbles, spacing, borders, shadows, and responsive behavior for desktop and mobile.
- Kept the existing no-build ASP.NET Core + static HTML/CSS/JS implementation.

## Verification

- `dotnet test`: passed, 77 passed, 0 failed, 0 skipped.
- Initial parallel `dotnet build` hit a Windows `VBCSCompiler` file lock while `dotnet test` was running.
- Sequential `dotnet build`: passed, 0 warnings, 0 errors.
- Self-contained WebUI run:

```text
dotnet run --project Nanobot.Web --self-contained -r win-x64 --urls http://127.0.0.1:8788
```

- API smoke:
  - `GET /api/sessions`: 200.
  - `GET /api/workspace/files`: 200.
- Screenshot smoke:
  - desktop dark Chinese.
  - desktop light Chinese.
  - desktop light English.
  - mobile light Chinese.
