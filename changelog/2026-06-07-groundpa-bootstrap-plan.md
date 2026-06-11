# 2026-06-07 Plugin And Nong.Toolkit.Net Bootstrap Plan

## Decision

Nong.NanoBot.Net should become the Nong.Toolkit.Net host, but it should not ship with full external skill payloads bundled by default.

The product should ship with a generic plugin / skill-pack bootstrap system. Any repository with `plugin.json` should be installable, deployable, updatable, and health-checkable by NanoBot. Nong.Toolkit.Net and Nong.Cli.Net are first-class plugins on top of this system, not hard-bundled payloads.

- configure plugin marketplace sources
- install repositories with `plugin.json`
- install Nong.Toolkit.Net / Nong on demand through the same plugin mechanism
- continue deployment in the background while NanoBot is already running
- detect local Nong health and version
- update installed Nong.Toolkit.Net components
- expose plugin, Nong.Toolkit.Net, and Nong.Cli.Net readiness and capability discovery to the WebUI

## Reference

Claude Code plugin marketplace installation flow is the UX reference:

```text
claude plugin marketplace add https://gitcode.com/angri450/Nong.Toolkit.Net.git
claude plugin install nong-toolkit@angri450
```

NanoBot should implement its own equivalent flow, for example:

```text
nanobot plugin marketplace add https://gitcode.com/angri450/Nong.Toolkit.Net.git
nanobot plugin install nong-toolkit@angri450
nanobot plugin status
nanobot plugin update

nanobot nong-toolkit marketplace add https://gitcode.com/angri450/Nong.Toolkit.Net.git
nanobot nong-toolkit install nong-toolkit@angri450
nanobot nong-toolkit status
nanobot nong-toolkit update
```

The `nong-toolkit` commands are semantic sugar over the generic plugin installer.

## Updated Plan

- P4 is now plugin / Nong.Toolkit.Net bootstrap plus agent control panels.
- Runtime API should include plugin status/install/update endpoints plus Nong.Toolkit.Net status, install, update, and capability discovery endpoints.
- WebUI should show ready / missing / installing / failed / update-available states for plugins, Nong.Toolkit.Net, and Nong.
- Nong.Toolkit.Net and Nong.Cli.Net can update independently from NanoBot.
