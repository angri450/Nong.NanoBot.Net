# 2026-06-07 WebUI Model Settings

## Added

- Added WebUI model settings APIs for reading and saving DMX DeepSeek V4 Pro configuration.
- Added a Chinese-first model settings panel with API base, model ID, and API key fields.
- Saved keys are written only to the local `~/.nanobot/config.json` file and are returned to the browser only as masked status.

## Changed

- Replaced single-character rail labels with clear text labels.
- Moved model settings near the top of the sidebar so a fresh MSI install has an obvious place to configure the runtime.
