using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Sraosha.Models;

namespace Jellyfin.Plugin.Sraosha.Services;

public interface ITraktClient
{
    Task<TraktWatchHistory?> GetWatchHistoryAsync(string clientId, string username, CancellationToken cancellationToken);

    Task<(bool Success, string Message)> TestConnectionAsync(string clientId, string username, CancellationToken cancellationToken);
}
