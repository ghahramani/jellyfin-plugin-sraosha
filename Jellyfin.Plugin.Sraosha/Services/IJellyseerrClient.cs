using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Sraosha.Models;

namespace Jellyfin.Plugin.Sraosha.Services;

public interface IJellyseerrClient
{
    Task<List<JellyseerrRequest>> GetStalledRequestsAsync(string baseUrl, string apiKey, int staleAfterHours, CancellationToken cancellationToken);

    Task<(bool Success, string Message)> TestConnectionAsync(string baseUrl, string apiKey, CancellationToken cancellationToken);
}
