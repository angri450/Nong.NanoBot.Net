using System.Text.Json.Nodes;

namespace Nanobot.Core.Tools.Builtin;

public class StockTool : ITool
{
    private readonly HttpClient _httpClient;
    private readonly Uri _baseUri;

    public StockTool(HttpClient? httpClient = null, string? baseUrl = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _baseUri = new Uri(baseUrl ?? "https://stooq.com/q/l/");
    }

    public string Name => "get_stock_price";
    public string Description => "获取股票报价（无需 API Key）。默认使用 Stooq CSV API，支持美股符号如 AAPL、MSFT，也支持传入带交易所后缀的 Stooq 符号。";

    public JsonNode Parameters => JsonNode.Parse("""
    {
        "type": "object",
        "properties": {
            "symbol": { "type": "string", "description": "股票代码，例如 'AAPL' 或 'TSLA'。对于 A 股请包含市场代码，如 '600519:SHA'。" }
        },
        "required": ["symbol"]
    }
    """)!;

    public async Task<string> ExecuteAsync(JsonNode? arguments)
    {
        var symbol = arguments?["symbol"]?.ToString();
        if (string.IsNullOrEmpty(symbol)) return "错误：必须提供股票代码 (symbol)";

        try
        {
            var normalizedSymbol = NormalizeSymbol(symbol);
            var url = new Uri(_baseUri, $"?s={Uri.EscapeDataString(normalizedSymbol)}&f=sd2t2ohlcv&h&e=csv");
            var csv = await _httpClient.GetStringAsync(url);
            var quote = ParseQuote(csv);
            if (quote is null)
            {
                return $"未能获取股票 {symbol} 的报价。提示：请确认代码或使用 Stooq 符号格式，例如 AAPL.US。";
            }

            return $@"股票: {quote.Symbol.ToUpperInvariant()}
日期: {quote.Date} {quote.Time}
开盘: {quote.Open}
最高: {quote.High}
最低: {quote.Low}
收盘/最新: {quote.Close}
成交量: {quote.Volume}";
        }
        catch (Exception ex)
        {
            return $"获取数据时发生异常: {ex.Message}";
        }
    }

    private static string NormalizeSymbol(string symbol)
    {
        var trimmed = symbol.Trim().ToLowerInvariant();
        if (trimmed.Contains('.') || trimmed.Contains(':'))
        {
            return trimmed.Replace(':', '.');
        }

        return trimmed + ".us";
    }

    private static StockQuote? ParseQuote(string csv)
    {
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
        {
            return null;
        }

        var values = SplitCsvLine(lines[1]);
        if (values.Length < 8 || values.Any(value => value.Equals("N/D", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        return new StockQuote(
            values[0],
            values[1],
            values[2],
            values[3],
            values[4],
            values[5],
            values[6],
            values[7]
        );
    }

    private static string[] SplitCsvLine(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;
        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        values.Add(current.ToString());
        return values.ToArray();
    }

    private sealed record StockQuote(
        string Symbol,
        string Date,
        string Time,
        string Open,
        string High,
        string Low,
        string Close,
        string Volume
    );
}
