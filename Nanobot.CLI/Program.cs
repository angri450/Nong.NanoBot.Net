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
        (Agent Agent, RuntimeEventBus EventBus, AppConfig Config, bool StreamingEnabled) SetupAgentContext() {
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
            
            string? githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? (config.Providers.TryGetValue("github", out var gh) ? gh.ApiKey : null);
            registry.Register(new GitHubTool(githubToken));

            var memory = new FileMemoryStore(workspace);
            var eventBus = new RuntimeEventBus();
            return (new Agent(provider, registry, memory, eventBus), eventBus, config, providerSetup.StreamingEnabled);
        }

        Agent SetupAgent() {
            var setup = SetupAgentContext();
            return setup.Agent;
        }

        // --- Default Command Handler (Root) ---
        rootCommand.SetHandler(async () => {
            try {
                var setup = SetupAgentContext();
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
        var gatewayCommand = new Command("gateway", "Start the Telegram bot gateway");
        gatewayCommand.SetHandler(async () => {
            var agent = SetupAgent();
            var config = ConfigLoader.Load(configFile);
            
            var cronService = new CronService(cronFile, async (job) => await agent.RunAsync(job.Payload.Message));
            await cronService.StartAsync();

            if (config.Channels.TryGetValue("telegram", out var tg) && tg.Enabled && !string.IsNullOrEmpty(tg.Token)) {
                var tgChannel = new TelegramChannel(tg.Token, async (inbound) => {
                    var response = await agent.RunAsync(inbound.Content);
                    return new OutboundMessage(inbound.Channel, inbound.ChatId, response);
                });
                await tgChannel.StartAsync();
                Console.WriteLine("Gateway running (Telegram)...");
                await Task.Delay(-1);
            } else {
                Console.WriteLine("Telegram configuration missing. Gateway cannot start.");
            }
        });
        rootCommand.AddCommand(gatewayCommand);

        // --- Command: WebSocket Gateway ---
        var websocketCommand = new Command("websocket", "Start the lightweight WebSocket agent gateway");
        websocketCommand.SetHandler(async () => {
            var setup = SetupAgentContext();
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
            var setup = SetupAgentContext();
            await RunChatLoop(setup.Agent, setup.StreamingEnabled);
        });
        rootCommand.AddCommand(chatCommand);

        // --- Command: Agent (Single Message) ---
        var msgOption = new Option<string>("--message", "Message to send") { IsRequired = true };
        msgOption.AddAlias("-m");
        var agentCommand = new Command("agent", "Send a single message to the agent");
        agentCommand.AddOption(msgOption);
        agentCommand.SetHandler(async (message) => {
            var setup = SetupAgentContext();
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
