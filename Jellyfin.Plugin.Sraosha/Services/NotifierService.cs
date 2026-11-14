using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Sraosha.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Sraosha.Services;

public class NotifierService : INotifierService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NotifierService> _logger;

    public NotifierService(IHttpClientFactory httpClientFactory, ILogger<NotifierService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendNotificationAsync(string webhookUrl, CheckRunSummary summary, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            return;
        }

        try
        {
            var totalTriggered = summary.MoviesTriggered + summary.ShowsTriggered;
            if (totalTriggered == 0)
            {
                return;
            }

            var client = _httpClientFactory.CreateClient();
            var title = $"🛡️ Sraosha: Re-triggered {summary.MoviesTriggered} movies & {summary.EpisodesTriggered} episodes";
            var details = string.Join("\n", summary.TriggeredItems.Take(12));
            if (summary.TriggeredItems.Count > 12)
            {
                details += $"\n*...and {summary.TriggeredItems.Count - 12} more*";
            }

            string json;
            if (webhookUrl.Contains("discord.com", StringComparison.OrdinalIgnoreCase))
            {
                var discordPayload = new
                {
                    embeds = new[]
                    {
                        new
                        {
                            title = title,
                            description = details,
                            color = summary.IsDryRun ? 0xFEE75C : 0x57F287,
                            footer = new
                            {
                                text = $"Sraosha Sentinel • {(summary.IsDryRun ? "DRY RUN • " : "")}{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"
                            }
                        }
                    }
                };
                json = JsonSerializer.Serialize(discordPayload);
            }
            else
            {
                var genericPayload = new
                {
                    title = title,
                    text = details,
                    body = details,
                    timestamp = DateTime.UtcNow,
                    dryRun = summary.IsDryRun,
                    moviesTriggered = summary.MoviesTriggered,
                    episodesTriggered = summary.EpisodesTriggered
                };
                json = JsonSerializer.Serialize(genericPayload);
            }

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(webhookUrl, content, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Notifier] Webhook responded with HTTP {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Notifier] Failed to send webhook notification");
        }
    }
}
