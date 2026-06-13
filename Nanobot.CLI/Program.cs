using System.CommandLine;
using System.Diagnostics;
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
using Nanobot.Core.Skills;

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
                var nongTool = new NongTool(workspace, config.Tools.Nong);
                registry.Register(nongTool);
                // Only register individual command tools if explicitly enabled (--detailed-tools)
                // Otherwise, run_nong handles everything via its args array.
                if (config.Tools.Nong.DetailedTools)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var tools = await NongTool.DiscoverOpenAiToolsAsync(
                                config.Tools.Nong.Command,
                                workspace: workspace);
                            foreach (var t in tools)
                            {
                                registry.Register(new NongDiscoveredToolWrapper(
                                    nongTool, t.Name, t.Args.ToArray(), t.Description, t.Parameters));
                            }
                            Console.Error.WriteLine($"[nong] Discovered {tools.Count} command tools");
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[nong] Command discovery skipped: {ex.Message}");
                        }
                    });
                }
            }

            // Skill tools (2-phase progressive disclosure)
            var skillLoader = new SkillLoader();
            registry.Register(new GetSkillCatalogTool(skillLoader, workspace));
            registry.Register(new LoadSkillTool(skillLoader, workspace));
            registry.Register(new LoadSkillReferenceTool(skillLoader, workspace));

            // Plugin manager (install skills from Nong.Toolkit.Net releases)
            var pluginManager = new PluginManager(workspace);
            registry.Register(new PluginInstallTool(pluginManager));
            registry.Register(new PluginListTool(pluginManager));
            
            string? githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? (config.Providers.TryGetValue("github", out var gh) ? gh.ApiKey : null);
            registry.Register(new GitHubTool(githubToken));

            var memory = new FileMemoryStore(workspace);
            registry.Register(new MemoryTool(memory));
            await RegisterMcpToolsAsync(registry, config);
            var eventBus = new RuntimeEventBus();
            var nongConfirmHook = new NongConfirmationHook();
            return (new Agent(provider, registry, memory, eventBus, hooks: new IAgentHook[] { nongConfirmHook }), eventBus, config, providerSetup.StreamingEnabled, memory, provider);
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
        var onboardCommand = new Command("onboard", "Initialize configuration, models, secrets, and workspace");
        onboardCommand.SetHandler(() => {
            if (!Directory.Exists(nanoDir)) Directory.CreateDirectory(nanoDir);

            var modelsFile = Path.Combine(nanoDir, "models.json");
            if (!File.Exists(modelsFile))
            {
                File.WriteAllText(modelsFile, """
                {
                  "providers": {
                    "dmx": {
                      "name": "DMX API",
                      "apiBase": "https://www.dmxapi.cn/v1/",
                      "defaultModel": "deepseek-v4-pro-guan",
                      "models": [
                        {
                          "id": "deepseek-v4-pro-guan",
                          "apiModelId": "deepseek-v4-pro-guan",
                          "displayName": "DeepSeek V4 Pro",
                          "contextWindow": 1000000,
                          "maxOutputTokens": 32000,
                          "supportsStreaming": true,
                          "supportsTools": true,
                          "supportsReasoning": true,
                          "supportsInterleavedThinking": true,
                          "reasoningEffort": "high"
                        }
                      ]
                    }
                  }
                }
                """);
                Console.WriteLine($"Created models catalog at {modelsFile}");
            }

            var secretsFile = Path.Combine(nanoDir, "secrets.json");
            if (!File.Exists(secretsFile))
            {
                File.WriteAllText(secretsFile, """
                {
                  "dmx": {
                    "apiKey": ""
                  }
                }
                """);
                Console.WriteLine($"Created secrets file at {secretsFile}");
                Console.WriteLine("  Fill in your API key: edit ~/.nanobot/secrets.json");
            }

            if (!File.Exists(configFile))
            {
                File.WriteAllText(configFile, """
                {
                  "agents": {
                    "defaults": {
                      "model": "dmx::deepseek-v4-pro-guan",
                      "fallbackModels": [
                        "dmx::deepseek-v4-pro-guan"
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
                        "pdf",
                        "lit",
                        "slice",
                        "progress"
                      ]
                    }
                  }
                }
                """);
                Console.WriteLine($"Created runtime config at {configFile}");
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

        // --- Command: WebUI ---
        var webUrlsOption = new Option<string>(
            name: "--urls",
            getDefaultValue: () => "http://127.0.0.1:8788",
            description: "URL prefix for the local WebUI host");
        webUrlsOption.AddAlias("-u");
        var noOpenOption = new Option<bool>("--no-open", "Start the WebUI server without opening a browser");

        var webCommand = new Command("web", "Start the local WebUI and open the default browser");
        webCommand.AddOption(webUrlsOption);
        webCommand.AddOption(noOpenOption);
        webCommand.SetHandler(async (urls, noOpen) =>
        {
            Environment.ExitCode = await StartWebUiAsync(urls, openBrowser: !noOpen);
        }, webUrlsOption, noOpenOption);
        rootCommand.AddCommand(webCommand);

        var serveCommand = new Command("serve", "Start the local WebUI server without opening a browser");
        serveCommand.AddOption(webUrlsOption);
        serveCommand.SetHandler(async urls =>
        {
            Environment.ExitCode = await StartWebUiAsync(urls, openBrowser: false);
        }, webUrlsOption);
        rootCommand.AddCommand(serveCommand);

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

        var exitCode = await rootCommand.InvokeAsync(args);
        return Environment.ExitCode != 0 ? Environment.ExitCode : exitCode;
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

    static async Task<int> StartWebUiAsync(string urls, bool openBrowser)
    {
        var launch = ResolveWebHostLaunch();
        if (launch is null)
        {
            Console.WriteLine("Nanobot.Web runtime was not found. Publish the WebUI or run from the repository root.");
            return 1;
        }

        var browserUrl = ResolveBrowserUrl(urls);
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = launch.FileName,
            WorkingDirectory = launch.WorkingDirectory,
            UseShellExecute = false
        };

        foreach (var argument in launch.Arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.StartInfo.ArgumentList.Add("--urls");
        process.StartInfo.ArgumentList.Add(urls);

        ConsoleCancelEventHandler? cancelHandler = null;
        cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        };

        Console.CancelKeyPress += cancelHandler;
        try
        {
            if (!process.Start())
            {
                Console.WriteLine("Failed to start Nanobot.Web runtime.");
                return 1;
            }

            Console.WriteLine($"NanoBot WebUI listening on {browserUrl}");
            Console.WriteLine("Press Ctrl+C to stop the local runtime.");

            if (openBrowser)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1500);
                    OpenBrowser(browserUrl);
                });
            }

            await process.WaitForExitAsync();
            return process.ExitCode;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }

    static WebHostLaunch? ResolveWebHostLaunch()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var publishedExe = Path.Combine(baseDirectory, "web", "Nanobot.Web.exe");
        if (File.Exists(publishedExe))
        {
            return new WebHostLaunch(publishedExe, Array.Empty<string>(), Path.GetDirectoryName(publishedExe)!);
        }

        var publishedDll = Path.Combine(baseDirectory, "web", "Nanobot.Web.dll");
        if (File.Exists(publishedDll))
        {
            return new WebHostLaunch("dotnet", new[] { publishedDll }, Path.GetDirectoryName(publishedDll)!);
        }

        var repositoryRoot = FindRepositoryRoot(baseDirectory);
        if (repositoryRoot is null)
        {
            return null;
        }

        var webProject = Path.Combine(repositoryRoot, "Nanobot.Web", "Nanobot.Web.csproj");
        return File.Exists(webProject)
            ? new WebHostLaunch("dotnet", new[] { "run", "--project", webProject, "--" }, repositoryRoot)
            : null;
    }

    static string? FindRepositoryRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Nanobot.slnx"))
                && Directory.Exists(Path.Combine(directory.FullName, "Nanobot.Web")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    static string ResolveBrowserUrl(string urls)
    {
        var firstUrl = urls
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(url => url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            ?? "http://127.0.0.1:8788";

        if (!Uri.TryCreate(firstUrl, UriKind.Absolute, out var uri))
        {
            return firstUrl;
        }

        if (uri.Host is "*" or "+" or "0.0.0.0")
        {
            var builder = new UriBuilder(uri)
            {
                Host = "127.0.0.1"
            };
            return builder.Uri.ToString();
        }

        return firstUrl;
    }

    static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Open browser failed: {ex.Message}");
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

    private sealed record WebHostLaunch(string FileName, IReadOnlyList<string> Arguments, string WorkingDirectory);
}
