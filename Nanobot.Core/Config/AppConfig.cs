namespace Nanobot.Core.Config;

public class AppConfig
{
    public Dictionary<string, ProviderSettings> Providers { get; set; } = new();
    public AgentSettings Agents { get; set; } = new();
    public StreamingSettings Streaming { get; set; } = new();
    public GatewaySettings Gateway { get; set; } = new();
    public WebSearchSettings WebSearch { get; set; } = new();
    public Dictionary<string, ChannelSettings> Channels { get; set; } = new();
    public ToolSettings Tools { get; set; } = new();
}

public class ProviderSettings
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Kind { get; set; }
    public string? Type { get; set; }
    public bool Enabled { get; set; } = true;
    public string? ApiKey { get; set; }
    public string? ApiBase { get; set; }
    public string? BaseUrl { get; set; }
    public string? Endpoint { get; set; }
    public string? Deployment { get; set; }
    public string? ApiVersion { get; set; }
    public string? DefaultModel { get; set; }
    public List<ModelSettings> Models { get; set; } = new();
    public ProviderCapabilitySettings Capabilities { get; set; } = new();
    public Dictionary<string, string> Settings { get; set; } = new();
}

public class AgentSettings
{
    public DefaultAgentSettings Defaults { get; set; } = new();
}

public class DefaultAgentSettings
{
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public List<string> FallbackModels { get; set; } = new();
    public DreamSettings Dream { get; set; } = new();
}

public class DreamSettings
{
    public bool Enabled { get; set; } = true;
    public int IntervalHours { get; set; } = 6;
}

public class ModelSettings
{
    public string? Id { get; set; }
    public string? ApiModelId { get; set; }
    public bool Enabled { get; set; } = true;
    public bool? SupportsStreaming { get; set; }
    public bool? SupportsTools { get; set; }
}

public class ProviderCapabilitySettings
{
    public bool Chat { get; set; } = true;
    public bool Tools { get; set; } = true;
    public bool Streaming { get; set; } = true;
    public bool Images { get; set; }
}

public class StreamingSettings
{
    public bool Enabled { get; set; } = true;
}

public class GatewaySettings
{
    public WebSocketGatewaySettings WebSocket { get; set; } = new();
    public HeartbeatSettings Heartbeat { get; set; } = new();
}

public class WebSocketGatewaySettings
{
    public string? Prefix { get; set; }
    public string? Token { get; set; }
}

public class WebSearchSettings
{
    public string? ApiKey { get; set; }
}

public class HeartbeatSettings
{
    public bool Enabled { get; set; }
    public int IntervalSeconds { get; set; } = 1800;
}

public class ChannelSettings
{
    public bool Enabled { get; set; }
    public string? Token { get; set; }
    public string? BotToken { get; set; }
    public string? AppId { get; set; }
    public string? AppSecret { get; set; }
    public string? SigningSecret { get; set; }
    public string? Endpoint { get; set; }
    public List<string> AllowFrom { get; set; } = new();
    public Dictionary<string, string> Settings { get; set; } = new();
}

public class ToolSettings
{
    public Dictionary<string, Mcp.McpServerConfig> McpServers { get; set; } = new();
    public StockToolSettings Stock { get; set; } = new();
}

public class StockToolSettings
{
    public string? ApiKey { get; set; }
    public string? Provider { get; set; }
}
