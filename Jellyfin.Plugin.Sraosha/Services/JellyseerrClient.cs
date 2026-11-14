using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Sraosha.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Sraosha.Services;

public class JellyseerrClient : IJellyseerrClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<JellyseerrClient> _logger;

    public JellyseerrClient(IHttpClientFactory httpClientFactory, ILogger<JellyseerrClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateClient(string baseUrl, string apiKey)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/api/v1/");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    public async Task<List<JellyseerrRequest>> GetStalledRequestsAsync(
        string baseUrl,
        string apiKey,
        int staleAfterHours,
        CancellationToken cancellationToken)
    {
        var stalled = new List<JellyseerrRequest>();
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("[Jellyseerr] URL or API key is not configured.");
            return stalled;
        }

        using var client = CreateClient(baseUrl, apiKey);
        var now = DateTime.UtcNow;
        var staleCutoff = TimeSpan.FromHours(staleAfterHours);
        int page = 1;
        int totalPages = 1;

        while (page <= totalPages && !cancellationToken.IsCancellationRequested)
        {
            var url = $"request?filter=processing&take=100&skip={(page - 1) * 100}";
            try
            {
                var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var data = JsonSerializer.Deserialize<JellyseerrPagedResponse<JellyseerrRequest>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data == null || data.Results == null)
                {
                    break;
                }

                totalPages = data.PageInfo.Pages > 0 ? data.PageInfo.Pages : 1;

                foreach (var req in data.Results)
                {
                    if (req.Media == null)
                    {
                        continue;
                    }

                    // Skip if media is already marked as available
                    if (req.Media.Status == MediaStatus.Available)
                    {
                        continue;
                    }

                    // Check age
                    if (DateTime.TryParse(req.UpdatedAt ?? req.CreatedAt, out var itemDate))
                    {
                        var age = now - itemDate.ToUniversalTime();
                        if (age < staleCutoff)
                        {
                            _logger.LogDebug("[Jellyseerr] Skipping request #{Id} ({Type}) — only {AgeMinutes}m old",
                                req.Id, req.Type, Math.Round(age.TotalMinutes));
                            continue;
                        }
                    }

                    stalled.Add(req);
                }

                page++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Jellyseerr] Error querying page {Page} from Jellyseerr", page);
                break;
            }
        }

        _logger.LogInformation("[Jellyseerr] Found {Count} stalled requests", stalled.Count);
        return stalled;
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(string baseUrl, string apiKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            return (false, "Jellyseerr URL and API Key must not be empty.");
        }

        try
        {
            using var client = CreateClient(baseUrl, apiKey);
            var response = await client.GetAsync("status", cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return (true, "Successfully connected to Jellyseerr!");
            }

            return (false, $"Jellyseerr responded with HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }
}
