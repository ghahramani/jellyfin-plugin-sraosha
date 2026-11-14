using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Sraosha.Configuration;
using Jellyfin.Plugin.Sraosha.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Sraosha.Services;

public class SraoshaMonitorService : ISraoshaMonitorService
{
    private readonly IJellyseerrClient _jellyseerrClient;
    private readonly IRadarrClient _radarrClient;
    private readonly ISonarrClient _sonarrClient;
    private readonly ITraktClient _traktClient;
    private readonly INotifierService _notifierService;
    private readonly ILogger<SraoshaMonitorService> _logger;

    public SraoshaMonitorService(
        IJellyseerrClient jellyseerrClient,
        IRadarrClient radarrClient,
        ISonarrClient sonarrClient,
        ITraktClient traktClient,
        INotifierService notifierService,
        ILogger<SraoshaMonitorService> logger)
    {
        _jellyseerrClient = jellyseerrClient;
        _radarrClient = radarrClient;
        _sonarrClient = sonarrClient;
        _traktClient = traktClient;
        _notifierService = notifierService;
        _logger = logger;
    }

    public async Task<CheckRunSummary> RunCheckAsync(IProgress<double>? progress, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var summary = new CheckRunSummary
        {
            Timestamp = DateTime.UtcNow,
            IsDryRun = config.DryRun
        };

        _logger.LogInformation("=================================================");
        _logger.LogInformation("🛡️ Sraosha Sentinel started scan (DryRun: {DryRun})", config.DryRun);
        _logger.LogInformation("=================================================");

        progress?.Report(5.0);

        try
        {
            // 1. Fetch Trakt watch history if configured
            TraktWatchHistory? watchHistory = null;
            if (config.EnableTrakt && config.TraktSkipWatched)
            {
                _logger.LogInformation("[Sraosha] Loading Trakt watch history for user \"{User}\"...", config.TraktUsername);
                watchHistory = await _traktClient.GetWatchHistoryAsync(config.TraktClientId, config.TraktUsername, cancellationToken).ConfigureAwait(false);
            }

            progress?.Report(15.0);

            // 2. Fetch stalled requests from Jellyseerr
            _logger.LogInformation("[Sraosha] Querying Jellyseerr at {Url} (threshold: {Hours}h)...", config.JellyseerrUrl, config.StaleAfterHours);
            var stalledRequests = await _jellyseerrClient.GetStalledRequestsAsync(
                config.JellyseerrUrl,
                config.JellyseerrApiKey,
                config.StaleAfterHours,
                cancellationToken).ConfigureAwait(false);

            summary.StalledRequestsCount = stalledRequests.Count;
            progress?.Report(30.0);

            if (stalledRequests.Count == 0)
            {
                _logger.LogInformation("[Sraosha] No stalled requests found. All clear!");
                progress?.Report(100.0);
                if (Plugin.Instance != null)
                {
                    Plugin.Instance.LastRun = summary;
                }
                return summary;
            }

            var movieRequests = stalledRequests.Where(r => string.Equals(r.Type, "movie", StringComparison.OrdinalIgnoreCase)).ToList();
            var tvRequests = stalledRequests.Where(r => string.Equals(r.Type, "tv", StringComparison.OrdinalIgnoreCase)).ToList();

            summary.MoviesChecked = movieRequests.Count;
            summary.ShowsChecked = tvRequests.Count;

            // 3. Process Movies
            var enabledRadarrs = config.RadarrInstances.Where(r => r.Enabled && !string.IsNullOrWhiteSpace(r.Url)).ToList();
            if (movieRequests.Count > 0)
            {
                _logger.LogInformation("[Sraosha] Checking {Count} movie request(s) across {RadarrCount} Radarr instance(s)...",
                    movieRequests.Count, enabledRadarrs.Count);

                int idx = 0;
                foreach (var req in movieRequests)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    idx++;
                    var currentProgress = 30.0 + (35.0 * idx / movieRequests.Count);
                    progress?.Report(currentProgress);

                    var tmdbId = req.Media?.TmdbId;
                    if (!tmdbId.HasValue)
                    {
                        _logger.LogWarning("[Movies] Request #{Id} has no tmdbId — skipping", req.Id);
                        summary.MoviesSkipped++;
                        continue;
                    }

                    // Trakt skip
                    if (watchHistory != null && config.TraktSkipWatched && watchHistory.WatchedMovieTmdbIds.Contains(tmdbId.Value))
                    {
                        _logger.LogInformation("[Movies] tmdbId={TmdbId} already watched — skipping (Trakt)", tmdbId.Value);
                        summary.MoviesSkipped++;
                        continue;
                    }

                    bool foundInAny = false;
                    foreach (var radarr in enabledRadarrs)
                    {
                        var movie = await _radarrClient.FindMovieByTmdbIdAsync(radarr, tmdbId.Value, cancellationToken).ConfigureAwait(false);
                        if (movie == null)
                        {
                            continue;
                        }

                        foundInAny = true;

                        if (movie.HasFile)
                        {
                            _logger.LogInformation("[Movies] \"{Title}\" already has a file on disk in {Radarr} — skipping", movie.Title, radarr.Name);
                            summary.MoviesSkipped++;
                            break;
                        }

                        // Ensure monitored
                        await _radarrClient.EnsureMonitoredAsync(radarr, movie, cancellationToken).ConfigureAwait(false);

                        // Trigger search
                        var ok = await _radarrClient.SearchMovieAsync(radarr, movie.Id, movie.Title, config.DryRun, cancellationToken).ConfigureAwait(false);
                        if (ok)
                        {
                            summary.MoviesTriggered++;
                            summary.TriggeredItems.Add($"🎬 [Movie] {movie.Title} ({radarr.Name})");
                        }
                        else
                        {
                            summary.MoviesErrors++;
                        }

                        break; // Handled in first matching instance
                    }

                    if (!foundInAny)
                    {
                        _logger.LogWarning("[Movies] tmdbId={TmdbId} not found in any enabled Radarr instance", tmdbId.Value);
                        summary.MoviesSkipped++;
                    }
                }
            }

            progress?.Report(65.0);

            // 4. Process TV Shows
            var enabledSonarrs = config.SonarrInstances.Where(s => s.Enabled && !string.IsNullOrWhiteSpace(s.Url)).ToList();
            if (tvRequests.Count > 0)
            {
                _logger.LogInformation("[Sraosha] Checking {Count} TV show request(s) across {SonarrCount} Sonarr instance(s)...",
                    tvRequests.Count, enabledSonarrs.Count);

                int idx = 0;
                foreach (var req in tvRequests)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    idx++;
                    var currentProgress = 65.0 + (30.0 * idx / tvRequests.Count);
                    progress?.Report(currentProgress);

                    var tvdbId = req.Media?.TvdbId;
                    if (!tvdbId.HasValue)
                    {
                        _logger.LogWarning("[Shows] Request #{Id} has no tvdbId — skipping", req.Id);
                        summary.ShowsSkipped++;
                        continue;
                    }

                    var requestedSeasons = req.Seasons?.Select(s => s.SeasonNumber).ToList();

                    bool foundInAny = false;
                    foreach (var sonarr in enabledSonarrs)
                    {
                        var series = await _sonarrClient.FindSeriesByTvdbIdAsync(sonarr, tvdbId.Value, cancellationToken).ConfigureAwait(false);
                        if (series == null)
                        {
                            continue;
                        }

                        foundInAny = true;

                        await _sonarrClient.EnsureSeriesMonitoredAsync(sonarr, series, cancellationToken).ConfigureAwait(false);
                        var episodes = await _sonarrClient.GetEpisodesForSeriesAsync(sonarr, series.Id, cancellationToken).ConfigureAwait(false);
                        var missing = _sonarrClient.GetMissingEpisodes(episodes, requestedSeasons);

                        // Filter out Trakt watched episodes
                        if (watchHistory != null && config.TraktSkipWatched)
                        {
                            var beforeCount = missing.Count;
                            missing = missing.Where(e =>
                            {
                                var key = $"{tvdbId.Value}:{e.SeasonNumber}:{e.EpisodeNumber}";
                                return !watchHistory.WatchedEpisodeKeys.Contains(key);
                            }).ToList();

                            var filteredOut = beforeCount - missing.Count;
                            if (filteredOut > 0)
                            {
                                _logger.LogInformation("[Shows] \"{Title}\" — skipped {Filtered} already-watched episodes (Trakt)",
                                    series.Title, filteredOut);
                            }
                        }

                        if (missing.Count == 0)
                        {
                            _logger.LogInformation("[Shows] \"{Title}\" — no missing un-watched episodes to re-search", series.Title);
                            summary.ShowsSkipped++;
                            break;
                        }

                        _logger.LogInformation("[Shows] \"{Title}\" — found {Count} missing episode(s) to search in {Sonarr}",
                            series.Title, missing.Count, sonarr.Name);

                        var episodeIds = missing.Select(e => e.Id).ToList();
                        var ok = await _sonarrClient.SearchEpisodesAsync(sonarr, episodeIds, series.Title, config.DryRun, cancellationToken).ConfigureAwait(false);

                        if (ok)
                        {
                            summary.ShowsTriggered++;
                            summary.EpisodesTriggered += missing.Count;
                            summary.TriggeredItems.Add($"📺 [Series] {series.Title} ({missing.Count} ep{(missing.Count > 1 ? "s" : "")}) ({sonarr.Name})");
                        }
                        else
                        {
                            summary.ShowsErrors++;
                        }

                        break; // Handled in first matching instance
                    }

                    if (!foundInAny)
                    {
                        _logger.LogWarning("[Shows] tvdbId={TvdbId} not found in any enabled Sonarr instance", tvdbId.Value);
                        summary.ShowsSkipped++;
                    }
                }
            }

            progress?.Report(95.0);

            // 5. Send Webhook notification if enabled
            if (config.WebhookEnabled && !string.IsNullOrWhiteSpace(config.WebhookUrl) && summary.TriggeredItems.Count > 0)
            {
                await _notifierService.SendNotificationAsync(config.WebhookUrl, summary, cancellationToken).ConfigureAwait(false);
            }

            progress?.Report(100.0);
            _logger.LogInformation("=================================================");
            _logger.LogInformation("🛡️ Sraosha Sentinel finished. Movies: {MTrig} trig, {MSkip} skip. Shows: {STrig} trig ({ETrig} eps), {SSkip} skip.",
                summary.MoviesTriggered, summary.MoviesSkipped, summary.ShowsTriggered, summary.EpisodesTriggered, summary.ShowsSkipped);
            _logger.LogInformation("=================================================");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🛡️ Sraosha Sentinel encountered an unhandled error during scan");
            summary.IsSuccess = false;
            summary.ErrorMessage = ex.Message;
        }

        if (Plugin.Instance != null)
        {
            Plugin.Instance.LastRun = summary;
        }

        return summary;
    }
}
