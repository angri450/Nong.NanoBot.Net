<div style="font-family:-apple-system,BlinkMacSystemFont,Microsoft YaHei,PingFang SC,system-ui,sans-serif;color:#c9d1d9;max-width:960px;margin:0 auto;padding:24px">

<h1 style="font-size:2.5rem;font-weight:700;color:#c9d1d9;margin:0 0 8px">NanoBot.net</h1>
<p style="font-size:1.1rem;color:#8b949e;margin:0 0 24px">
  一个基于 .NET 10 的个人 AI 助手运行时 —— 轻量、CLI 优先、多 Provider 路由、工具安全边界、流式输出、MCP 适配、自带网关。
  受 <a href="https://github.com/HKUDS/nanobot" style="color:#a78bfa">HKUDS/nanobot</a> 启发。
</p>

<div style="display:flex;flex-wrap:wrap;gap:8px;margin:20px 0">
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#3fb950;border-color:#3fb950">55 测试通过</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#3fb950;border-color:#3fb950">0 警告</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#3fb950;border-color:#3fb950">0 错误</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #7c3aed;color:#a78bfa">.NET 10</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #7c3aed;color:#a78bfa">C# 14</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#c9d1d9">MIT</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#c9d1d9">跨平台</span>
</div>

<p style="margin:8px 0">
  <a href="README.md" style="color:#a78bfa">English README</a>
</p>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">这是什么？</h2>

<p style="color:#c9d1d9;margin:12px 0">
  NanoBot.net 把原版 nanobot 的超轻量 Agent 理念用 .NET 10 重新实现了一遍，得到一个类型安全、可测试、结构化程度更高的运行时。
  你得到的是同样的"掌控自己的 AI Agent 技术栈"体验 —— 本地配置、本地工作区、不依赖任何云服务 —— 外加静态编译运行时的可靠性。
</p>

<div style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:14px 18px;margin:16px 0;font-size:0.9rem;color:#8b949e">
  <strong>当前阶段：</strong>集成就绪的开发基线。适合本地 Agent 工作流、内部验证、Provider 评测和发布打包。
  还没到可以直接暴露给公网的完整多租户生产服务级别。
</div>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">快速开始</h2>

<pre style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:16px 20px;overflow-x:auto;margin:12px 0;line-height:1.5;font-size:0.85rem;color:#c9d1d9"><code># 1. 安装 .NET 10 SDK，然后：
git clone https://github.com/angri450/NanoBot.net.git
cd NanoBot.net

# 2. 初始化配置和工作区
dotnet run --project Nanobot.CLI -- onboard

# 3. 编辑 ~/.nanobot/config.json，填入 API key
#    也可以设环境变量：OPENAI_API_KEY

# 4. 开始对话
dotnet run --project Nanobot.CLI</code></pre>

<p style="color:#c9d1d9;margin:12px 0">
  运行 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">onboard</code> 后，工作区位于
  <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">~/.nanobot/workspace</code>。
  把记忆笔记放在 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">workspace/memory/MEMORY.md</code>，
  技能定义放在 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">workspace/skills/&lt;名称&gt;/SKILL.md</code>。
</p>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">命令一览</h2>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">命令</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">作用</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">交互式聊天（流式输出，默认模式）</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- agent -m "..."</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">单次提问，输出结果后退出</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- gateway</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">启动 Telegram bot</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- websocket</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">启动 WebSocket 网关（支持 token 认证）</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- onboard</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">创建 ~/.nanobot/ 目录，生成默认配置和工作区</td></tr>
</table>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">内置工具</h2>

<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:16px;margin:20px 0">
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">文件系统</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">read_file</code> &middot; <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">write_file</code> &middot; <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">edit_file</code> &middot; <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">list_dir</code></p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Shell</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">run_shell</code> — 限制在工作区，带超时和输出截断</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">网络</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">web_search</code> &middot; <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">web_fetch</code> — 带 SSRF 防护</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">数据</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">get_weather</code> &middot; <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">get_stock_price</code></p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">GitHub</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">github_*</code> — Issue、PR、仓库、搜索</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">AI</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">summarize</code> — 递归文本摘要</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">MCP</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">Stdio MCP Server 自动适配为工具</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">可扩展</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">实现 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">ITool</code> 接口即可注册</p></div>
</div>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">LLM Provider 支持</h2>

<p style="color:#c9d1d9;margin:12px 0">
  Provider 系统由配置文件驱动，环境变量优先覆盖。模型以 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">providerId::modelId</code> 格式引用。
</p>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Provider 类型</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">配置键</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">流式</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">工具调用</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">OpenAI 兼容</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">openai</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">支持</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">支持</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Anthropic</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">anthropic</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">非流式</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">支持</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Azure OpenAI</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">azure-openai</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">非流式</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">支持</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Fallback 链</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">fallbackModels</code></td><td colspan="2" style="padding:10px 14px;border-bottom:1px solid #30363d">按顺序尝试多个 provider/model，失败自动切换</td></tr>
</table>

<h3 style="font-size:1.15rem;font-weight:600;color:#c9d1d9;margin:24px 0 10px">以蚂蚁百灵为例</h3>

<pre style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:16px 20px;overflow-x:auto;margin:12px 0;line-height:1.5;font-size:0.85rem;color:#c9d1d9"><code>{
  "providers": {
    "openai": {
      "kind": "openai-compatible",
      "apiKey": "sk-studio-...",
      "apiBase": "https://api.ant-ling.com/v1/",
      "defaultModel": "Ling-2.6-1T",
      "models": [
        { "id": "Ling-2.6-1T", "apiModelId": "Ling-2.6-1T",
          "supportsStreaming": true, "supportsTools": true }
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

<p style="color:#c9d1d9;margin:12px 0">任何 OpenAI 兼容接口都可以，包括 OpenRouter、DeepSeek、Groq、LM Studio、Ollama 等等。</p>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">环境变量</h2>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">变量</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">用途</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">OPENAI_API_KEY</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">OpenAI 兼容 provider 的 API key</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">OPENAI_API_BASE</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">覆盖 API base URL</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">ANTHROPIC_API_KEY</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">启用 Anthropic provider</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">AZURE_OPENAI_API_KEY</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">启用 Azure OpenAI</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">NANOBOT_STREAMING</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">1 / true / yes 启用流式输出</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">BRAVE_API_KEY</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Web 搜索后端</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">GITHUB_TOKEN</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">GitHub 工具访问</td></tr>
</table>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">架构</h2>

<pre style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:16px 20px;overflow-x:auto;margin:12px 0;line-height:1.5;font-size:0.85rem;color:#8b949e">CLI / Telegram / WebSocket
        |
      Agent
        |
   AgentLoop  ---- Memory + Skills + Session History
        |
   AgentRunner ---- Provider + ToolRegistry
        |
  Providers / Built-in Tools / MCP Tools</pre>

<h3 style="font-size:1.15rem;font-weight:600;color:#c9d1d9;margin:24px 0 10px">核心组件说明</h3>

<ul style="list-style:none;color:#c9d1d9;padding:0">
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">AgentLoop</strong> — 构建 prompt 上下文：memory、skills、会话历史，发布生命周期事件。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">AgentRunner</strong> — 处理 LLM 对话轮次、流式 delta、工具调用和工具事件，与 AgentLoop 解耦。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">ProviderConfigurationFactory</strong> — 把 config + 环境变量解析为 provider 注册表、模型引用和 fallback 链。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">ProviderRegistry</strong> — 按名称注册 provider，附带能力描述符。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">FallbackLLMProvider</strong> — 按顺序尝试多个 model/provider。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">RuntimeEventBus</strong> — 进程内发布/订阅，覆盖 run/tool 生命周期事件。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">IAgentHook</strong> — run 和 tool 的前置、后置、异常扩展点。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">McpToolProvider</strong> — 把 MCP stdio server 的工具转换为 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">ITool</code>。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">SkillLoader</strong> — 扫描 workspace 下 skills/ 目录注入系统提示词。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">FileMemoryStore</strong> — 持久化记忆存储，原子写入。</li>
</ul>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">安全模型</h2>

<p style="color:#c9d1d9;margin:12px 0">实用的安全基线，足以应对本地和可信网络环境：</p>

<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:16px;margin:20px 0">
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">网页抓取防护</h3><ul style="padding-left:18px;color:#8b949e;font-size:0.9rem"><li>只允许 http/https</li><li>请求前校验 DNS</li><li>拦截内网、loopback 等受限地址</li><li>重定向前重新校验目标</li></ul></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Shell 沙箱</h3><ul style="padding-left:18px;color:#8b949e;font-size:0.9rem"><li>命令限制在 workspace 内</li><li>无法逃逸工作目录</li><li>超时强制执行</li><li>输出长度可配置</li></ul></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">网关认证</h3><ul style="padding-left:18px;color:#8b949e;font-size:0.9rem"><li>WebSocket Bearer token</li><li>查询参数 token 备用</li><li>错误返回结构化 JSON</li></ul></div>
</div>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">测试</h2>

<pre style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:16px 20px;overflow-x:auto;margin:12px 0;line-height:1.5;font-size:0.85rem;color:#c9d1d9"><code>dotnet test                       # 55 测试，始终安全
dotnet build                      # 构建所有项目
NANOBOT_RUN_INTEGRATION_TESTS=1 OPENAI_API_KEY=... dotnet test --filter ~RealIntegrationTests</code></pre>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem">Workflow</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem">用途</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">ci.yml</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">push/PR 时 build、test</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">integration.yml</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">手动触发真实 API 集成测试</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">release.yml</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">tag 触发发布 win/linux/osx</td></tr>
</table>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">已知边界</h2>

<ul style="list-style:none;color:#c9d1d9;padding:0">
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">Azure OpenAI</strong> — 当前是 API key 认证，还没接入 AAD。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">Anthropic 和 Azure</strong> — 目前非流式；OpenAI 兼容流式已完整实现。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">MCP</strong> — stdio 已支持，远程 MCP、OAuth、断线重连未完成。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">WebSocket</strong> — token 认证；完整授权和 WebUI 是后续工作。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">不是原版完整复刻</strong> — session compaction、Dream 记忆未迁移。</li>
</ul>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">为什么选 .NET？</h2>

<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:16px;margin:20px 0">
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">类型安全</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">编译器在运行前捕获配置不匹配、空引用和 schema 错误。</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">性能</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">JIT 编译、无 GIL、原生 async/await，适合长时运行。</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">部署</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">单文件发布 win/linux/osx，目标机无需安装运行时。</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">生态</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">NuGet 包管理、MSBuild、一流 IDE 支持。</p></div>
</div>

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">许可证</h2>

<p style="color:#c9d1d9;margin:12px 0">MIT —— 随便用，随便改，随便发。</p>

<p style="color:#8b949e;font-size:0.85rem;margin-top:40px;text-align:center">
  受 <a href="https://github.com/HKUDS/nanobot" style="color:#a78bfa">HKUDS/nanobot</a> 启发 &middot;
  基于 .NET 10 构建 &middot;
  <a href="https://github.com/angri450/NanoBot.net" style="color:#a78bfa">GitHub</a>
</p>

</div>
