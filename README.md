<div style="font-family:-apple-system,BlinkMacSystemFont,Segoe UI,system-ui,sans-serif;color:#c9d1d9;max-width:960px;margin:0 auto;padding:24px">

<h1 style="font-size:2.5rem;font-weight:700;color:#c9d1d9;margin:0 0 8px">NanoBot.net</h1>
<p style="font-size:1.1rem;color:#8b949e;margin:0 0 24px">
  A .NET 10 personal-agent runtime — small CLI-first core, structured agent loop,
  multi-provider LLM routing, tool safety boundaries, streaming, MCP adaptation, and lightweight gateways.
  Inspired by <a href="https://github.com/HKUDS/nanobot" style="color:#a78bfa">HKUDS/nanobot</a>.
</p>

<div style="display:flex;flex-wrap:wrap;gap:8px;margin:20px 0">
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#3fb950;border-color:#3fb950">55 tests passed</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#3fb950;border-color:#3fb950">0 warnings</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#3fb950;border-color:#3fb950">0 errors</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #7c3aed;color:#a78bfa">.NET 10</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #7c3aed;color:#a78bfa">C# 14</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#c9d1d9">MIT</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#c9d1d9">cross-platform</span>
</div>

<p style="margin:8px 0">
  <a href="README.zh-CN.md" style="color:#a78bfa">中文说明</a>
</p>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">What Is This?</h2>

<p style="color:#c9d1d9;margin:12px 0">
  NanoBot.net takes the ultra-lightweight agent philosophy of the original nanobot
  and rebuilds it on .NET 10 with a typed, testable architecture. You get the same
  "own your agent stack" experience — local config, local workspace, no cloud
  dependency — plus the reliability of a compiled, statically-typed runtime.
</p>

<p style="color:#c9d1d9;margin:12px 0">
  It is <strong>integration-ready</strong>: suitable for local agent workflows, internal testing,
  provider evaluation, and release packaging. It is <em>not</em> a fully hardened
  public multi-tenant service yet.
</p>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Quick Start</h2>

<pre style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:16px 20px;overflow-x:auto;margin:12px 0;line-height:1.5;font-size:0.85rem;color:#c9d1d9"><code># 1. Install .NET 10 SDK, then:
git clone https://github.com/angri450/NanoBot.net.git
cd NanoBot.net

# 2. Initialize config and workspace
dotnet run --project Nanobot.CLI -- onboard

# 3. Edit ~/.nanobot/config.json, add your API key (OpenAI-compatible)
#    Or set environment variable: OPENAI_API_KEY

# 4. Start chatting
dotnet run --project Nanobot.CLI</code></pre>

<p style="color:#c9d1d9;margin:12px 0">
  After <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">onboard</code>, your workspace lives at
  <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">~/.nanobot/workspace</code>.
  Put memory notes in <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">workspace/memory/MEMORY.md</code>
  and skills in <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">workspace/skills/&lt;name&gt;/SKILL.md</code>.
</p>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Commands</h2>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Command</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">What It Does</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Interactive chat (streaming, default mode)</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- chat</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Explicit interactive chat</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- agent -m "..."</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Single-turn message, prints response and exits</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- gateway</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Start Telegram bot</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- websocket</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Start WebSocket agent gateway with token auth</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- onboard</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Create <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">~/.nanobot/</code> with default config</td></tr>
</table>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Built-in Tools</h2>

<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:16px;margin:20px 0">
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Filesystem</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">read_file</code> &middot; <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">write_file</code> &middot; <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">edit_file</code> &middot; <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">list_dir</code></p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Shell</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">run_shell</code> — workspace-bounded, timeout + output cap</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Web</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">web_search</code> &middot; <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">web_fetch</code> — SSRF-guarded</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Data</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">get_weather</code> &middot; <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">get_stock_price</code></p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">GitHub</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">github_*</code> — issues, PRs, repos, search</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">AI</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">summarize</code> — recursive text summarization</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">MCP</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">Stdio MCP servers auto-adapted as tools</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Extensible</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">Implement <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">ITool</code>, register, done</p></div>
</div>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">LLM Providers</h2>

<p style="color:#c9d1d9;margin:12px 0">
  The provider system is config-driven with env-var override. Models are referenced as
  <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">providerId::modelId</code>.
</p>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Provider Kind</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Config Key</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Streaming</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Tools</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">OpenAI-compatible</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">openai</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">Yes</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">Yes</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Anthropic</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">anthropic</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">Non-streaming</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">Yes</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Azure OpenAI</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">azure-openai</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">Non-streaming</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">Yes</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Fallback chain</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">fallbackModels</code></td><td colspan="2" style="padding:10px 14px;border-bottom:1px solid #30363d">Sequential retry across providers</td></tr>
</table>

<h3 style="font-size:1.15rem;font-weight:600;color:#c9d1d9;margin:24px 0 10px">Example: Ant Ling</h3>

<pre style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:16px 20px;overflow-x:auto;margin:12px 0;line-height:1.5;font-size:0.85rem;color:#c9d1d9"><code>{
  "providers": {
    "openai": {
      "kind": "openai-compatible",
      "apiKey": "sk-studio-...",
      "apiBase": "https://api.ant-ling.com/v1/",
      "defaultModel": "Ling-2.6-1T",
      "models": [
        {
          "id": "Ling-2.6-1T",
          "apiModelId": "Ling-2.6-1T",
          "supportsStreaming": true,
          "supportsTools": true
        }
      ]
    }
  },
  "agents": {
    "defaults": {
      "model": "openai::Ling-2.6-1T",
      "fallbackModels": ["openai::Ling-2.6-1T"]
    }
  }
}</code></pre>

<p style="color:#c9d1d9;margin:12px 0">Any OpenAI-compatible endpoint works: OpenRouter, DeepSeek, Groq, LM Studio, Ollama, etc.</p>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Environment Variables</h2>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Variable</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Purpose</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">OPENAI_API_KEY</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">API key for OpenAI-compatible provider</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">OPENAI_API_BASE</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Override base URL</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">ANTHROPIC_API_KEY</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Enable Anthropic provider</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">AZURE_OPENAI_API_KEY</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Enable Azure OpenAI</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">NANOBOT_STREAMING</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Set 1/true/yes to enable streaming</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">BRAVE_API_KEY</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Web search backend</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">GITHUB_TOKEN</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">GitHub tool access</td></tr>
</table>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Architecture</h2>

<pre style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:16px 20px;overflow-x:auto;margin:12px 0;line-height:1.5;font-size:0.85rem;color:#8b949e">CLI / Telegram / WebSocket
        |
      Agent
        |
   AgentLoop  ---- Memory + Skills + Session History
        |
   AgentRunner ---- Provider + ToolRegistry
        |
  Providers / Built-in Tools / MCP Tools</pre>

<h3 style="font-size:1.15rem;font-weight:600;color:#c9d1d9;margin:24px 0 10px">Key Components</h3>

<ul style="list-style:none;color:#c9d1d9;padding:0">
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">AgentLoop</strong> — builds prompt context: memory, skills, session history, lifecycle events.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">AgentRunner</strong> — LLM turns, streaming deltas, tool calls, tool events. Separated for testability.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">ProviderConfigurationFactory</strong> — config+env into provider registry, model refs, fallback chain.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">ProviderRegistry</strong> — named provider catalog with capability descriptors.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">FallbackLLMProvider</strong> — sequential model/provider fallback.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">RuntimeEventBus</strong> — in-process pub/sub for lifecycle events.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">IAgentHook</strong> — extension points around runs and tools.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">McpToolProvider</strong> — converts MCP stdio tools into <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">ITool</code> instances.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">SkillLoader</strong> — scans workspace skills/ and injects into system prompt.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">FileMemoryStore</strong> — persistent memory with atomic writes.</li>
</ul>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Safety</h2>

<p style="color:#c9d1d9;margin:12px 0">Practical safety baseline for local and trusted-network use:</p>

<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:16px;margin:20px 0">
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Web Fetch Guard</h3><ul style="padding-left:18px;color:#8b949e;font-size:0.9rem"><li>Only http/https allowed</li><li>DNS validated before request</li><li>Loopback, private, link-local, CGNAT blocked</li><li>Redirect targets re-checked</li></ul></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Shell Sandbox</h3><ul style="padding-left:18px;color:#8b949e;font-size:0.9rem"><li>Commands inside workspace</li><li>Cannot escape workspace</li><li>Timeout enforced</li><li>Output cap configurable</li></ul></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Gateway Auth</h3><ul style="padding-left:18px;color:#8b949e;font-size:0.9rem"><li>WebSocket Bearer token</li><li>Query-string token fallback</li><li>Errors as structured JSON</li></ul></div>
</div>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Testing</h2>

<pre style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:16px 20px;overflow-x:auto;margin:12px 0;line-height:1.5;font-size:0.85rem;color:#c9d1d9"><code>dotnet test                       # 55 tests, always safe
dotnet build                      # Build all projects
NANOBOT_RUN_INTEGRATION_TESTS=1 OPENAI_API_KEY=... dotnet test --filter ~RealIntegrationTests</code></pre>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem">Workflow</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem">Purpose</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">ci.yml</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Restore, build, test on push/PR</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">integration.yml</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Manual real API + WebSocket smoke tests</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">release.yml</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Tag-triggered publish win/linux/osx</td></tr>
</table>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Known Boundaries</h2>

<ul style="list-style:none;color:#c9d1d9;padding:0">
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">Azure OpenAI</strong> — API key auth, not AAD/managed identity.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">Anthropic &amp; Azure</strong> — non-streaming currently; OpenAI-compatible streaming done.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">MCP</strong> — stdio works; remote, OAuth, reconnect not finished.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">WebSocket</strong> — token auth; full authorization and WebUI are future work.</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">Not a full Python port</strong> — session compaction, Dream memory not yet ported.</li>
</ul>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">Why .NET?</h2>

<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:16px;margin:20px 0">
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Type Safety</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">Compiler catches config mismatches, null refs, schema violations before runtime.</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Performance</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">JIT-compiled, zero GIL, native async/await for long-running processes.</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Deployment</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">Single-file publish for win-x64, linux-x64, osx-arm64. No runtime needed.</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Ecosystem</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">NuGet packages, MSBuild, first-class IDE support.</p></div>
</div>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">License</h2>

<p style="color:#c9d1d9;margin:12px 0">MIT — use it, fork it, ship it.</p>

<p style="color:#8b949e;font-size:0.85rem;margin-top:40px;text-align:center">
  Inspired by <a href="https://github.com/HKUDS/nanobot" style="color:#a78bfa">HKUDS/nanobot</a> &middot;
  Built with .NET 10 &middot;
  <a href="https://github.com/angri450/NanoBot.net" style="color:#a78bfa">GitHub</a>
</p>

</div>
