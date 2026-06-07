# 2026-06-08 AtomCode GitCode CodingPlan Analysis

## Added

- Added a Chinese design note for absorbing AtomCode's GitCode OAuth and CodingPlan model-sync flow into NanoBot.net.
- Captured DeepSeek-TUI's DeepSeek V4 Flash/Pro model strategy as a separate model-routing reference.

## Decisions

- Keep DMX `deepseek-v4-pro-guan` as the working default until GitCode/CodingPlan gateway calls are legally and technically callable.
- Treat GitCode/CodingPlan as a special OAuth-backed provider kind, not as a plain OpenAI-compatible API-key provider.
- Do not copy or reverse engineer AtomCode's private request-signing overlay.

## Next

- Implement GitCode auth and CodingPlan model sync first.
- Add WebUI account/model-sync controls before attempting direct gateway calls.
