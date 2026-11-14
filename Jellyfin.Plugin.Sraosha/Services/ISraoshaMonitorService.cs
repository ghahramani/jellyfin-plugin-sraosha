using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Sraosha.Models;

namespace Jellyfin.Plugin.Sraosha.Services;

public interface ISraoshaMonitorService
{
    Task<CheckRunSummary> RunCheckAsync(IProgress<double>? progress, CancellationToken cancellationToken);
}
