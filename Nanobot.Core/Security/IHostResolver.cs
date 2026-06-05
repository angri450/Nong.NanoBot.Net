using System.Net;

namespace Nanobot.Core.Security;

public interface IHostResolver
{
    Task<IPAddress[]> GetHostAddressesAsync(string host);
}
