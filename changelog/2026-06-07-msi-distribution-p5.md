# 2026-06-07 MSI Distribution P5

## Added

- Added a Windows x64 MSI packaging path with WiX Toolset.
- Added `eng/package-msi.ps1` to publish the CLI and WebUI runtime, generate WiX file components, and build a local MSI artifact.
- Added `nanobot web` and `nanobot serve` commands so the installed CLI can launch the local browser workbench without a WebView2 or Electron shell.
- The MSI installs per user, adds `nanobot.exe` to the user PATH, and creates Start Menu shortcuts.

## Notes

- GroundPA-Toolkit and Nong are not bundled in the MSI payload. They remain plugin/bootstrap responsibilities.
- Generated packages are written to `artifacts/installer/` and stay out of git.
