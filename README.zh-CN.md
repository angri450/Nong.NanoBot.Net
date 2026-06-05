<div style="font-family:-apple-system,BlinkMacSystemFont,Microsoft YaHei,PingFang SC,system-ui,sans-serif;color:#c9d1d9;max-width:960px;margin:0 auto;padding:24px">

<h1 style="font-size:2.5rem;font-weight:700;color:#c9d1d9;margin:0 0 8px">NanoBot.net</h1>
<p style="font-size:1.1rem;color:#8b949e;margin:0 0 24px">
  基于 <strong style="color:#c9d1d9">.NET 10 的个人 AI 助手运行时</strong> —— 类型安全、可测试、生产级核心质量。
  结构化 Agent Loop、多 Provider 路由与 fallback、流式输出、工具安全边界、
  MCP stdio 适配、三种轻量网关（CLI / Telegram / WebSocket）。
  受 <a href="https://github.com/HKUDS/nanobot" style="color:#a78bfa">HKUDS/nanobot</a> 启发。
</p>

<div style="display:flex;flex-wrap:wrap;gap:8px;margin:20px 0">
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#3fb950;border-color:#3fb950">50 测试通过</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#3fb950;border-color:#3fb950">0 警告</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#3fb950;border-color:#3fb950">0 错误</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#3fb950;border-color:#3fb950">零 TODO</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #7c3aed;color:#a78bfa">.NET 10</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #7c3aed;color:#a78bfa">C# 14</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#c9d1d9">MIT</span>
  <span style="display:inline-flex;align-items:center;gap:6px;padding:4px 12px;border-radius:20px;font-size:0.8rem;font-weight:500;background:#161b22;border:1px solid #30363d;color:#c9d1d9">跨平台</span>
</div>

<p style="margin:8px 0">
  <a href="README.md" style="color:#a78bfa">English README</a> &middot;
  <a href="https://github.com/angri450/NanoBot.net/releases" style="color:#a78bfa">Releases</a>
</p>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">这是什么？</h2>

<p style="color:#c9d1d9;margin:12px 0">
  NanoBot.net 把原版 Python nanobot 的超轻量 Agent 理念用 .NET 10 重新实现，
  得到一个<strong>类型安全、架构清晰、可测试的运行时</strong>。
  全项目 80+ 源文件，<strong>零 TODO、零 stub、零 NotImplementedException</strong> —— 每个文件都是能跑的生产代码。
  你得到的是同样的"掌控自己的 AI Agent 技术栈"体验 —— 本地配置、本地工作区、不依赖云 —— 外加静态编译的可靠性。
</p>

<div style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:14px 18px;margin:16px 0;font-size:0.9rem;color:#8b949e">
  <strong>当前阶段：</strong>集成就绪的开发基线。适合本地 Agent 工作流、内部验证、Provider 评测和发布打包。
  还没到可以暴露给公网的完整多租户生产服务级别。
</div>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">快速开始</h2>

<pre style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:16px 20px;overflow-x:auto;margin:12px 0;line-height:1.5;font-size:0.85rem;color:#c9d1d9"><code># 1. 前置条件：安装 .NET 10 SDK
git clone https://github.com/angri450/NanoBot.net.git
cd NanoBot.net

# 2. 初始化配置和工作区（~/.nanobot/）
dotnet run --project Nanobot.CLI -- onboard

# 3. 编辑 ~/.nanobot/config.json，填入 API key
#    或设置环境变量 OPENAI_API_KEY（优先级更高）

# 4. 开始对话
dotnet run --project Nanobot.CLI</code></pre>

<p style="color:#c9d1d9;margin:12px 0">
  运行 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">onboard</code> 后，
  工作区位于 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">~/.nanobot/workspace</code>。
  将长期记忆放入 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">workspace/memory/MEMORY.md</code>，
  技能定义放入 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">workspace/skills/&lt;名称&gt;/SKILL.md</code>，
  Agent 会自动读取注入系统提示词。
</p>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">命令一览</h2>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">命令</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">作用</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">交互式聊天，流式输出（默认模式）</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- agent -m "..."</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">单次提问，输出结果后退出</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- gateway</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">启动 Telegram bot + cron 定时任务</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- websocket</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">启动 WebSocket 网关（支持 token 认证）</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">dotnet run --project Nanobot.CLI -- onboard</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">创建 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">~/.nanobot/</code> 及默认配置 + 工作区</td></tr>
</table>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">实现全景</h2>

<p style="color:#c9d1d9;margin:12px 0">
  2026-06-06 全量代码审计 — <strong>80+ 源文件，零 TODO、零 stub、零 NotImplementedException。</strong>
</p>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">模块</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">状态</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">详情</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Agent Loop</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">完整</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">多轮工具调用、流式、6 个 hook 点、session 隔离、事件总线</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">OpenAI Provider</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">完整</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">流式 + 工具调用，基于 OpenAI SDK 2.8。支持任意兼容端点（百灵、OpenRouter、DeepSeek、Ollama...）</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Anthropic Provider</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">完整</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">工具调用完整（原始 HTTP + Messages API）。暂缺流式。</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Azure OpenAI Provider</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">完整</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">工具调用完整（原始 HTTP + REST API）。暂缺流式。</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Fallback 链</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">完整</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">多 provider 顺序重试，同步和流式路径均支持</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Provider 配置</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">完整</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">环境变量覆盖、provider 注册表、模型 ID 映射、能力检测</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">10 个内置工具</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">完整</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">文件系统、Shell、网络、天气、股票、GitHub、摘要 — 全部可用</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">MCP (stdio)</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">完整</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">JSON-RPC 2.0、init 握手、tools/list、tools/call。暂缺 HTTP/SSE 传输。</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">WebSocket 网关</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">完整</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">认证（Bearer + query）、流式、事件转发、JSON 协议</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">安全</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">完整</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">SSRF 防护（IPv4 + IPv6）、Shell 沙箱、常量时间 token 比对</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">事件总线</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">完整</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">线程安全 pub/sub、6 种事件类型、快照迭代安全取消订阅</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Skills</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">完整</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">扫描工作区 skill 目录，自动注入系统提示词</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Cron 定时任务</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">完整</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">at/every/cron 三种模式，JSON 持久化，已接入 gateway 命令</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">CLI</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">完整</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">5 个命令，System.CommandLine，env + config 双通道解析</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">CI/CD</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">完整</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">自动 build/test CI、手动集成冒烟测试、tag 触发跨平台发布</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Memory</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">部分</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">可读 MEMORY.md 注入上下文。无写入 API — Agent 无法自动记录新记忆。</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">MCP (HTTP/SSE)</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">缺失</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">仅实现 stdio 传输，未支持远程 MCP。</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Heartbeat</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">未接入</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">代码完整但所有 CLI 命令均未启动 HeartbeatService。</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Channel</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">部分</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">仅有 Telegram。无 Discord、Slack、飞书等。</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Anthropic/Azure 流式</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">缺失</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">两者仅实现 ILLMProvider，未实现 IStreamingLLMProvider。</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">StockTool</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">脆弱</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">抓取 Google Finance HTML 用硬编码 CSS 类名解析，网站改版即失效。</td></tr>
</table>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">架构</h2>

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

<h3 style="font-size:1.15rem;font-weight:600;color:#c9d1d9;margin:24px 0 10px">核心组件详解</h3>

<ul style="list-style:none;color:#c9d1d9;padding:0">
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">AgentLoop</strong> — 构建系统提示词（memory + skills），管理会话历史（上限 20 条），发布生命周期事件。将会话轮次委托给 AgentRunner。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">AgentRunner</strong> — 实际 LLM 循环：发消息、收工具调用、执行工具、回传结果、继续循环。最多 20 轮，工具输出截断 15000 字符。不支持流式的 provider 自动降级为非流式。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">IAgentHook</strong> — 6 个扩展点，默认空实现。支持工具重命名、拒绝执行、异常捕获。用于安全策略、日志记录和自定义行为。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">ProviderConfigurationFactory</strong>（520 行）— 解析 config + 环境变量为 provider 注册表、模型引用和 fallback 链。将 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">providerId::modelId</code> 映射到具体 provider。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">ProviderRegistry</strong> — 按名称管理 provider。不区分大小写。每个注册项附带能力描述符（Chat、Tools、Streaming、Images）。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">FallbackLLMProvider</strong> — 顺序尝试多个模型。捕获异常和 finish-reason 错误，自动切换。流式路径按 provider 缓冲 —— 失败的 provider 不会泄露部分输出。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">RuntimeEventBus</strong> — 线程安全进程内 pub/sub。6 种事件类型。快照迭代支持安全取消订阅。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">McpStdioClient</strong> — 完整 JSON-RPC 2.0 stdio 实现。init 握手、tools/list、tools/call。SemaphoreSlim 序列化请求。自动管理子进程生命周期。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">SkillLoader</strong> — 扫描 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">workspace/skills/</code>，加载各子目录的 SKILL.md，拼接进系统提示词。上限 12000 字符。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">FileMemoryStore</strong> — 从 workspace 读取 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">MEMORY.md</code>，作为「长期记忆」注入上下文。只读 —— Agent 暂无记忆写入路径。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">NetworkSecurityGuard</strong> — 通过 DNS + IP 校验阻止 SSRF。覆盖 loopback、RFC 1918 内网、link-local、CGNAT、multicast、broadcast、IPv6 唯一本地/链路本地/组播。每次重定向前重新校验。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">WebSocketAgentGateway</strong> — HTTP 监听升级为 WebSocket。Bearer + query-string token 认证（常量时间比对）。订阅事件总线实时推送。SemaphoreSlim 线程安全发送。</li>
  <li style="padding:8px 0;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">CronService</strong> — at/every/cron 三种调度，Cronos 库解析。JSON 持久化。每秒轮询。已接入 gateway 命令。</li>
</ul>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">内置工具</h2>

<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:16px;margin:20px 0">
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">文件系统</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">read_file</code> <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">write_file</code> <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">edit_file</code> <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">list_dir</code></p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Shell</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">run_shell</code> — 限制在工作区，超时控制（默认 30s），输出截断，跨平台</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">网络</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">web_search</code>（Brave API） <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">web_fetch</code>（SSRF 防护，最多 5 次重定向）</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">数据</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">get_weather</code>（wttr.in，无需 key） <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">get_stock_price</code>（Google Finance）</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">GitHub</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">github</code> — 搜索仓库、查看 Issue（基于 Octokit）</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">AI</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">summarize</code> — 递归文本/URL 摘要（调用 LLM）</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">MCP</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">自动将 stdio MCP server 适配为原生工具（JSON-RPC 2.0）</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">可扩展</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">实现 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">ITool</code> 接口即注册。Hook 系统支持工具拦截。</p></div>
</div>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">LLM Provider 支持</h2>

<p style="color:#c9d1d9;margin:12px 0">
  配置驱动，环境变量优先覆盖。模型以 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.85rem">providerId::modelId</code> 标识。
  Agent 对流式缺失的 provider 自动降级为非流式。
</p>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Provider</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">配置键</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">工具调用</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">流式</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">实现方式</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">OpenAI 兼容</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">openai</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">支持</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">支持</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">OpenAI SDK 2.8</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Anthropic</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">anthropic</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">支持</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">不支持</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">原始 HTTP（Messages API 2023-06-01）</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Azure OpenAI</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">azure-openai</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">支持</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#d2a8ff">不支持</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">原始 HTTP（REST API，api-key 认证）</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">Fallback 链</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">fallbackModels[]</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">支持</td><td style="padding:10px 14px;border-bottom:1px solid #30363d;color:#3fb950">支持</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">顺序重试，每个 provider 独立缓冲</td></tr>
</table>

<h3 style="font-size:1.15rem;font-weight:600;color:#c9d1d9;margin:24px 0 10px">示例：接入蚂蚁百灵</h3>

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

<p style="color:#c9d1d9;margin:12px 0">兼容任意 OpenAI 格式接口：OpenRouter、DeepSeek、Groq、LM Studio、Ollama 等。</p>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">环境变量</h2>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">变量</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">用途</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">OPENAI_API_KEY</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">OpenAI 兼容 provider 的 API key</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">OPENAI_API_BASE</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">覆盖 API base URL（用于 OpenRouter、Ollama 等）</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">OPENAI_MODEL</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">覆盖默认模型（支持 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">provider::model</code> 格式）</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">ANTHROPIC_API_KEY</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">启用 Anthropic provider</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">AZURE_OPENAI_API_KEY</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">启用 Azure OpenAI</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">NANOBOT_STREAMING</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">1</code> / <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">true</code> / <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">yes</code> 启用流式输出</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">BRAVE_API_KEY</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Web 搜索后端（Brave Search API）</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">GITHUB_TOKEN</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">GitHub 工具访问（Octokit）</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">NANOBOT_WS_PREFIX</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">WebSocket 网关监听地址</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">NANOBOT_WS_TOKEN</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">WebSocket 网关认证 token</td></tr>
</table>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">安全模型</h2>

<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:16px;margin:20px 0">
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">SSRF 防护</h3><ul style="padding-left:18px;color:#8b949e;font-size:0.9rem"><li>仅允许 http/https 协议</li><li>每次请求前校验 DNS</li><li>拦截：loopback、10.x、172.16-31.x、192.168.x、169.254.x、CGNAT、组播、广播、0.x</li><li>IPv6：拦截 ::1、fc00::/7、fe80::/10、ff00::/8</li><li>每次重定向前重新校验</li></ul></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">Shell 沙箱</h3><ul style="padding-left:18px;color:#8b949e;font-size:0.9rem"><li>工作目录限制在 workspace 内</li><li>路径规范化防逃逸</li><li>超时强制终止（默认 30s，最大 120s）</li><li>输出长度可配置截断</li><li>进程树完整终止</li></ul></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">网关认证</h3><ul style="padding-left:18px;color:#8b949e;font-size:0.9rem"><li>Bearer token + 查询参数备选</li><li>常量时间比较（防时序攻击）</li><li>工具错误以结构化 JSON 返回</li><li>WebSocket 实时事件推送</li></ul></div>
</div>

<div style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:14px 18px;margin:16px 0;font-size:0.9rem;color:#8b949e">
  <strong>注意：</strong>这是实用的安全基线，不是完整沙箱。不要在缺少更强授权、限流和部署控制的情况下暴露网关给不可信用户。
</div>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">测试</h2>

<pre style="background:#161b22;border:1px solid #30363d;border-radius:8px;padding:16px 20px;overflow-x:auto;margin:12px 0;line-height:1.5;font-size:0.85rem;color:#c9d1d9"><code>dotnet test                       # 50 个单元测试（始终安全，无需 API key）
dotnet build                      # 构建所有项目

# 真实集成测试（需要 API key）
NANOBOT_RUN_INTEGRATION_TESTS=1 OPENAI_API_KEY=... dotnet test --filter ~RealIntegrationTests</code></pre>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">Workflow</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">触发</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">用途</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">ci.yml</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">push、PR</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Restore、build、test</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">integration.yml</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">手动触发</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">真实 API + WebSocket 集成冒烟测试</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">release.yml</code></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">tag v*</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">跨平台单文件发布 + Release</td></tr>
</table>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">六个已知缺口</h2>

<p style="color:#c9d1d9;margin:12px 0">
  审计发现的六个真实缺口。所有核心路径正常工作 —— 这些是下一步要补的内容。
</p>

<table style="width:100%;border-collapse:collapse;margin:16px 0;font-size:0.9rem;color:#c9d1d9">
  <tr><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">#</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">模块</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">问题</th><th style="padding:10px 14px;text-align:left;border-bottom:1px solid #30363d;color:#8b949e;font-size:0.8rem;text-transform:uppercase">影响</th></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">1</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">Memory 写入</strong></td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">FileMemoryStore</code> 只读，Agent 无法自动记录新记忆。</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Agent 无法跨会话学习</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">2</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">Anthropic/Azure 流式</strong></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">两者仅实现 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">ILLMProvider</code>，未实现 <code style="background:#161b22;padding:2px 6px;border-radius:4px;font-size:0.8rem">IStreamingLLMProvider</code>。</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">用户看不到逐字输出效果</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">3</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">MCP HTTP/SSE 传输</strong></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">仅支持 stdio，无 HTTP 或 SSE 传输层。</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">无法连接远程 MCP 服务</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">4</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">HeartbeatService 未接入</strong></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">代码完整但所有 CLI 命令均未启动。</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">周期性自检功能闲置</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">5</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">Channel 单一</strong></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">仅有 Telegram。无 Discord、Slack、飞书等。</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">平台覆盖有限</td></tr>
  <tr><td style="padding:10px 14px;border-bottom:1px solid #30363d">6</td><td style="padding:10px 14px;border-bottom:1px solid #30363d"><strong style="color:#a78bfa">StockTool 脆弱</strong></td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Google Finance HTML 抓取，硬编码 CSS 类名。</td><td style="padding:10px 14px;border-bottom:1px solid #30363d">Google 改版即失效</td></tr>
</table>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">为什么选 .NET？</h2>

<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:16px;margin:20px 0">
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">类型安全</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">编译器在运行前捕获配置不匹配、空引用和 schema 错误。生产环境零运行时类型错误。</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">性能</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">JIT 编译、无 GIL、原生 async/await。长时运行网关进程保持响应。</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">部署</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">win/linux/osx 单文件发布。目标机器零依赖 —— 不需要 .NET 运行时，不需要 Python。</p></div>
  <div style="background:#161b22;border:1px solid #30363d;border-radius:10px;padding:20px"><h3 style="margin:0 0 8px;font-size:1rem;color:#c9d1d9">可测试性</h3><p style="font-size:0.9rem;color:#8b949e;margin:4px 0">IHostResolver、IMcpClient、IAgentHook 提供干净的可测试抽象。50 测试、零跳过、每次 push 自动 CI。</p></div>
</div>

<!-- ============================================================ -->

<h2 style="font-size:1.5rem;font-weight:600;color:#c9d1d9;margin:48px 0 16px;padding-bottom:8px;border-bottom:1px solid #30363d">许可证</h2>

<p style="color:#c9d1d9;margin:12px 0">MIT —— 随便用，随便改，随便发。</p>

<p style="color:#8b949e;font-size:0.85rem;margin-top:40px;text-align:center">
  受 <a href="https://github.com/HKUDS/nanobot" style="color:#a78bfa">HKUDS/nanobot</a> 启发 &middot;
  基于 .NET 10 构建 &middot;
  <a href="https://github.com/angri450/NanoBot.net" style="color:#a78bfa">GitHub</a> &middot;
  <a href="https://gitee.com/angri450/NanoBot.net" style="color:#a78bfa">Gitee</a> &middot;
  <a href="https://gitcode.com/angri450/NanoBot.net" style="color:#a78bfa">GitCode</a>
</p>

</div>
