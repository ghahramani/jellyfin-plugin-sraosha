using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Sraosha.Services;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Sraosha.ScheduledTasks;

/// <summary>
/// Scheduled task that runs Sraosha's stalled download check on a recurring schedule.
/// </summary>
public class SraoshaScheduledTask : IScheduledTask
{
    private readonly ISraoshaMonitorService _monitorService;
    private readonly ILogger<SraoshaScheduledTask> _logger;

    public SraoshaScheduledTask(ISraoshaMonitorService monitorService, ILogger<SraoshaScheduledTask> logger)
    {
        _monitorService = monitorService;
        _logger = logger;
    }

    public string Name => "Check Stalled Downloads (Sraosha)";

    public string Key => "SraoshaCheckStalled";

    public string Description => "Vigilantly inspects Jellyseerr for stalled requests and triggers re-downloads in Radarr and Sonarr.";

    public string Category => "Maintenance";

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[SraoshaScheduledTask] Scheduled execution triggered.");
        await _monitorService.RunCheckAsync(progress, cancellationToken).ConfigureAwait(false);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // Default: Run every 6 hours
        yield return new TaskTriggerInfo
        {
#if NET10_0_OR_GREATER
            Type = TaskTriggerInfoType.IntervalTrigger,
#else
            Type = TaskTriggerInfo.TriggerInterval,
#endif
            IntervalTicks = TimeSpan.FromHours(6).Ticks
        };
    }
}
