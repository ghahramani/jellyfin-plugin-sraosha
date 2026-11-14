using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Sraosha.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Sraosha.Services;

public class RadarrClient : IRadarrClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RadarrClient> _logger;

    public RadarrClient(IHttpClientFactory httpClientFactory, ILogger<RadarrClient> logger)
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

    public async Task<RadarrMovie?> FindMovieByTmdbIdAsync(ServiceInstanceConfig instance, int tmdbId, CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateClient(instance);
            var response = await client.GetAsync($"movie?tmdbId={tmdbId}", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var movies = JsonSerializer.Deserialize<List<RadarrMovie>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return movies != null && movies.Count > 0 ? movies[0] : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Radarr:{Instance}] Failed to find movie tmdbId={TmdbId}", instance.Name, tmdbId);
            return null;
        }
    }

    public async Task<bool> EnsureMonitoredAsync(ServiceInstanceConfig instance, RadarrMovie movie, CancellationToken cancellationToken)
    {
        if (movie.Monitored)
        {
            return true;
        }

        try
        {
            using var client = CreateClient(instance);
            movie.Monitored = true;
            var json = JsonSerializer.Serialize(movie);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"movie/{movie.Id}", httpContent, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Radarr:{Instance}] Enabled monitoring for \"{Title}\"", instance.Name, movie.Title);
                return true;
            }

            _logger.LogWarning("[Radarr:{Instance}] Could not enable monitoring for \"{Title}\": HTTP {StatusCode}",
                instance.Name, movie.Title, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Radarr:{Instance}] Failed to enable monitoring for \"{Title}\"", instance.Name, movie.Title);
            return false;
        }
    }

    public async Task<bool> SearchMovieAsync(ServiceInstanceConfig instance, int movieId, string movieTitle, bool dryRun, CancellationToken cancellationToken)
    {
        if (dryRun)
        {
            _logger.LogInformation("[DRY RUN] [Radarr:{Instance}] Would trigger search for \"{Title}\" (id={MovieId})",
                instance.Name, movieTitle, movieId);
            return true;
        }

        try
        {
            using var client = CreateClient(instance);
            var cmd = new RadarrCommandRequest
            {
                Name = "MoviesSearch",
                MovieIds = new List<int> { movieId }
            };

            var json = JsonSerializer.Serialize(cmd);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("command", httpContent, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[Radarr:{Instance}] Failed to trigger search for \"{Title}\": HTTP {StatusCode}",
                    instance.Name, movieTitle, response.StatusCode);
                return false;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<RadarrCommandResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation("[Radarr:{Instance}] Triggered search for \"{Title}\" (id={MovieId}) -> command #{CmdId}",
                instance.Name, movieTitle, movieId, result?.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Radarr:{Instance}] Error triggering search for \"{Title}\"", instance.Name, movieTitle);
            return false;
        }
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
                return (true, $"Successfully connected to Radarr ({instance.Name})!");
            }

            return (false, $"Radarr responded with HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }
}
