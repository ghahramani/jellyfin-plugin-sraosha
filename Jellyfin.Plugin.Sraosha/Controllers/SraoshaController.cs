using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Sraosha.Configuration;
using Jellyfin.Plugin.Sraosha.Models;
using Jellyfin.Plugin.Sraosha.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Sraosha.Controllers;

/// <summary>
/// API Controller for Sraosha test connections, manual triggers, and status checks.
/// </summary>
[ApiController]
[Route("Sraosha")]
[Authorize]
public class SraoshaController : ControllerBase
{
    private readonly ISraoshaMonitorService _monitorService;
    private readonly IJellyseerrClient _jellyseerrClient;
    private readonly IRadarrClient _radarrClient;
    private readonly ISonarrClient _sonarrClient;
    private readonly ITraktClient _traktClient;
    private readonly ILogger<SraoshaController> _logger;

    public SraoshaController(
        ISraoshaMonitorService monitorService,
        IJellyseerrClient jellyseerrClient,
        IRadarrClient radarrClient,
        ISonarrClient sonarrClient,
        ITraktClient traktClient,
        ILogger<SraoshaController> logger)
    {
        _monitorService = monitorService;
        _jellyseerrClient = jellyseerrClient;
        _radarrClient = radarrClient;
        _sonarrClient = sonarrClient;
        _traktClient = traktClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets the summary of the last check run.
    /// </summary>
    [HttpGet("LastRun")]
    public ActionResult<CheckRunSummary?> GetLastRun()
    {
        return Ok(Plugin.Instance?.LastRun);
    }

    /// <summary>
    /// Manually triggers a scan for stalled requests.
    /// </summary>
    [HttpPost("RunCheck")]
    public async Task<ActionResult<CheckRunSummary>> RunCheck(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[SraoshaController] Manual scan initiated from Dashboard API");
        var summary = await _monitorService.RunCheckAsync(null, cancellationToken).ConfigureAwait(false);
        return Ok(summary);
    }

    /// <summary>
    /// Tests connection to Jellyseerr.
    /// </summary>
    [HttpPost("TestJellyseerr")]
    public async Task<ActionResult<object>> TestJellyseerr([FromQuery] string? url, [FromQuery] string? apiKey, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var targetUrl = !string.IsNullOrWhiteSpace(url) ? url : config.JellyseerrUrl;
        var targetKey = !string.IsNullOrWhiteSpace(apiKey) ? apiKey : config.JellyseerrApiKey;

        var (success, message) = await _jellyseerrClient.TestConnectionAsync(targetUrl, targetKey, cancellationToken).ConfigureAwait(false);
        return Ok(new { success, message });
    }

    /// <summary>
    /// Tests connection to a Radarr instance.
    /// </summary>
    [HttpPost("TestRadarr")]
    public async Task<ActionResult<object>> TestRadarr([FromQuery] string url, [FromQuery] string apiKey, [FromQuery] string? name, CancellationToken cancellationToken)
    {
        var instance = new ServiceInstanceConfig
        {
            Name = name ?? "Radarr",
            Url = url,
            ApiKey = apiKey
        };

        var (success, message) = await _radarrClient.TestConnectionAsync(instance, cancellationToken).ConfigureAwait(false);
        return Ok(new { success, message });
    }

    /// <summary>
    /// Tests connection to a Sonarr instance.
    /// </summary>
    [HttpPost("TestSonarr")]
    public async Task<ActionResult<object>> TestSonarr([FromQuery] string url, [FromQuery] string apiKey, [FromQuery] string? name, CancellationToken cancellationToken)
    {
        var instance = new ServiceInstanceConfig
        {
            Name = name ?? "Sonarr",
            Url = url,
            ApiKey = apiKey
        };

        var (success, message) = await _sonarrClient.TestConnectionAsync(instance, cancellationToken).ConfigureAwait(false);
        return Ok(new { success, message });
    }

    /// <summary>
    /// Tests connection to Trakt.
    /// </summary>
    [HttpPost("TestTrakt")]
    public async Task<ActionResult<object>> TestTrakt([FromQuery] string clientId, [FromQuery] string username, CancellationToken cancellationToken)
    {
        var (success, message) = await _traktClient.TestConnectionAsync(clientId, username, cancellationToken).ConfigureAwait(false);
        return Ok(new { success, message });
    }
}
