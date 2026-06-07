# NanoBot.net Agent Instructions

This file defines the working direction for development inside this repository.

## Product Direction

- NanoBot.net is the current main line for the GroundPA / Nong agent-runtime layer.
- Treat `DEVELOPMENT_PLAN.zh-CN.md` as the active construction plan for this repository. External GUI/runtime repositories are references unless the user explicitly changes the main implementation target.
- Treat it as an independent .NET runtime, not as a fork/port positioning project.
- Keep the baseline aligned with Nong CLI and GroundPA: .NET 8, Apache-2.0, local-first operation, deterministic tool bridges, and practical safety boundaries.
- NanoBot should not ship with full external skill payloads bundled by default. It should ship with a plugin/skill-pack bootstrap system that can install, update, detect, and run repositories with `plugin.json`; GroundPA-Toolkit and Nong are first-class plugins on top of that system, including background deployment while the runtime is already usable.
- Prefer improving the runtime that already exists here over starting parallel agent runtimes unless a comparison matrix shows a stronger candidate.
- soloncode.net, GenericAgent.net, PilotDeck.net, and related projects are comparison lines, not the default implementation target.

## Technical Baseline

- Target .NET 8 unless the user explicitly changes the platform plan.
- Keep the repo usable from CLI first, then WebUI, then optional native shells such as WinUI.
- Preserve the local-first model: `~/.nanobot/config.json`, `~/.nanobot/workspace`, local memory files, local session data, and no required cloud control plane.
- Keep tool execution explicit, bounded, and inspectable. High-risk bridges such as Nong must use argument arrays, workspace boundaries, allowlists, timeouts, and capped output.
- Prefer existing project patterns in `Nanobot.Core`, `Nanobot.CLI`, `Nanobot.Web`, and tests before introducing new abstractions.

## WebUI Direction

- WebUI is the next product surface and should be usable as the first visual shell.
- The default UI language is Chinese because the owner wants to work primarily in Chinese.
- English can exist as an optional language toggle, but Chinese labels, messages, and flows must stay complete.
- Support both dark and light themes. Do not regress either theme when changing layout or colors.
- CodeBuddy can be used as a UX reference for layout quality, color feel, session management, tool detail display, and bilingual/theme support.
- Do not copy CodeBuddy source code, bundled assets, proprietary names, or visual assets. Implement original UI code and assets in this repo.
- Prefer a dense workbench interface over a marketing landing page: sessions, streaming chat, workspace files, tool calls, runtime status, memory, and configuration feedback.

## Documentation

- Keep `README.md` as the English public entry.
- Keep `README.zh-CN.md` as the detailed Chinese entry.
- When changing meaningful runtime behavior, update both READMEs if the behavior affects users.
- Add dated changelog notes for milestones, especially runtime architecture, WebUI phases, license changes, and integration decisions.
- Avoid describing NanoBot.net as a line-by-line rebuild or fork. Use independent-runtime wording.

## Testing And Verification

- Run `dotnet build` and `dotnet test` before committing meaningful changes.
- For WebUI changes, verify at least one desktop layout and one mobile/narrow layout when feasible.
- For streaming, session persistence, file browsing, or tool-event changes, include API smoke checks when feasible.
- Keep generated artifacts such as screenshots under ignored paths such as `artifacts/`.

## Git Hygiene

- Do not revert unrelated user changes.
- Keep commits scoped to the active milestone.
- Commit messages should be concise and feature/result oriented, for example `feat: expand WebUI workbench`.
- Push after completing a user-requested milestone when the user has already asked for repository updates to be pushed.
