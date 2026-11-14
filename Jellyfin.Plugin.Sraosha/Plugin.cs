using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Sraosha.Configuration;
using Jellyfin.Plugin.Sraosha.Models;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Sraosha;

/// <summary>
/// Sraosha: The Sleepless Sentinel for Jellyfin.
/// Monitors Jellyseerr requests and re-triggers missing or stalled downloads in Radarr &amp; Sonarr.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public static readonly Guid PluginGuid = Guid.Parse("593b0f99-5be9-448c-8a45-3b8412d9c132");

    public override string Name => "Sraosha";

    public override Guid Id => PluginGuid;

    public override string Description => "The sleepless sentinel for Jellyfin. Vigilantly guards your Jellyseerr requests and revives stalled downloads in Radarr & Sonarr.";

    public static Plugin? Instance { get; private set; }

    public ILogger<Plugin> Logger { get; }

    /// <summary>
    /// In-memory cache of the most recent scan summary.
    /// </summary>
    public CheckRunSummary? LastRun { get; set; }

    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        ILogger<Plugin> logger)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        Logger = logger;
    }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = "Sraosha",
            EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html",
            EnableInMainMenu = false
        };
    }
}
