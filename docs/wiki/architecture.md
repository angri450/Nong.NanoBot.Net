# Nong.NanoBot.Net Architecture

Last updated: 2026-06-13

Nong.NanoBot.Net is the .NET 8 local-first runtime layer for the Nong ecosystem.

## Main Components

```text
Nanobot.CLI
  command-line entry points

Nanobot.Core
  agent loop, providers, tools, memory, MCP, Nong bridge, plugin management

Nanobot.Web
  WebUI workbench, sessions, streaming, status panels, Nong/plugin/model surfaces

Nanobot.Tests
  verification for runtime behavior
```

## Dependency Direction

```text
Nong.Cli.Net
  nong commands and OpenAI tool schema export
        |
        v
Nong.Toolkit.Net
  installable skills/plugins
        |
        v
Nong.NanoBot.Net
  runtime discovery, bridge, approval, status, UI
```

NanoBot should discover and install external capabilities; it should not copy the whole CLI/Toolkit payload into the runtime core by default.

## Runtime Boundaries

- Keep tool execution explicit, bounded, and inspectable.
- Nong calls use argument arrays, not shell command strings.
- WebUI is the first visual shell.
- Native desktop can be evaluated later through WinUI/WPF calling runtime APIs.
- Do not introduce Electron or WebView2 as the desktop architecture without an explicit direction change.
