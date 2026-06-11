# 2026-06-07 Native Desktop Distribution Plan

## Decision

Nong.NanoBot.Net can use MSI for Windows distribution, but desktop UX must not be implemented as a WebView2, Electron, or browser-shell wrapper.

This follows the Motrix / Motrix Next lesson: Electron plus an aging frontend stack can enable fast early desktop delivery, but it tends to create long-term packaging, dependency, performance, and maintenance debt. NanoBot should not bind its agent runtime to a heavy browser shell.

## Direction

- MSI is a distribution format, not a UI architecture.
- Short-term Windows MSI can install `nanobot.exe`, `Nanobot.Web`, static WebUI assets, config templates, and Start Menu shortcuts.
- Short-term Start Menu entries may launch `nanobot web` and open the user's default browser.
- Future desktop clients must be native UI, such as WinUI or WPF, calling NanoBot HTTP/SSE runtime APIs.
- Do not keep a WebView2/Electron browser engine resident in the background for the desktop UI.
- Nong.Toolkit.Net and Nong.Cli.Net still install through the plugin system rather than being bundled into the MSI payload.

## API Boundary

Native desktop UI must call the same runtime boundary as WebUI:

```text
sessions, stream, events, workspace, plugins, Nong.Toolkit.Net, tools, memory
```
