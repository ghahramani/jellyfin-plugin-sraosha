using System.Collections.Generic;
using Jellyfin.Plugin.Sraosha.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Sraosha.Configuration;

/// <summary>
/// Plugin configuration for Sraosha.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the Jellyseerr base URL (e.g. http://192.168.1.10:5055).
    /// </summary>
    public string JellyseerrUrl { get; set; } = "http://localhost:5055";

    /// <summary>
    /// Gets or sets the Jellyseerr API Key.
    /// </summary>
    public string JellyseerrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hours threshold after which a processing request is considered stalled.
    /// </summary>
    public int StaleAfterHours { get; set; } = 2;

    /// <summary>
    /// Gets or sets a value indicating whether dry-run mode is enabled (log actions without triggering searches).
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// Gets or sets configured Radarr instances.
    /// </summary>
    public List<ServiceInstanceConfig> RadarrInstances { get; set; } = new()
    {
        new ServiceInstanceConfig
        {
            Name = "Default Radarr",
            Url = "http://localhost:7878",
            ApiKey = string.Empty,
            Enabled = true
        }
    };

    /// <summary>
    /// Gets or sets configured Sonarr instances.
    /// </summary>
    public List<ServiceInstanceConfig> SonarrInstances { get; set; } = new()
    {
        new ServiceInstanceConfig
        {
            Name = "Default Sonarr",
            Url = "http://localhost:8989",
            ApiKey = string.Empty,
            Enabled = true
        }
    };

    /// <summary>
    /// Gets or sets a value indicating whether Trakt integration is enabled.
    /// </summary>
    public bool EnableTrakt { get; set; } = false;

    /// <summary>
    /// Gets or sets the Trakt Client ID (API Application Key).
    /// </summary>
    public string TraktClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Trakt username.
    /// </summary>
    public string TraktUsername { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to skip re-downloading media already marked as watched in Trakt.
    /// </summary>
    public bool TraktSkipWatched { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Webhook notifications are enabled.
    /// </summary>
    public bool WebhookEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the Webhook URL (e.g. Discord, ntfy, Slack).
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;
}
