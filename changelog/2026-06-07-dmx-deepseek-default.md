# 2026-06-07 DMX DeepSeek Default

## Changed

- Switched the onboarding provider example from generic OpenAI `gpt-4o` to DMX DeepSeek V4 Pro.
- Added `DMX_API_KEY`, `DMX_API_BASE`, and `DMX_MODEL` environment overrides.
- Updated English and Chinese README configuration examples to use `dmx::deepseek-v4-pro-guan`.

## Verification

- `dotnet test`
- `dotnet build`
- Real DMX smoke tests for non-streaming and streaming OpenAI-compatible calls using process-local environment variables.
