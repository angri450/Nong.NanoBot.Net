# 2026-06-07 Plugin And GroundPA Bootstrap Plan

## Decision

NanoBot.net should become the GroundPA host, but it should not ship with full external skill payloads bundled by default.

The product should ship with a generic plugin / skill-pack bootstrap system. Any repository with `plugin.json` should be installable, deployable, updatable, and health-checkable by NanoBot. GroundPA-Toolkit and Nong are first-class plugins on top of this system, not hard-bundled payloads.

- configure plugin marketplace sources
- install repositories with `plugin.json`
- install GroundPA-Toolkit / Nong on demand through the same plugin mechanism
- continue deployment in the background while NanoBot is already running
- detect local Nong health and version
- update installed GroundPA components
- expose plugin, GroundPA, and Nong readiness and capability discovery to the WebUI

## Reference

Claude Code plugin marketplace installation flow is the UX reference:

```text
claude plugin marketplace add https://gitcode.com/angri450/GroundPA-Toolkit.git
claude plugin install groundpa-toolkit@angri450
```

NanoBot should implement its own equivalent flow, for example:

```text
nanobot plugin marketplace add https://gitcode.com/angri450/GroundPA-Toolkit.git
nanobot plugin install groundpa-toolkit@angri450
nanobot plugin status
nanobot plugin update

nanobot groundpa marketplace add https://gitcode.com/angri450/GroundPA-Toolkit.git
nanobot groundpa install groundpa-toolkit@angri450
nanobot groundpa status
nanobot groundpa update
```

The `groundpa` commands are semantic sugar over the generic plugin installer.

## Updated Plan

- P4 is now plugin / GroundPA bootstrap plus agent control panels.
- Runtime API should include plugin status/install/update endpoints plus GroundPA status, install, update, and capability discovery endpoints.
- WebUI should show ready / missing / installing / failed / update-available states for plugins, GroundPA-Toolkit, and Nong.
- GroundPA-Toolkit and Nong can update independently from NanoBot.
