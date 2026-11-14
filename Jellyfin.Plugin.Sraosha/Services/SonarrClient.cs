using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Sraosha.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Sraosha.Services;

public class SonarrClient : ISonarrClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SonarrClient> _logger;

    public SonarrClient(IHttpClientFactory httpClientFactory, ILogger<SonarrClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateClient(ServiceInstanceConfig instance)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(instance.Url.TrimEnd('/') + "/api/v3/");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("X-Api-Key", instance.ApiKey);
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    public async Task<SonarrSeries?> FindSeriesByTvdbIdAsync(ServiceInstanceConfig instance, int tvdbId, CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateClient(instance);
            var response = await client.GetAsync($"series?tvdbId={tvdbId}", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var seriesList = JsonSerializer.Deserialize<List<SonarrSeries>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return seriesList != null && seriesList.Count > 0 ? seriesList[0] : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Sonarr:{Instance}] Failed to find series tvdbId={TvdbId}", instance.Name, tvdbId);
            return null;
        }
    }

    public async Task<bool> EnsureSeriesMonitoredAsync(ServiceInstanceConfig instance, SonarrSeries series, CancellationToken cancellationToken)
    {
        if (series.Monitored)
        {
            return true;
        }

        try
        {
            using var client = CreateClient(instance);
            series.Monitored = true;
            var json = JsonSerializer.Serialize(series);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"series/{series.Id}", httpContent, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Sonarr:{Instance}] Enabled monitoring for \"{Title}\"", instance.Name, series.Title);
                return true;
            }

            _logger.LogWarning("[Sonarr:{Instance}] Could not enable monitoring for \"{Title}\": HTTP {StatusCode}",
                instance.Name, series.Title, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Sonarr:{Instance}] Failed to enable monitoring for \"{Title}\"", instance.Name, series.Title);
            return false;
        }
    }

    public async Task<List<SonarrEpisode>> GetEpisodesForSeriesAsync(ServiceInstanceConfig instance, int seriesId, CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateClient(instance);
            var response = await client.GetAsync($"episode?seriesId={seriesId}", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new List<SonarrEpisode>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var episodes = JsonSerializer.Deserialize<List<SonarrEpisode>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return episodes ?? new List<SonarrEpisode>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Sonarr:{Instance}] Failed to get episodes for seriesId={SeriesId}", instance.Name, seriesId);
            return new List<SonarrEpisode>();
        }
    }

    public List<SonarrEpisode> GetMissingEpisodes(List<SonarrEpisode> episodes, List<int>? requestedSeasons)
    {
        var now = DateTime.UtcNow;
        var hasSeasonFilter = requestedSeasons != null && requestedSeasons.Count > 0;

        return episodes.Where(ep =>
        {
            if (!ep.Monitored)
            {
                return false;
            }

            if (ep.HasFile)
            {
                return false;
            }

            // Skip future unaired episodes
            if (!string.IsNullOrEmpty(ep.AirDateUtc) && DateTime.TryParse(ep.AirDateUtc, out var airDate))
            {
                if (airDate.ToUniversalTime() > now)
                {
                    return false;
                }
            }

            // Season filter if requested in Jellyseerr
            if (hasSeasonFilter && !requestedSeasons!.Contains(ep.SeasonNumber))
            {
                return false;
            }

            return true;
        }).ToList();
    }

    public async Task<bool> SearchEpisodesAsync(
        ServiceInstanceConfig instance,
        List<int> episodeIds,
        string seriesTitle,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        if (episodeIds.Count == 0)
        {
            return true;
        }

        if (dryRun)
        {
            _logger.LogInformation("[DRY RUN] [Sonarr:{Instance}] Would search {Count} missing episodes for \"{Title}\"",
                instance.Name, episodeIds.Count, seriesTitle);
            return true;
        }

        // Batch in groups of 100 to avoid overly large payloads
        var batches = episodeIds.Chunk(100);
        bool allOk = true;

        using var client = CreateClient(instance);

        foreach (var batch in batches)
        {
            try
            {
                var cmd = new SonarrCommandRequest
                {
                    Name = "EpisodeSearch",
                    EpisodeIds = batch.ToList()
                };

                var json = JsonSerializer.Serialize(cmd);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("command", httpContent, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[Sonarr:{Instance}] Failed to search episode batch for \"{Title}\": HTTP {StatusCode}",
                        instance.Name, seriesTitle, response.StatusCode);
                    allOk = false;
                    continue;
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var result = JsonSerializer.Deserialize<SonarrCommandResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("[Sonarr:{Instance}] Triggered search for {Count} episodes of \"{Title}\" -> command #{CmdId}",
                    instance.Name, batch.Length, seriesTitle, result?.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Sonarr:{Instance}] Error searching episode batch for \"{Title}\"", instance.Name, seriesTitle);
                allOk = false;
            }
        }

        return allOk;
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(ServiceInstanceConfig instance, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(instance.Url) || string.IsNullOrWhiteSpace(instance.ApiKey))
        {
            return (false, "URL and API Key must not be empty.");
        }

        try
        {
            using var client = CreateClient(instance);
            var response = await client.GetAsync("system/status", cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return (true, $"Successfully connected to Sonarr ({instance.Name})!");
            }

            return (false, $"Sonarr responded with HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }
}
