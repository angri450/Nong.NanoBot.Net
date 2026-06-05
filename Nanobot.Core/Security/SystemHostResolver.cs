using System.Net;

namespace Nanobot.Core.Security;

public class SystemHostResolver : IHostResolver
{
    public Task<IPAddress[]> GetHostAddressesAsync(string host)
    {
        return Dns.GetHostAddressesAsync(host);
    }
}
