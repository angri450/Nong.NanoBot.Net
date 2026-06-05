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
    font-family: -apple-system, BlinkMacSystemFont, 'Microsoft YaHei', 'PingFang SC', system-ui, sans-serif;
    background: var(--bg);
    color: var(--fg);
    line-height: 1.7;
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
  p { margin: 12px 0; }
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

  .features { list-style: none; }
  .features li { padding: 8px 0; border-bottom: 1px solid var(--border); }
  .features li:last-child { border: none; }
  .features strong { color: #a78bfa; }

  .status-pass { color: var(--green); }
  .status-ok { color: #d2a8ff; }

  .arch { font-family: monospace; white-space: pre; color: var(--muted); line-height: 1.5; }

  .note { background: var(--card-bg); border: 1px solid var(--border); border-radius: 8px; padding: 14px 18px; margin: 16px 0; font-size: 0.9rem; color: var(--muted); }

  @media (max-width: 640px) {
    body { padding: 20px 16px 60px; }
    h1 { font-size: 1.8rem; }
  }
</style>

<h1>NanoBot.net</h1>
<p class="tagline">
  一个基于 .NET 10 的个人 AI 助手运行时 —— 轻量、CLI 优先、多 Provider 路由、工具安全边界、流式输出、MCP 适配、自带网关。
  受 <a href="https://github.com/HKUDS/nanobot">HKUDS/nanobot</a> 启发。
</p>

<div class="badges">
  <span class="badge good">55 测试通过</span>
  <span class="badge good">0 警告</span>
  <span class="badge good">0 错误</span>
  <span class="badge accent">.NET 10</span>
  <span class="badge accent">C# 14</span>
  <span class="badge">MIT</span>
  <span class="badge">跨平台</span>
</div>

<p style="margin-top:8px">
  <a href="README.md">English README</a>
</p>

<h2>这是什么？</h2>

<p>
  NanoBot.net 把原版 nanobot 的超轻量 Agent 理念用 .NET 10 重新实现了一遍，得到一个类型安全、可测试、结构化程度更高的运行时。
  你得到的是同样的"掌控自己的 AI Agent 技术栈"体验 —— 本地配置、本地工作区、不依赖任何云服务 —— 外加静态编译运行时的可靠性。
</p>

<div class="note">
  <strong>当前阶段：</strong>集成就绪的开发基线。适合本地 Agent 工作流、内部验证、Provider 评测和发布打包。
  还没到可以直接暴露给公网的完整多租户生产服务级别。
</div>

<h2>快速开始</h2>

<pre><code># 1. 安装 .NET 10 SDK，然后：
git clone https://github.com/angri450/NanoBot.net.git
cd NanoBot.net

# 2. 初始化配置和工作区
dotnet run --project Nanobot.CLI -- onboard

# 3. 编辑 ~/.nanobot/config.json，填入 API key（OpenAI 兼容格式即可）
#    也可以设环境变量：OPENAI_API_KEY

# 4. 开始对话
dotnet run --project Nanobot.CLI</code></pre>

<p>
  运行 <code>onboard</code> 后，工作区位于 <code>~/.nanobot/workspace</code>。
  把记忆笔记放在 <code>workspace/memory/MEMORY.md</code>，技能定义放在
  <code>workspace/skills/&lt;名称&gt;/SKILL.md</code>，Agent 会自动读取注入系统提示词。
</p>

<h2>命令一览</h2>

<table>
  <tr><th>命令</th><th>作用</th></tr>
  <tr>
    <td><code>dotnet run --project Nanobot.CLI</code></td>
    <td>交互式聊天（流式输出，默认模式）</td>
  </tr>
  <tr>
    <td><code>dotnet run --project Nanobot.CLI -- chat</code></td>
    <td>显式启动交互聊天</td>
  </tr>
  <tr>
    <td><code>dotnet run --project Nanobot.CLI -- agent -m "..."</code></td>
    <td>单次提问，输出结果后退出</td>
  </tr>
  <tr>
    <td><code>dotnet run --project Nanobot.CLI -- gateway</code></td>
    <td>启动 Telegram bot（需在 config 中配置 channels.telegram）</td>
  </tr>
  <tr>
    <td><code>dotnet run --project Nanobot.CLI -- websocket</code></td>
    <td>启动 WebSocket Agent 网关（支持 token 认证）</td>
  </tr>
  <tr>
    <td><code>dotnet run --project Nanobot.CLI -- onboard</code></td>
    <td>创建 <code>~/.nanobot/</code> 目录，生成默认配置和工作区</td>
  </tr>
</table>

<h2>内置工具</h2>

<div class="grid">
  <div class="card">
    <h3>文件系统</h3>
    <p><code>read_file</code> &middot; <code>write_file</code> &middot; <code>edit_file</code> &middot; <code>list_dir</code></p>
  </div>
  <div class="card">
    <h3>Shell</h3>
    <p><code>run_shell</code> — 限制在工作区范围内执行，带超时和输出截断</p>
  </div>
  <div class="card">
    <h3>网络</h3>
    <p><code>web_search</code> &middot; <code>web_fetch</code> — 带 SSRF 防护</p>
  </div>
  <div class="card">
    <h3>数据</h3>
    <p><code>get_weather</code> &middot; <code>get_stock_price</code></p>
  </div>
  <div class="card">
    <h3>GitHub</h3>
    <p><code>github_*</code> — Issue、PR、仓库、搜索</p>
  </div>
  <div class="card">
    <h3>AI</h3>
    <p><code>summarize</code> — 递归文本摘要</p>
  </div>
  <div class="card">
    <h3>MCP</h3>
    <p>Stdio MCP Server 自动适配为工具</p>
  </div>
  <div class="card">
    <h3>可扩展</h3>
    <p>实现 <code>ITool</code> 接口即可注册</p>
  </div>
</div>

<h2>LLM Provider 支持</h2>

<p>
  Provider 系统由配置文件驱动，环境变量优先覆盖。模型以 <code>providerId::modelId</code> 格式引用。
</p>

<table>
  <tr><th>Provider 类型</th><th>配置键</th><th>流式</th><th>工具调用</th></tr>
  <tr>
    <td>OpenAI 兼容</td>
    <td><code>openai</code></td>
    <td class="status-pass">支持</td>
    <td class="status-pass">支持</td>
  </tr>
  <tr>
    <td>Anthropic</td>
    <td><code>anthropic</code></td>
    <td class="status-ok">非流式</td>
    <td class="status-pass">支持</td>
  </tr>
  <tr>
    <td>Azure OpenAI</td>
    <td><code>azure-openai</code></td>
    <td class="status-ok">非流式</td>
    <td class="status-pass">支持</td>
  </tr>
  <tr>
    <td>Fallback 链</td>
    <td><code>fallbackModels</code></td>
    <td colspan="2">按顺序尝试多个 provider/model，失败自动切换</td>
  </tr>
</table>

<h3>以蚂蚁百灵为例</h3>

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

<p>任何 OpenAI 兼容接口都可以，包括 OpenRouter、DeepSeek、Groq、LM Studio、Ollama 等等。</p>

<h2>环境变量</h2>

<table>
  <tr><th>变量</th><th>用途</th></tr>
  <tr><td><code>OPENAI_API_KEY</code></td><td>OpenAI 兼容 provider 的 API key</td></tr>
  <tr><td><code>OPENAI_API_BASE</code></td><td>覆盖 API base URL</td></tr>
  <tr><td><code>OPENAI_MODEL</code></td><td>覆盖默认模型（<code>provider::model</code> 格式）</td></tr>
  <tr><td><code>ANTHROPIC_API_KEY</code></td><td>启用 Anthropic provider</td></tr>
  <tr><td><code>AZURE_OPENAI_API_KEY</code></td><td>启用 Azure OpenAI</td></tr>
  <tr><td><code>NANOBOT_STREAMING</code></td><td><code>1</code>、<code>true</code>、<code>yes</code> 表示启用流式输出</td></tr>
  <tr><td><code>NANOBOT_WS_PREFIX</code></td><td>WebSocket 监听地址</td></tr>
  <tr><td><code>NANOBOT_WS_TOKEN</code></td><td>WebSocket 认证 token</td></tr>
  <tr><td><code>BRAVE_API_KEY</code></td><td>Web 搜索后端</td></tr>
  <tr><td><code>GITHUB_TOKEN</code></td><td>GitHub 工具访问</td></tr>
</table>

<h2>架构</h2>

<div class="arch">CLI / Telegram / WebSocket
        |
      Agent
        |
   AgentLoop  ──── Memory + Skills + Session History
        |
   AgentRunner ──── Provider + ToolRegistry
        |
  Providers / Built-in Tools / MCP Tools</div>

<h3>核心组件说明</h3>

<ul class="features">
  <li>
    <strong>AgentLoop</strong> — 构建 prompt 上下文：memory、skills、会话历史，发布生命周期事件。
  </li>
  <li>
    <strong>AgentRunner</strong> — 处理 LLM 对话轮次、流式输出 delta、工具调用和工具事件。与 AgentLoop 解耦以便独立测试。
  </li>
  <li>
    <strong>ProviderConfigurationFactory</strong> — 把 config + 环境变量解析为 provider 注册表、模型引用和 fallback 链。
  </li>
  <li>
    <strong>ProviderRegistry</strong> — 按名称注册 provider，附带能力描述符。
  </li>
  <li>
    <strong>FallbackLLMProvider</strong> — 按顺序尝试模型列表，每个模型绑定自己的 provider 和 API model id。
  </li>
  <li>
    <strong>RuntimeEventBus</strong> — 进程内发布/订阅，覆盖 run/tool 的开始/完成/失败事件。
  </li>
  <li>
    <strong>IAgentHook</strong> — run 和 tool 的前置、后置、异常扩展点。
  </li>
  <li>
    <strong>McpToolProvider</strong> — 把 MCP stdio server 的工具转换为 <code>ITool</code> 实例。
  </li>
  <li>
    <strong>SkillLoader</strong> — 扫描 workspace 下 <code>skills/</code> 目录，把 SKILL.md 注入系统提示词。
  </li>
  <li>
    <strong>FileMemoryStore</strong> — 持久化记忆存储，原子写入，自动注入 Agent 上下文。
  </li>
</ul>

<h2>安全模型</h2>

<p>NanoBot.net 提供了实用的安全基线 —— 不是完整沙箱，但足以应对本地和可信网络环境：</p>

<div class="grid">
  <div class="card">
    <h3>网页抓取防护</h3>
    <ul>
      <li>只允许 <code>http</code> / <code>https</code> 协议</li>
      <li>请求前校验 DNS 解析结果</li>
      <li>拦截 loopback、内网、link-local、CGNAT、multicast 等受限地址</li>
      <li>每次重定向前重新校验目标地址</li>
    </ul>
  </div>
  <div class="card">
    <h3>Shell 沙箱</h3>
    <ul>
      <li>命令限制在配置的工作区路径内执行</li>
      <li>工作目录无法逃逸 workspace</li>
      <li>超时限制强制执行</li>
      <li>输出长度可配置截断</li>
    </ul>
  </div>
  <div class="card">
    <h3>网关认证</h3>
    <ul>
      <li>WebSocket 网关支持 <code>Bearer</code> token</li>
      <li>查询参数 token 备用</li>
      <li>工具错误以结构化 JSON 返回</li>
    </ul>
  </div>
</div>

<div class="note">
  不要在缺少更强授权、限流和部署控制的情况下暴露网关给不可信用户。
</div>

<h2>配置说明</h2>

<p>
  CLI 优先读取<strong>环境变量</strong>，未设置时回退到 <code>~/.nanobot/config.json</code>。
  你可以把密钥放在环境变量里，其他配置放在文件中。
</p>

<h3>模型身份</h3>

<p>模型采用 <code>providerId::modelId</code> 模式：</p>

<pre><code>openai::gpt-4o
openrouter::gpt-4o
anthropic::claude-sonnet-4-5
azure-openai::production-chat</code></pre>

<p>
  左侧选择 provider，右侧是配置中的模型 id。每个模型项可通过 <code>apiModelId</code> 映射到实际 API 调用时使用的模型名。
</p>

<h2>网关</h2>

<div class="grid">
  <div class="card">
    <h3>CLI</h3>
    <p>交互式聊天，支持流式输出。输入 <code>exit</code> 或 <code>quit</code> 退出。</p>
  </div>
  <div class="card">
    <h3>Telegram</h3>
    <p>长时间运行的 bot，支持 cron 定时任务。在 config 中配置 <code>channels.telegram</code>。</p>
  </div>
  <div class="card">
    <h3>WebSocket</h3>
    <p>JSON 协议，消息类型包括 <code>delta</code>、<code>response</code>、<code>event</code>、<code>error</code>。支持纯文本和 JSON 输入。</p>
  </div>
</div>

<h2>测试</h2>

<pre><code># 单元测试（55 个测试，始终安全可运行）
dotnet test

# 构建所有项目
dotnet build

# 真实集成测试（需要 API key）
NANOBOT_RUN_INTEGRATION_TESTS=1 OPENAI_API_KEY=... dotnet test --filter FullyQualifiedName~RealIntegrationTests</code></pre>

<table>
  <tr><th>Workflow</th><th>用途</th></tr>
  <tr><td><code>ci.yml</code></td><td>push/PR 时 restore、build、test</td></tr>
  <tr><td><code>integration.yml</code></td><td>手动触发真实 API + WebSocket 集成冒烟测试</td></tr>
  <tr><td><code>release.yml</code></td><td>tag 触发发布，产出 win-x64、linux-x64、osx-arm64 单文件</td></tr>
</table>

<h2>已知边界</h2>

<ul class="features">
  <li><strong>Azure OpenAI</strong> 当前使用 API key 认证，还没接入 AAD / 托管身份。</li>
  <li><strong>Anthropic 和 Azure</strong> provider 目前是非流式；OpenAI 兼容流式已完整实现。</li>
  <li><strong>MCP</strong> stdio 已支持，远程 MCP、OAuth、断线重连和完整生命周期管理还没完成。</li>
  <li><strong>WebSocket</strong> 认证基于 token；完整授权体系、事件过滤和 WebUI 是后续工作。</li>
  <li><strong>不是原版 Python 的完整复刻</strong> —— session compaction、Dream 记忆和部分原始 channel 还没迁移过来。</li>
</ul>

<h2>为什么选 .NET？</h2>

<p>原版 nanobot 是 Python 写的 —— 原型开发快、生态丰富。NanoBot.net 用 .NET 10 实现同样的理念：</p>

<div class="grid">
  <div class="card">
    <h3>类型安全</h3>
    <p>编译器在运行前就能捕获配置不匹配、空引用错误、工具 schema 不一致等问题。</p>
  </div>
  <div class="card">
    <h3>性能</h3>
    <p>JIT 编译运行、无 GIL、原生 async/await。适合长时间运行的网关进程。</p>
  </div>
  <div class="card">
    <h3>部署</h3>
    <p>单文件发布到 win-x64、linux-x64、osx-arm64。目标机器不需要安装 Python 运行时。</p>
  </div>
  <div class="card">
    <h3>生态</h3>
    <p>NuGet 包管理、MSBuild 构建系统、Rider / VS / VS Code 一流 IDE 支持。</p>
  </div>
</div>

<h2>许可证</h2>

<p>MIT —— 随便用，随便改，随便发。</p>

<p style="color:var(--muted); font-size:0.85rem; margin-top:40px; text-align:center;">
  受 <a href="https://github.com/HKUDS/nanobot">HKUDS/nanobot</a> 启发 &middot;
  基于 .NET 10 构建 &middot;
  <a href="https://github.com/angri450/NanoBot.net">GitHub</a>
</p>
