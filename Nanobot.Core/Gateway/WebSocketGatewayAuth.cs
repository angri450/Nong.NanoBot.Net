using System.Security.Cryptography;
using System.Text;

namespace Nanobot.Core.Gateway;

public static class WebSocketGatewayAuth
{
    public static bool IsAuthorized(
        string? configuredToken,
        string? authorizationHeader,
        string? queryToken)
    {
        if (string.IsNullOrWhiteSpace(configuredToken))
        {
            return true;
        }

        var presentedToken = ExtractBearerToken(authorizationHeader);
        if (string.IsNullOrWhiteSpace(presentedToken))
        {
            presentedToken = queryToken;
        }

        return FixedTimeEquals(configuredToken, presentedToken);
    }

    private static string? ExtractBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return null;
        }

        const string bearerPrefix = "Bearer ";
        return authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader[bearerPrefix.Length..].Trim()
            : null;
    }

    private static bool FixedTimeEquals(string expected, string? actual)
    {
        if (string.IsNullOrWhiteSpace(actual))
        {
            return false;
        }

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
