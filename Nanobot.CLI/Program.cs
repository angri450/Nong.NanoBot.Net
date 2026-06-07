using System.CommandLine;
using Nanobot.Core.Config;
using Nanobot.Core.Events;
using Nanobot.Core.Gateway;
using Nanobot.Core.Providers;
using Nanobot.Core.Tools;
using Nanobot.Core.Tools.Builtin;
using Nanobot.Core.Memory;
using Nanobot.Core.Agent;
using Nanobot.Core.Cron;
using Nanobot.Core.Models;
using Nanobot.Core.Channels;
using Nanobot.Core.Mcp;
using Nanobot.Core.Heartbeat;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Nanobot .NET CLI (Default: Chat Mode)");

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var nanoDir = Path.Combine(home, ".nanobot");
        var configFile = Path.Combine(nanoDir, "config.json");
        var workspace = Path.Combine(nanoDir, "workspace");
        var cronFile = Path.Combine(nanoDir, "cron.json");

        // --- Helper: Setup Agent with unified provider configuration ---
        async Task<(Agent Agent, RuntimeEventBus EventBus, AppConfig Config, bool StreamingEnabled, FileMemoryStore Memory, ILLMProvider Provider)> SetupAgentContextAsync() {
            AppConfig config = File.Exists(configFile) ? ConfigLoader.Load(configFile) : new AppConfig();
            var providerSetup = ProviderConfigurationFactory.Create(config);
            var provider = providerSetup.Provider;

            var registry = new ToolRegistry();
            registry.Register(new ReadFileTool());
            registry.Register(new WriteFileTool());
            registry.Register(new EditFileTool());
            registry.Register(new ListDirTool());
            registry.Register(new ShellTool(workspace));
            registry.Register(new WebSearchTool(Environment.GetEnvironmentVariable("BRAVE_API_KEY") ?? config.WebSearch?.ApiKey ?? ""));
            registry.Register(new WebFetchTool());
            registry.Register(new WeatherTool());
            registry.Register(new StockTool());
            registry.Register(new SummarizeTool(provider));
            if (config.Tools.Nong.Enabled)
            {
                registry.Register(new NongTool(workspace, config.Tools.Nong));
            }
            
            string? githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? (config.Providers.TryGetValue("github", out var gh) ? gh.ApiKey : null);
            registry.Register(new GitHubTool(githubToken));

            var memory = new FileMemoryStore(workspace);
            registry.Register(new MemoryTool(memory));
            await RegisterMcpToolsAsync(registry, config);
            var eventBus = new RuntimeEventBus();
            return (new Agent(provider, registry, memory, eventBus), eventBus, config, providerSetup.StreamingEnabled, memory, provider);
        }

        // --- Default Command Handler (Root) ---
        rootCommand.SetHandler(async () => {
            try {
                var setup = await SetupAgentContextAsync();
                await RunChatLoop(setup.Agent, setup.StreamingEnabled);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        });

        // --- Command: Onboard ---
        var onboardCommand = new Command("onboard", "Initialize configuration and workspace");
        onboardCommand.SetHandler(() => {
            if (!Directory.Exists(nanoDir)) Directory.CreateDirectory(nanoDir);
            if (!File.Exists(configFile)) {
                File.WriteAllText(configFile, """
                {
                  "providers": {
                    "openai": {
                      "kind": "openai-compatible",
                      "apiKey": "",
                      "apiBase": null,
                      "defaultModel": "gpt-4o",
                      "models": [
                        {
                          "id": "gpt-4o",
                          "apiModelId": "gpt-4o",
                          "supportsStreaming": true,
                          "supportsTools": true
                        }
                      ]
                    }
                  },
                  "agents": {
                    "defaults": {
                      "model": "openai::gpt-4o",
                      "fallbackModels": [
                        "openai::gpt-4o"
                      ]
                    }
                  },
                  "streaming": {
                    "enabled": true
                  },
                  "gateway": {
                    "webSocket": {
                      "prefix": "http://localhost:8765/ws/",
                      "token": ""
                    }
                  },
                  "webSearch": {
                    "apiKey": ""
                  },
                  "tools": {
                    "nong": {
                      "enabled": true,
                      "command": "nong",
                      "appendJson": true,
                      "timeoutMs": 120000,
                      "maxOutputChars": 20000,
                      "allowedRoots": [
                        "commands",
                        "word",
                        "inspect",
                        "chart",
                        "excel",
                        "diagram",
                        "genre",
                        "icons",
                        "skill",
                        "pptx",
                        "ocr",
                        "pdf"
                      ]
                    }
                  }
                }
                """);
                Console.WriteLine($"Created default config at {configFile}");
            }
            if (!Directory.Exists(workspace)) Directory.CreateDirectory(workspace);
            Console.WriteLine("Onboarding complete.");
        });
        rootCommand.AddCommand(onboardCommand);

        // --- Command: Gateway ---
        var gatewayCommand = new Command("gateway", "Start enabled chat channels and cron scheduler");
        gatewayCommand.SetHandler(async () => {
            var setup = await SetupAgentContextAsync();
            var agent = setup.Agent;
            var config = ConfigLoader.Load(configFile);
            
            var dream = new DreamConsolidator(setup.Memory, setup.Provider);
            var cronService = new CronService(cronFile, async (job) =>
            {
                if (job.Name.Equals("dream", StringComparison.OrdinalIgnoreCase))
                {
                    var result = await dream.RunOnceAsync();
                    return result.Status;
                }

                return await agent.RunAsync(job.Payload.Message);
            });
            if (setup.Config.Agents.Defaults.Dream.Enabled)
            {
                EnsureNamedCronJob(
                    cronService,
                    "dream",
                    new CronSchedule
                    {
                        Kind = "every",
                        EveryMs = Math.Max(1, setup.Config.Agents.Defaults.Dream.IntervalHours) * 60L * 60L * 1000L
                    },
                    "__dream__"
                );
                Console.WriteLine($"Dream running (every {setup.Config.Agents.Defaults.Dream.IntervalHours}h)...");
            }

            await cronService.StartAsync();
            if (setup.Config.Gateway.Heartbeat.Enabled)
            {
                var heartbeat = new HeartbeatService(
                    workspace,
                    prompt => agent.RunAsync(prompt, AgentExecutionContext.CreateRoot(workspace) with { SessionId = "heartbeat" }),
                    setup.Config.Gateway.Heartbeat.IntervalSeconds
                );
                await heartbeat.StartAsync();
                Console.WriteLine($"Heartbeat running (every {setup.Config.Gateway.Heartbeat.IntervalSeconds}s)...");
            }

            var channels = new List<IMessageChannel>();
            foreach (var (name, settings) in config.Channels.Where(pair => pair.Value.Enabled))
            {
                var channel = ChannelFactory.Create(name, settings, async (inbound, cancellationToken) =>
                {
                    var sessionContext = AgentExecutionContext.CreateRoot(workspace) with
                    {
                        SessionId = inbound.SessionKey
                    };
                    var response = await agent.RunAsync(inbound.Content, sessionContext);
                    return new OutboundMessage(inbound.Channel, inbound.ChatId, response);
                });
                await channel.StartAsync();
                channels.Add(channel);
                Console.WriteLine($"Gateway channel running ({channel.Name})...");
            }

            if (channels.Count == 0)
            {
                Console.WriteLine("No enabled channels configured. Gateway cron is running without chat channels.");
            }

            await Task.Delay(-1);
        });
        rootCommand.AddCommand(gatewayCommand);

        // --- Command: WebSocket Gateway ---
        var websocketCommand = new Command("websocket", "Start the lightweight WebSocket agent gateway");
        websocketCommand.SetHandler(async () => {
            var setup = await SetupAgentContextAsync();
            var prefix = Environment.GetEnvironmentVariable("NANOBOT_WS_PREFIX")
                ?? setup.Config.Gateway.WebSocket.Prefix
                ?? "http://localhost:8765/ws/";
            var authToken = Environment.GetEnvironmentVariable("NANOBOT_WS_TOKEN")
                ?? setup.Config.Gateway.WebSocket.Token;
            var wsGateway = new WebSocketAgentGateway(
                setup.Agent,
                setup.EventBus,
                workspace,
                prefix,
                authToken,
                setup.StreamingEnabled
            );
            Console.WriteLine($"WebSocket gateway listening on {prefix}");
            if (string.IsNullOrWhiteSpace(authToken))
            {
                Console.WriteLine("WebSocket auth token is not configured. Use only for local development.");
            }
            await wsGateway.StartAsync();
        });
        rootCommand.AddCommand(websocketCommand);

        // --- Command: Chat (Explicit) ---
        var chatCommand = new Command("chat", "Start interactive chat (Default)");
        chatCommand.SetHandler(async () => {
            var setup = await SetupAgentContextAsync();
            await RunChatLoop(setup.Agent, setup.StreamingEnabled);
        });
        rootCommand.AddCommand(chatCommand);

        // --- Command: Agent (Single Message) ---
        var msgOption = new Option<string>("--message", "Message to send") { IsRequired = true };
        msgOption.AddAlias("-m");
        var agentCommand = new Command("agent", "Send a single message to the agent");
        agentCommand.AddOption(msgOption);
        agentCommand.SetHandler(async (message) => {
            var setup = await SetupAgentContextAsync();
            if (setup.StreamingEnabled)
            {
                await setup.Agent.RunStreamingAsync(message, WriteConsoleDeltaAsync);
                Console.WriteLine();
            }
            else
            {
                var response = await setup.Agent.RunAsync(message);
                Console.WriteLine(response);
            }
        }, msgOption);
        rootCommand.AddCommand(agentCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task RegisterMcpToolsAsync(ToolRegistry registry, AppConfig config)
    {
        foreach (var (_, serverConfig) in config.Tools.McpServers)
        {
            var client = McpClientFactory.Create(serverConfig);
            var tools = await new McpToolProvider(client).LoadToolsAsync();
            foreach (var tool in tools)
            {
                registry.Register(tool);
            }
        }
    }

    static void EnsureNamedCronJob(CronService cronService, string name, CronSchedule schedule, string message)
    {
        if (cronService.ListJobs(includeDisabled: true).Any(job =>
            job.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        cronService.AddJob(name, schedule, message);
    }

    static async Task RunChatLoop(Agent agent, bool streamingEnabled) {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════╗");
        Console.WriteLine("║        Nanobot.NET Interactive Chat          ║");
        Console.WriteLine("╚══════════════════════════════════════════════╝");
        Console.ResetColor();

        while (true) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nYou: ");
            Console.ResetColor();
            
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.ToLower() is "exit" or "quit") break;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Thinking...");
            Console.ResetColor();

            try {
                Console.Write("\r" + new string(' ', 15) + "\r"); // Clear line
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write("Agent: ");
                Console.ResetColor();
                if (streamingEnabled)
                {
                    await agent.RunStreamingAsync(input, WriteConsoleDeltaAsync);
                    Console.WriteLine();
                }
                else
                {
                    var response = await agent.RunAsync(input);
                    Console.WriteLine(response);
                }
            } catch (Exception ex) {
                Console.WriteLine($"\nError: {ex.Message}");
            }
        }
    }

    static Task WriteConsoleDeltaAsync(string delta, CancellationToken cancellationToken)
    {
        Console.Write(delta);
        return Task.CompletedTask;
    }
}
