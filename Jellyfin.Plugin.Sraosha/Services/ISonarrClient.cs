using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Sraosha.Models;

namespace Jellyfin.Plugin.Sraosha.Services;

public interface ISonarrClient
{
    Task<SonarrSeries?> FindSeriesByTvdbIdAsync(ServiceInstanceConfig instance, int tvdbId, CancellationToken cancellationToken);

    Task<bool> EnsureSeriesMonitoredAsync(ServiceInstanceConfig instance, SonarrSeries series, CancellationToken cancellationToken);

    Task<List<SonarrEpisode>> GetEpisodesForSeriesAsync(ServiceInstanceConfig instance, int seriesId, CancellationToken cancellationToken);

    List<SonarrEpisode> GetMissingEpisodes(List<SonarrEpisode> episodes, List<int>? requestedSeasons);

    Task<bool> SearchEpisodesAsync(ServiceInstanceConfig instance, List<int> episodeIds, string seriesTitle, bool dryRun, CancellationToken cancellationToken);

    Task<(bool Success, string Message)> TestConnectionAsync(ServiceInstanceConfig instance, CancellationToken cancellationToken);
}
