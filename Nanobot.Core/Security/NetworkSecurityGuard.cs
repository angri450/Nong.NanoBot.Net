using System.Net;
using System.Net.Sockets;

namespace Nanobot.Core.Security;

public class NetworkSecurityGuard
{
    private readonly IHostResolver _resolver;

    public NetworkSecurityGuard(IHostResolver? resolver = null)
    {
        _resolver = resolver ?? new SystemHostResolver();
    }

    public async Task ValidateHttpUrlAsync(Uri uri)
    {
        if (uri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("Only http and https URLs are allowed.");
        }

        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException("URL host is required.");
        }

        var addresses = IPAddress.TryParse(uri.Host, out var literalAddress)
            ? new[] { literalAddress }
            : await _resolver.GetHostAddressesAsync(uri.IdnHost);

        if (addresses.Length == 0)
        {
            throw new InvalidOperationException($"Host '{uri.Host}' did not resolve to any IP address.");
        }

        var blocked = addresses.FirstOrDefault(IsBlockedAddress);
        if (blocked is not null)
        {
            throw new InvalidOperationException($"Blocked SSRF target '{uri.Host}' resolved to restricted address {blocked}.");
        }
    }

    public static bool IsBlockedAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => IsBlockedIPv4(address.GetAddressBytes()),
            AddressFamily.InterNetworkV6 => IsBlockedIPv6(address.GetAddressBytes()),
            _ => true
        };
    }

    private static bool IsBlockedIPv4(byte[] bytes)
    {
        if (bytes.Length != 4)
        {
            return true;
        }

        return bytes[0] switch
        {
            0 => true,
            10 => true,
            100 when bytes[1] is >= 64 and <= 127 => true,
            127 => true,
            169 when bytes[1] == 254 => true,
            172 when bytes[1] is >= 16 and <= 31 => true,
            192 when bytes[1] == 168 => true,
            >= 224 => true,
            _ => bytes is [255, 255, 255, 255]
        };
    }

    private static bool IsBlockedIPv6(byte[] bytes)
    {
        if (bytes.Length != 16)
        {
            return true;
        }

        if (bytes.All(value => value == 0))
        {
            return true;
        }

        if (bytes[0] == 0xfc || bytes[0] == 0xfd)
        {
            return true;
        }

        if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80)
        {
            return true;
        }

        if (bytes[0] == 0xff)
        {
            return true;
        }

        return false;
    }
}
