<style>
  :root {
    --bg: #0d1117;
    --fg: #c9d1d9;
    --muted: #8b949e;
    --border: #30363d;
    --accent: #7c3aed;
    --green: #3fb950;
    --code-bg: #161b22;
    --card-bg: #161b22;
  }
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
    background: var(--bg);
    color: var(--fg);
    line-height: 1.6;
    max-width: 960px;
    margin: 0 auto;
    padding: 40px 24px 80px;
  }
  a { color: #a78bfa; text-decoration: none; }
  a:hover { text-decoration: underline; }
  h1 { font-size: 2.5rem; font-weight: 700; margin-bottom: 8px; }
  h2 {
    font-size: 1.5rem; font-weight: 600; margin: 48px 0 16px;
    padding-bottom: 8px; border-bottom: 1px solid var(--border);
  }
  h3 { font-size: 1.15rem; font-weight: 600; margin: 24px 0 10px; }
  p { margin: 12px 0; color: var(--fg); }
  .tagline { font-size: 1.1rem; color: var(--muted); margin-bottom: 24px; }

  .badges { display: flex; flex-wrap: wrap; gap: 8px; margin: 20px 0; }
  .badge {
    display: inline-flex; align-items: center; gap: 6px;
    padding: 4px 12px; border-radius: 20px;
    font-size: 0.8rem; font-weight: 500;
    background: var(--card-bg); border: 1px solid var(--border);
  }
  .badge.good { color: var(--green); border-color: var(--green); }
  .badge.accent { color: #a78bfa; border-color: var(--accent); }

  .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px; margin: 20px 0; }
  .card {
    background: var(--card-bg); border: 1px solid var(--border);
    border-radius: 10px; padding: 20px;
  }
  .card h3 { margin: 0 0 8px; font-size: 1rem; }
  .card p, .card li { font-size: 0.9rem; color: var(--muted); margin: 4px 0; }
  .card ul { padding-left: 18px; }

  pre, code {
    font-family: 'Cascadia Code', 'Fira Code', 'JetBrains Mono', monospace;
    font-size: 0.85rem;
  }
  pre {
    background: var(--code-bg); border: 1px solid var(--border);
    border-radius: 8px; padding: 16px 20px; overflow-x: auto;
    margin: 12px 0; line-height: 1.5;
  }
  code { background: var(--code-bg); padding: 2px 6px; border-radius: 4px; }
  pre code { background: none; padding: 0; }

  table { width: 100%; border-collapse: collapse; margin: 16px 0; font-size: 0.9rem; }
  th, td { padding: 10px 14px; text-align: left; border-bottom: 1px solid var(--border); }
  th { color: var(--muted); font-weight: 600; font-size: 0.8rem; text-transform: uppercase; }
  td code { font-size: 0.8rem; }

  .features { list-style: none; }
  .features li { padding: 8px 0; border-bottom: 1px solid var(--border); }
  .features li:last-child { border: none; }
  .features strong { color: #a78bfa; }

  .status-pass { color: var(--green); }
  .status-ok { color: #d2a8ff; }

  .arch { font-family: monospace; white-space: pre; color: var(--muted); line-height: 1.4; }

  @media (max-width: 640px) {
    body { padding: 20px 16px 60px; }
    h1 { font-size: 1.8rem; }
  }
</style>

<h1>NanoBot.net</h1>
<p class="tagline">
  A .NET 10 personal-agent runtime — small CLI-first core, structured agent loop,
  multi-provider LLM routing, tool safety boundaries, streaming, MCP adaptation, and lightweight gateways.
  Inspired by <a href="https://github.com/HKUDS/nanobot">HKUDS/nanobot</a>.
</p>

<div class="badges">
  <span class="badge good">55 tests passed</span>
  <span class="badge good">0 warnings</span>
  <span class="badge good">0 errors</span>
  <span class="badge accent">.NET 10</span>
  <span class="badge accent">C# 14</span>
  <span class="badge">MIT</span>
  <span class="badge">cross-platform</span>
</div>

<p style="margin-top:8px">
  <a href="README.zh-CN.md">中文说明</a>
</p>

<h2>What Is This?</h2>

<p>
  NanoBot.net takes the ultra-lightweight agent philosophy of the original nanobot
  and rebuilds it on .NET 10 with a typed, testable architecture. You get the same
  "own your agent stack" experience — local config, local workspace, no cloud
  dependency — plus the reliability of a compiled, statically-typed runtime.
</p>

<p>
  It is <strong>integration-ready</strong>: suitable for local agent workflows, internal testing,
  provider evaluation, and release packaging. It is <em>not</em> a fully hardened
  public multi-tenant service yet.
</p>

<h2>Quick Start</h2>

<pre><code># 1. Install .NET 10 SDK, then:
git clone https://github.com/angri450/NanoBot.net.git
cd NanoBot.net

# 2. Initialize config and workspace
dotnet run --project Nanobot.CLI -- onboard

# 3. Edit ~/.nanobot/config.json, add your API key (OpenAI-compatible)
#    Or set environment variable: OPENAI_API_KEY

# 4. Start chatting
dotnet run --project Nanobot.CLI</code></pre>

<p>
  After <code>onboard</code>, your workspace lives at <code>~/.nanobot/workspace</code>.
  Put memory notes in <code>workspace/memory/MEMORY.md</code> and skills in
  <code>workspace/skills/&lt;name&gt;/SKILL.md</code> — the agent picks them up automatically.
</p>

<h2>Commands</h2>

<table>
  <tr><th>Command</th><th>What It Does</th></tr>
  <tr>
    <td><code>dotnet run --project Nanobot.CLI</code></td>
    <td>Interactive chat (streaming, default mode)</td>
  </tr>
  <tr>
    <td><code>dotnet run --project Nanobot.CLI -- chat</code></td>
    <td>Explicit interactive chat</td>
  </tr>
  <tr>
    <td><code>dotnet run --project Nanobot.CLI -- agent -m "..."</code></td>
    <td>Single-turn message, prints response and exits</td>
  </tr>
  <tr>
    <td><code>dotnet run --project Nanobot.CLI -- gateway</code></td>
    <td>Start Telegram bot (requires <code>channels.telegram</code> in config)</td>
  </tr>
  <tr>
    <td><code>dotnet run --project Nanobot.CLI -- websocket</code></td>
    <td>Start WebSocket agent gateway with token auth</td>
  </tr>
  <tr>
    <td><code>dotnet run --project Nanobot.CLI -- onboard</code></td>
    <td>Create <code>~/.nanobot/</code> with default config and workspace</td>
  </tr>
</table>

<h2>Built-in Tools</h2>

<div class="grid">
  <div class="card">
    <h3>Filesystem</h3>
    <p><code>read_file</code> &middot; <code>write_file</code> &middot; <code>edit_file</code> &middot; <code>list_dir</code></p>
  </div>
  <div class="card">
    <h3>Shell</h3>
    <p><code>run_shell</code> — workspace-bounded, timeout + output cap</p>
  </div>
  <div class="card">
    <h3>Web</h3>
    <p><code>web_search</code> &middot; <code>web_fetch</code> — SSRF-guarded</p>
  </div>
  <div class="card">
    <h3>Data</h3>
    <p><code>get_weather</code> &middot; <code>get_stock_price</code></p>
  </div>
  <div class="card">
    <h3>GitHub</h3>
    <p><code>github_*</code> — issues, PRs, repos, search</p>
  </div>
  <div class="card">
    <h3>AI</h3>
    <p><code>summarize</code> — recursive text summarization</p>
  </div>
  <div class="card">
    <h3>MCP</h3>
    <p>Stdio MCP servers auto-adapted as tools</p>
  </div>
  <div class="card">
    <h3>Extensible</h3>
    <p>Implement <code>ITool</code>, register, done</p>
  </div>
</div>

<h2>LLM Providers</h2>

<p>
  The provider system is config-driven with env-var override. Models are referenced as
  <code>providerId::modelId</code>.
</p>

<table>
  <tr><th>Provider Kind</th><th>Config Key</th><th>Streaming</th><th>Tools</th></tr>
  <tr>
    <td>OpenAI-compatible</td>
    <td><code>openai</code></td>
    <td class="status-pass">Yes</td>
    <td class="status-pass">Yes</td>
  </tr>
  <tr>
    <td>Anthropic</td>
    <td><code>anthropic</code></td>
    <td class="status-ok">Non-streaming</td>
    <td class="status-pass">Yes</td>
  </tr>
  <tr>
    <td>Azure OpenAI</td>
    <td><code>azure-openai</code></td>
    <td class="status-ok">Non-streaming</td>
    <td class="status-pass">Yes</td>
  </tr>
  <tr>
    <td>Fallback chain</td>
    <td><code>fallbackModels</code></td>
    <td colspan="2">Sequential retry across providers</td>
  </tr>
</table>

<h3>Example: Ant Ling (蚂蚁百灵)</h3>

<pre><code>{
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

<p>Any OpenAI-compatible endpoint works the same way: OpenRouter, DeepSeek, Groq, LM Studio, Ollama, etc.</p>

<h2>Environment Variables</h2>

<table>
  <tr><th>Variable</th><th>Purpose</th></tr>
  <tr><td><code>OPENAI_API_KEY</code></td><td>API key for the OpenAI-compatible provider</td></tr>
  <tr><td><code>OPENAI_API_BASE</code></td><td>Override the base URL</td></tr>
  <tr><td><code>OPENAI_MODEL</code></td><td>Override default model (<code>provider::model</code> format)</td></tr>
  <tr><td><code>ANTHROPIC_API_KEY</code></td><td>Enable Anthropic provider</td></tr>
  <tr><td><code>AZURE_OPENAI_API_KEY</code></td><td>Enable Azure OpenAI</td></tr>
  <tr><td><code>NANOBOT_STREAMING</code></td><td>Set <code>1</code>, <code>true</code>, or <code>yes</code> to enable streaming</td></tr>
  <tr><td><code>NANOBOT_WS_PREFIX</code></td><td>WebSocket listener prefix</td></tr>
  <tr><td><code>NANOBOT_WS_TOKEN</code></td><td>WebSocket bearer/query token</td></tr>
  <tr><td><code>BRAVE_API_KEY</code></td><td>Web search backend</td></tr>
  <tr><td><code>GITHUB_TOKEN</code></td><td>GitHub tool access</td></tr>
</table>

<h2>Architecture</h2>

<div class="arch">CLI / Telegram / WebSocket
        |
      Agent
        |
   AgentLoop  ──── Memory + Skills + Session History
        |
   AgentRunner ──── Provider + ToolRegistry
        |
  Providers / Built-in Tools / MCP Tools</div>

<h3>Key Components</h3>

<ul class="features">
  <li>
    <strong>AgentLoop</strong> — builds prompt context: memory, skills, session history, and publishes lifecycle events.
  </li>
  <li>
    <strong>AgentRunner</strong> — handles LLM turns, streaming deltas, tool calls, and tool events.
    Separated from the loop for testability.
  </li>
  <li>
    <strong>ProviderConfigurationFactory</strong> — resolves config + env vars into a provider registry,
    model references, and fallback chain.
  </li>
  <li>
    <strong>ProviderRegistry</strong> — named provider catalog with capability descriptors.
  </li>
  <li>
    <strong>FallbackLLMProvider</strong> — tries models in sequence, each bound to its own provider and API model id.
  </li>
  <li>
    <strong>RuntimeEventBus</strong> — in-process pub/sub for run/tool started/completed/failed events.
  </li>
  <li>
    <strong>IAgentHook</strong> — extension points around runs and tools (before, after, on error).
  </li>
  <li>
    <strong>McpToolProvider</strong> — converts MCP stdio server tools into <code>ITool</code> instances.
  </li>
  <li>
    <strong>SkillLoader</strong> — scans workspace <code>skills/</code> directory and injects SKILL.md content
    into the system prompt.
  </li>
  <li>
    <strong>FileMemoryStore</strong> — persistent memory with atomic writes, loaded into agent context.
  </li>
</ul>

<h2>Safety</h2>

<p>NanoBot.net ships with a practical safety baseline — not a complete sandbox, but enough for local and trusted-network use:</p>

<div class="grid">
  <div class="card">
    <h3>Web Fetch Guard</h3>
    <ul>
      <li>Only <code>http</code> / <code>https</code> allowed</li>
      <li>DNS results validated before requests</li>
      <li>Loopback, private, link-local, CGNAT, multicast blocked</li>
      <li>Redirect targets re-checked before following</li>
    </ul>
  </div>
  <div class="card">
    <h3>Shell Sandbox</h3>
    <ul>
      <li>Commands execute inside configured workspace</li>
      <li>Working directory cannot escape workspace</li>
      <li>Timeout limit enforced</li>
      <li>Output truncated at configurable cap</li>
    </ul>
  </div>
  <div class="card">
    <h3>Gateway Auth</h3>
    <ul>
      <li>WebSocket gateway supports <code>Bearer</code> token</li>
      <li>Query-string token fallback</li>
      <li>Tool errors returned as structured JSON</li>
    </ul>
  </div>
</div>

<p style="color:var(--muted); font-size:0.85rem; margin-top:12px;">
  Do not expose the gateway to untrusted users without stronger authorization,
  rate limits, and deployment controls.
</p>

<h2>Configuration</h2>

<p>
  The CLI reads <strong>environment variables first</strong>, then falls back to
  <code>~/.nanobot/config.json</code>. This means you can keep secrets in env vars
  and everything else in the config file.
</p>

<h3>Model Identity</h3>

<p>
  Models use the <code>providerId::modelId</code> pattern, familiar from mature
  provider registries:
</p>

<pre><code>openai::gpt-4o
openrouter::gpt-4o
anthropic::claude-sonnet-4-5
azure-openai::production-chat</code></pre>

<p>
  The left side picks the provider. The right side is the model id in your config.
  Each model entry can map to a different <code>apiModelId</code> for the actual API call.
</p>

<h2>Gateways</h2>

<div class="grid">
  <div class="card">
    <h3>CLI</h3>
    <p>Interactive chat with streaming output. Type <code>exit</code> or <code>quit</code> to leave.</p>
  </div>
  <div class="card">
    <h3>Telegram</h3>
    <p>Long-running bot with cron support. Configure <code>channels.telegram</code> in config.</p>
  </div>
  <div class="card">
    <h3>WebSocket</h3>
    <p>JSON protocol with <code>delta</code>, <code>response</code>, <code>event</code>, <code>error</code> message types. Plain text or JSON input.</p>
  </div>
</div>

<h2>Testing</h2>

<pre><code># Unit tests (55 tests, always safe)
dotnet test

# Build all projects
dotnet build

# Real integration tests (needs API key)
NANOBOT_RUN_INTEGRATION_TESTS=1 OPENAI_API_KEY=... dotnet test --filter FullyQualifiedName~RealIntegrationTests</code></pre>

<table>
  <tr><th>Workflow</th><th>Purpose</th></tr>
  <tr><td><code>ci.yml</code></td><td>Restore, build, test on push/PR</td></tr>
  <tr><td><code>integration.yml</code></td><td>Manual real API + WebSocket smoke tests</td></tr>
  <tr><td><code>release.yml</code></td><td>Tag-triggered publish for win-x64, linux-x64, osx-arm64</td></tr>
</table>

<h2>Known Boundaries</h2>

<ul class="features">
  <li><strong>Azure OpenAI</strong> uses API key auth, not AAD / managed identity.</li>
  <li><strong>Anthropic &amp; Azure</strong> providers are non-streaming today; OpenAI-compatible streaming is implemented.</li>
  <li><strong>MCP</strong> stdio works, but remote MCP, OAuth, reconnect, and lifecycle management are not finished.</li>
  <li><strong>WebSocket</strong> auth is token-level; full authorization, event filtering, and WebUI are future work.</li>
  <li><strong>Not a full port</strong> of the Python upstream — session compaction, Dream memory, and some original channels remain unported.</li>
</ul>

<h2>Why .NET?</h2>

<p>
  The original nanobot is Python — fast to prototype, huge ecosystem.
  NanoBot.net is the same idea on .NET 10:
</p>

<div class="grid">
  <div class="card">
    <h3>Type Safety</h3>
    <p>The compiler catches config mismatches, null-reference errors, and tool schema violations before you run.</p>
  </div>
  <div class="card">
    <h3>Performance</h3>
    <p>JIT-compiled runtime, zero GIL, native async/await. Suitable for long-running gateway processes.</p>
  </div>
  <div class="card">
    <h3>Deployment</h3>
    <p>Single-file publish for win-x64, linux-x64, osx-arm64. No Python runtime needed on the target machine.</p>
  </div>
  <div class="card">
    <h3>Ecosystem</h3>
    <p>NuGet package management, MSBuild build system, first-class IDE support in Rider, VS, and VS Code.</p>
  </div>
</div>

<h2>License</h2>

<p>MIT — use it, fork it, ship it.</p>

<p style="color:var(--muted); font-size:0.85rem; margin-top:40px; text-align:center;">
  Inspired by <a href="https://github.com/HKUDS/nanobot">HKUDS/nanobot</a> &middot;
  Built with .NET 10 &middot;
  <a href="https://github.com/angri450/NanoBot.net">GitHub</a>
</p>
