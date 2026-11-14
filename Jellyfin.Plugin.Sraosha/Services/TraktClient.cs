using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Sraosha.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Sraosha.Services;

public class TraktClient : ITraktClient
{
    private const string TraktBaseUrl = "https://api.trakt.tv/";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TraktClient> _logger;

    public TraktClient(IHttpClientFactory httpClientFactory, ILogger<TraktClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateClient(string clientId)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(TraktBaseUrl);
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("trakt-api-version", "2");
        client.DefaultRequestHeaders.Add("trakt-api-key", clientId);
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    public async Task<TraktWatchHistory?> GetWatchHistoryAsync(string clientId, string username, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(username))
        {
            _logger.LogWarning("[Trakt] Client ID or Username is not configured.");
            return null;
        }

        try
        {
            using var client = CreateClient(clientId);
            var history = new TraktWatchHistory();

            // Fetch watched movies
            var moviesResponse = await client.GetAsync($"users/{Uri.EscapeDataString(username)}/watched/movies", cancellationToken).ConfigureAwait(false);
            if (moviesResponse.IsSuccessStatusCode)
            {
                var content = await moviesResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var watchedMovies = JsonSerializer.Deserialize<List<TraktWatchedMovie>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (watchedMovies != null)
                {
                    foreach (var wm in watchedMovies)
                    {
                        if (wm.Movie?.Ids?.Tmdb.HasValue == true)
                        {
                            history.WatchedMovieTmdbIds.Add(wm.Movie.Ids.Tmdb.Value);
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("[Trakt] Failed to fetch watched movies: HTTP {StatusCode}", moviesResponse.StatusCode);
            }

            // Fetch watched shows
            var showsResponse = await client.GetAsync($"users/{Uri.EscapeDataString(username)}/watched/shows", cancellationToken).ConfigureAwait(false);
            if (showsResponse.IsSuccessStatusCode)
            {
                var content = await showsResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var watchedShows = JsonSerializer.Deserialize<List<TraktWatchedShow>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (watchedShows != null)
                {
                    foreach (var ws in watchedShows)
                    {
                        var tvdbId = ws.Show?.Ids?.Tvdb;
                        if (!tvdbId.HasValue || ws.Seasons == null)
                        {
                            continue;
                        }

                        foreach (var season in ws.Seasons)
                        {
                            if (season.Episodes == null)
                            {
                                continue;
                            }

                            foreach (var ep in season.Episodes)
                            {
                                history.WatchedEpisodeKeys.Add($"{tvdbId.Value}:{season.Number}:{ep.Number}");
                            }
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("[Trakt] Failed to fetch watched shows: HTTP {StatusCode}", showsResponse.StatusCode);
            }

            _logger.LogInformation("[Trakt] Loaded watch history: {MovieCount} movies, {EpCount} episodes",
                history.WatchedMovieTmdbIds.Count, history.WatchedEpisodeKeys.Count);

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Trakt] Failed to load watch history for user \"{User}\"", username);
            return null;
        }
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(string clientId, string username, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(username))
        {
            return (false, "Trakt Client ID and Username must not be empty.");
        }

        try
        {
            using var client = CreateClient(clientId);
            var response = await client.GetAsync($"users/{Uri.EscapeDataString(username)}/watched/movies", cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return (true, $"Successfully connected to Trakt (user: {username})!");
            }

            return (false, $"Trakt responded with HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }
}
