using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Sraosha.Models;

namespace Jellyfin.Plugin.Sraosha.Services;

public interface IRadarrClient
{
    Task<RadarrMovie?> FindMovieByTmdbIdAsync(ServiceInstanceConfig instance, int tmdbId, CancellationToken cancellationToken);

    Task<bool> EnsureMonitoredAsync(ServiceInstanceConfig instance, RadarrMovie movie, CancellationToken cancellationToken);

    Task<bool> SearchMovieAsync(ServiceInstanceConfig instance, int movieId, string movieTitle, bool dryRun, CancellationToken cancellationToken);

    Task<(bool Success, string Message)> TestConnectionAsync(ServiceInstanceConfig instance, CancellationToken cancellationToken);
}
