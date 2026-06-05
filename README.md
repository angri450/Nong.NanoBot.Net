<div style="font-family:-apple-system,BlinkMacSystemFont,Segoe UI,system-ui,sans-serif;color:#c9d1d9;max-width:960px;margin:0 auto;padding:24px">

<h1 style="font-size:2.5rem;font-weight:700;color:#c9d1d9;margin:0 0 8px">NanoBot.net</h1>
<p style="font-size:1.1rem;color:#8b949e;margin:0 0 24px">
  A <strong style="color:#c9d1d9">.NET 10 personal-agent runtime</strong> — typed, testable, production-quality core.
  Structured agent loop, multi-provider LLM routing with fallback, streaming, tool safety boundaries,
  MCP stdio adaptation, and lightweight gateways (CLI / Telegram / WebSocket).
  Inspired by <a href="https://github.com/HKUDS/nanobot" style="color:#a78bfa">HKUDS/nanobot</a>.
</p>

<div style="display:flex;flex-wrap:wrap;gap:8px;margin:20px 0">
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#3fb950;border-color:#3fb950">50 tests passed</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#3fb950;border-color:#3fb950">0 warnings</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#3fb950;border-color:#3fb950">0 errors</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#3fb950;border-color:#3fb950">zero TODOs</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #7c3aed;color:#a78bfa">.NET 10</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #7c3aed;color:#a78bfa">C# 14</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#c9d1d9">MIT</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#c9d1d9">cross-platform</span>
</div>

<p style="margin:8px 0">
  <a href="README.zh-CN.md" style="color:#a78bfa">中文说明</a> &middot;
  <a href="https://github.com/angri450/NanoBot.net/releases" style="color:#a78bfa">Releases</a>
</p>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">What Is This?</h2>

<p style="color:#c9d1d9;margin:12px 0">
  NanoBot.net takes the ultra-lightweight agent philosophy of the original Python nanobot
  and rebuilds it on .NET 10 with a <strong>typed, testable architecture</strong>.
  The codebase has zero TODOs, zero stubs, and zero NotImplementedExceptions — every file is working production code.
  You get the same "own your agent stack" experience — local config, local workspace, no cloud dependency —
  plus the reliability of a compiled, statically-typed runtime.
</p>

<div style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:14px 18px;margin:16px 0;font-size:0.9rem;color:#8b949e">
  <strong>Status:</strong> Integration-ready development baseline. Suitable for local agent workflows,
  internal testing, provider evaluation, and release packaging. Not a fully hardened multi-tenant service yet.
</div>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Quick Start</h2>

<pre style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:16px 20px;overflow-x:auto;margin:12px 0;line-height:1.5;font-size:0.85rem;color:#c9d1d9"><code># 1. Prerequisites: .NET 10 SDK
git clone https://github.com/angri450/NanoBot.net.git
cd NanoBot.net

# 2. Initialize config and workspace (~/.nanobot/)
dotnet run --project Nanobot.CLI -- onboard

# 3. Edit ~/.nanobot/config.json, add your API key
#    Or export OPENAI_API_KEY (env var takes priority)

# 4. Start chatting
dotnet run --project Nanobot.CLI</code></pre>

<p style="color:#c9d1d9;margin:12px 0">
  After <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">onboard</code>,
  your workspace is at <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">~/.nanobot/workspace</code>.
  Drop long-term memory into <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">workspace/memory/MEMORY.md</code>
  and skill definitions into <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">workspace/skills/&lt;name&gt;/SKILL.md</code>.
  The agent picks them up automatically.
</p>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Commands</h2>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Command</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">What It Does</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Interactive chat mode with streaming output (default)</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- agent -m "..."</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Single-turn message, prints response, exits</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- gateway</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Start Telegram bot + cron scheduler</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- websocket</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Start WebSocket agent gateway with token auth</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- onboard</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Create <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">~/.nanobot/</code> with default config + workspace</td></tr>
</table>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Implementation Status</h2>

<p style="color:#c9d1d9;margin:12px 0">
  Full codebase audit (2026-06-06) — <strong>zero TODOs, stubs, or NotImplementedExceptions across all 80+ source files.</strong>
</p>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Module</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Status</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Details</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Agent Loop</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">FULL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Multi-round tool calls, streaming, 6 hook points, session isolation, event bus</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">OpenAI Provider</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">FULL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Streaming + tool calls via OpenAI SDK 2.8. Any compatible endpoint (Ant Ling, OpenRouter, DeepSeek, Ollama...)</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Anthropic Provider</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">FULL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Tool use complete via raw HTTP (Messages API). No streaming yet.</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Azure OpenAI Provider</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">FULL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Tool use complete via raw HTTP (REST API). No streaming yet.</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Fallback Chain</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">FULL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Sequential retry across providers. Works for both sync and streaming paths.</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Provider Config</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">FULL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Env-var override, provider registry, model ID mapping, capability detection</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">10 Built-in Tools</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">FULL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Filesystem, shell, web, weather, stocks, GitHub, summarize — all working</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">MCP (stdio)</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">FULL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">JSON-RPC 2.0, init handshake, tools/list, tools/call. No HTTP/SSE transport yet.</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">WebSocket Gateway</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">FULL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Auth (Bearer + query), streaming, event forwarding, JSON protocol</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Security</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">FULL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">SSRF guard (IPv4 + IPv6), shell sandbox, constant-time token comparison</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Event Bus</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">FULL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Thread-safe pub/sub, 6 event types, snapshot iteration for safe unsubscribe</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Skills</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">FULL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Workspace skill scanning, auto-injected into system prompt</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Cron</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">FULL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">at/every/cron schedules, JSON persistence, wired into gateway command</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">CLI</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">FULL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">5 commands, System.CommandLine, env + config resolution</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">CI/CD</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">FULL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Build/test CI, manual integration smoke tests, tag-triggered cross-platform release</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Memory</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">PARTIAL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Reads MEMORY.md for context injection. No write API — agent cannot persist new memories.</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">MCP (HTTP/SSE)</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">MISSING</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Only stdio transport is implemented. Remote MCP not yet supported.</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Heartbeat</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">UNWIRED</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Fully implemented but never started by any CLI command.</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Channels</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">PARTIAL</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Telegram only. No Discord, Slack, Feishu, or other channels yet.</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Anthropic/Azure Streaming</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">MISSING</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Both implement ILLMProvider but not IStreamingLLMProvider.</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">StockTool</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">FRAGILE</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Scrapes Google Finance HTML with hardcoded CSS class names — will break on site changes.</td></tr>
</table>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Architecture</h2>

<pre style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:16px 20px;overflow-x:auto;margin:12px 0;line-height:1.5;font-size:0.85rem;color:#8b949e">CLI / Telegram / WebSocket
        |
      Agent
        |
   AgentLoop  ---- Memory + Skills + Session History (+ Heartbeat)
        |
   AgentRunner ---- ProviderRegistry + ToolRegistry + RuntimeEventBus
        |
  Providers (OpenAI / Anthropic / Azure / Fallback chain)
  Tools (10 built-in + MCP stdio adapters)
  Hooks (IAgentHook: before/after run, before/after/error tool)</pre>

<h3 style="font-size:1.15rem;font-weight:600;color:#c9d1d9;margin:24px 0 10px">Key Components</h3>

<ul style="list-style:none;color:#c9d1d9;padding:0">
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">AgentLoop</strong> — builds system prompt (memory + skills), manages per-session chat history (capped 20 msgs), publishes lifecycle events. Delegates turns to AgentRunner.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">AgentRunner</strong> — the actual LLM loop: send messages, receive tool calls, execute tools via ToolRegistry, feed results back, loop until done. Max 20 iterations, 15000 char tool output cap. Streaming gracefully degrades to non-streaming for providers without IStreamingLLMProvider.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">IAgentHook</strong> — 6 extension points with default no-op implementations. Supports tool renaming, rejection, and error interception. Used for security policies, logging, and custom behavior.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">ProviderConfigurationFactory</strong> (520 lines) — resolves config + env vars into provider registry, model references, and fallback chain. Maps <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">providerId::modelId</code> to concrete providers with per-model API ID mapping.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">ProviderRegistry</strong> — named provider catalog. Case-insensitive keys. Each registration carries a ProviderDescriptor with capabilities (Chat, Tools, Streaming, Images).</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">FallbackLLMProvider</strong> — tries models in sequence. Catches exceptions and finish-reason errors, moves to next provider. Collects failure messages for debugging. Streaming path buffers chunks per provider — no partial output leakage.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">RuntimeEventBus</strong> — thread-safe in-process pub/sub. Six event types: RunStarted/Completed/Failed, ToolStarted/Completed/Failed. Snapshot-based subscriber iteration for safe concurrent unsubscribe.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">McpStdioClient</strong> — full JSON-RPC 2.0 implementation over stdio. Initialize handshake, tools/list, tools/call. SemaphoreSlim for request serialization. Auto-starts and manages child process lifecycle.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">SkillLoader</strong> — scans <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">workspace/skills/</code>, loads SKILL.md from each subdirectory, concatenates into system prompt. Truncated at 12000 chars.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">FileMemoryStore</strong> — reads <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">MEMORY.md</code> from workspace, injected as "Long-term Memory" context. Read-only — agent has no memory write path yet.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">NetworkSecurityGuard</strong> — blocks SSRF via DNS + IP validation. Covers loopback, RFC 1918 private ranges, link-local, CGNAT, multicast, broadcast, unspecified, and IPv6 unique-local/link-local/multicast. Called before requests and after redirects.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">WebSocketAgentGateway</strong> — HTTP listener upgraded to WebSocket. Bearer + query-string token auth (constant-time comparison). Subscribes to event bus for real-time event push. Thread-safe send via SemaphoreSlim.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">CronService</strong> — at/every/cron schedules via Cronos library. JSON persistence. Polls every second. Wired into gateway command for scheduled agent turns.</li>
</ul>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Built-in Tools</h2>

<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:16px;margin:20px 0">
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Filesystem</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">read_file</code> <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">write_file</code> <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">edit_file</code> <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">list_dir</code></p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Shell</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">run_shell</code> — workspace-bounded, timeout (30s default), output truncation, cross-platform</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Web</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">web_search</code> (Brave API) <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">web_fetch</code> (SSRF-guarded, max 5 redirects)</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Data</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">get_weather</code> (wttr.in, no key) <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">get_stock_price</code> (Google Finance)</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">GitHub</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">github</code> — search repos, list issues via Octokit</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">AI</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">summarize</code> — recursive text/URL summarization via LLM</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">MCP</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">Auto-adapt stdio MCP servers as native tools (JSON-RPC 2.0)</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Extensible</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">Implement <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">ITool</code>, register, done. Hook system for tool interception.</p></div>
</div>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">LLM Providers</h2>

<p style="color:#c9d1d9;margin:12px 0">
  Config-driven with env-var override. Models use <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">providerId::modelId</code> identity.
  The agent gracefully degrades streaming to non-streaming where needed.
</p>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Provider</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Config Key</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Tool Calls</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Streaming</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Implementation</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">OpenAI-compatible</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">openai</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">Yes</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">Yes</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">OpenAI SDK 2.8</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Anthropic</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">anthropic</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">Yes</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">No</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Raw HTTP (Messages API 2023-06-01)</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Azure OpenAI</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">azure-openai</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">Yes</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">No</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Raw HTTP (REST API, api-key auth)</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Fallback chain</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">fallbackModels[]</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">Yes</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">Yes</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Sequential retry, per-provider buffering</td></tr>
</table>

<h3 style="font-size:1.15rem;font-weight:600;color:#c9d1d9;margin:24px 0 10px">Example: Ant Ling (蚂蚁百灵)</h3>

<pre style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:16px 20px;overflow-x:auto;margin:12px 0;line-height:1.5;font-size:0.85rem;color:#c9d1d9"><code>{
  "providers": {
    "openai": {
      "kind": "openai-compatible",
      "apiKey": "sk-studio-...",
      "apiBase": "https://api.ant-ling.com/v1/",
      "defaultModel": "Ling-2.6-1T",
      "models": [{ "id": "Ling-2.6-1T", "apiModelId": "Ling-2.6-1T",
                   "supportsStreaming": true, "supportsTools": true }]
    }
  },
  "agents": {
    "defaults": {
      "model": "openai::Ling-2.6-1T",
      "fallbackModels": ["openai::Ling-2.6-1T"]
    }
  }
}</code></pre>

<p style="color:#c9d1d9;margin:12px 0">Compatible with any OpenAI-format endpoint: OpenRouter, DeepSeek, Groq, LM Studio, Ollama, and more.</p>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Environment Variables</h2>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Variable</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Purpose</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">OPENAI_API_KEY</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">OpenAI-compatible provider API key</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">OPENAI_API_BASE</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Override base URL (for OpenRouter, Ollama, etc.)</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">OPENAI_MODEL</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Override default model (supports <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">provider::model</code> format)</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">ANTHROPIC_API_KEY</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Enable Anthropic provider</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">AZURE_OPENAI_API_KEY</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Enable Azure OpenAI</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">NANOBOT_STREAMING</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Set to <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">1</code>, <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">true</code>, or <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">yes</code> to enable streaming</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">BRAVE_API_KEY</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Web search backend (Brave Search API)</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">GITHUB_TOKEN</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">GitHub tool access (Octokit)</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">NANOBOT_WS_PREFIX</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">WebSocket gateway listener prefix</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">NANOBOT_WS_TOKEN</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">WebSocket gateway auth token</td></tr>
</table>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Safety</h2>

<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:16px;margin:20px 0">
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">SSRF Protection</h3><ul style="padding-left:18px;color:#8b949e;font-size:0.9rem"><li>Only http/https protocols</li><li>DNS validated before every request</li><li>Blocks: loopback, 10.x, 172.16-31.x, 192.168.x, 169.254.x, CGNAT, multicast, broadcast, 0.x</li><li>IPv6: blocks ::1, fc00::/7, fe80::/10, ff00::/8</li><li>Re-checks every redirect target</li></ul></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Shell Sandbox</h3><ul style="padding-left:18px;color:#8b949e;font-size:0.9rem"><li>Working directory bounded to workspace</li><li>Path normalization prevents escape</li><li>Timeout enforced (default 30s, max 120s)</li><li>Output capped at configurable limit</li><li>Process tree killed on timeout</li></ul></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Gateway Auth</h3><ul style="padding-left:18px;color:#8b949e;font-size:0.9rem"><li>Bearer token + query-string fallback</li><li>Constant-time comparison (timing-safe)</li><li>Tools errors returned as structured JSON</li><li>Event forwarding over WebSocket</li></ul></div>
</div>

<div style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:14px 18px;margin:16px 0;font-size:0.9rem;color:#8b949e">
  <strong>Note:</strong> This is a practical safety baseline, not a complete sandbox.
  Do not expose the gateway to untrusted users without stronger authorization, rate limits, and deployment controls.
</div>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Testing</h2>

<pre style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:16px 20px;overflow-x:auto;margin:12px 0;line-height:1.5;font-size:0.85rem;color:#c9d1d9"><code>dotnet test                       # 50 unit tests (always safe, no API keys needed)
dotnet build                      # Build all projects

# Real integration tests (needs API key)
NANOBOT_RUN_INTEGRATION_TESTS=1 OPENAI_API_KEY=... dotnet test --filter ~RealIntegrationTests</code></pre>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Workflow</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Trigger</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Purpose</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">ci.yml</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">push, PR</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Restore, build, test</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">integration.yml</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">manual (workflow_dispatch)</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Real API + WebSocket smoke tests</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">release.yml</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">tag v*</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Cross-platform single-file publish + release</td></tr>
</table>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Known Gaps</h2>

<p style="color:#c9d1d9;margin:12px 0">
  These are the six real gaps found in the audit. All core paths work — these are the next things to build.
</p>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">#</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Area</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Issue</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Impact</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">1</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">Memory (write path)</strong></td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">FileMemoryStore</code> is read-only. Agent has no tool or API to persist new memories.</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Agent cannot learn across sessions</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">2</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">Anthropic/Azure streaming</strong></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Both implement <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">ILLMProvider</code> but not <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">IStreamingLLMProvider</code>.</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">No real-time output for Anthropic/Azure users</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">3</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">MCP HTTP/SSE transport</strong></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Only stdio MCP is implemented. No HTTP-based or SSE transport.</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Cannot connect to remote MCP servers</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">4</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">HeartbeatService unwired</strong></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Fully implemented in <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">HeartbeatService.cs</code> but never started by any CLI command.</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Periodic self-check dead code</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">5</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">Single channel (Telegram only)</strong></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Only <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">TelegramChannel</code> exists. No Discord, Slack, Feishu, or others.</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Limited platform reach</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">6</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">StockTool scraping</strong></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Google Finance HTML scraping with hardcoded CSS class names.</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Will break when Google changes their markup</td></tr>
</table>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Why .NET?</h2>

<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:16px;margin:20px 0">
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Type Safety</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">Compiler catches config mismatches, null refs, and tool schema violations before runtime. Zero runtime type errors in production.</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Performance</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">JIT-compiled, no GIL, native async/await. Long-running gateway processes stay responsive under load.</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Deployment</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">Single-file publish for win-x64, linux-x64, osx-arm64. Target machine needs zero dependencies — no .NET runtime, no Python.</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Testability</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">IHostResolver, IMcpClient, and IAgentHook enable clean unit testing. 50 tests, zero skipped, CI on every push.</p></div>
</div>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">License</h2>

<p style="color:#c9d1d9;margin:12px 0">MIT — use it, fork it, ship it.</p>

<p style="color:#8b949e;font-size:0.85rem;margin-top:40px;text-align:center">
  Inspired by <a href="https://github.com/HKUDS/nanobot" style="color:#a78bfa">HKUDS/nanobot</a> &middot;
  Built with .NET 10 &middot;
  <a href="https://github.com/angri450/NanoBot.net" style="color:#a78bfa">GitHub</a> &middot;
  <a href="https://gitee.com/angri450/NanoBot.net" style="color:#a78bfa">Gitee</a> &middot;
  <a href="https://gitcode.com/angri450/NanoBot.net" style="color:#a78bfa">GitCode</a>
</p>

</div>
