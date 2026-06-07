# 2026-06-07 P7 Agent Layer Nong Bridge Construction Plan

## P1 Current Facts And Goal

- NanoBot.net is the clean .NET agent runtime candidate for GroundPA.
- Nong CLI is the deterministic GroundPA tool layer.
- Goal: expose Nong to the agent loop as a controlled tool, without making the model compose arbitrary shell commands.
- Non-goal: implement Office, PDF, OCR, or literature logic in NanoBot.net.
- Angri450.Nong currently has unrelated dirty state; this phase does not edit it.

## P2 Design And Boundaries

- Add a `run_nong` built-in tool in NanoBot.net.
- The tool executes a configured Nong command with `ProcessStartInfo.ArgumentList`.
- Tool inputs are argument arrays, not shell command strings.
- Working directories are resolved inside the NanoBot workspace.
- Default allowed Nong roots are published command groups: `commands`, `word`, `inspect`, `chart`, `excel`, `diagram`, `genre`, `icons`, `skill`, `pptx`, `ocr`, and `pdf`.
- The adapter appends `--json` by default unless the call already includes it.

## P3 Execution Steps

- Add Nong tool settings to `AppConfig`.
- Implement `NongTool`.
- Register it from the CLI setup path.
- Add tests for argument passing, allowlist rejection, workspace bounds, and absolute path rejection.
- Update English and Chinese README status/config/safety notes.

## P4 Verification Matrix

- `dotnet test`
- `dotnet build`
- `git status --short --branch`
- Tool behavior verified through unit tests using a fake Nong executable.

## P5 Risks And Follow-Up

- This phase verifies the adapter boundary, not real Nong command semantics.
- A later integration gate should run with a clean, published `nong` executable.
- More granular per-command allowlists can be added when GroundPA agent policies are formalized.
