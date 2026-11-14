using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Sraosha.Models;

namespace Jellyfin.Plugin.Sraosha.Services;

public interface INotifierService
{
    Task SendNotificationAsync(string webhookUrl, CheckRunSummary summary, CancellationToken cancellationToken);
}
