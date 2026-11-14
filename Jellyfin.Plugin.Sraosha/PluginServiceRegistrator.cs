using Jellyfin.Plugin.Sraosha.ScheduledTasks;
using Jellyfin.Plugin.Sraosha.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Sraosha;

/// <summary>
/// Registers Sraosha services and scheduled tasks into Jellyfin's dependency injection container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHttpClient();

        serviceCollection.AddSingleton<IJellyseerrClient, JellyseerrClient>();
        serviceCollection.AddSingleton<IRadarrClient, RadarrClient>();
        serviceCollection.AddSingleton<ISonarrClient, SonarrClient>();
        serviceCollection.AddSingleton<ITraktClient, TraktClient>();
        serviceCollection.AddSingleton<INotifierService, NotifierService>();
        serviceCollection.AddSingleton<ISraoshaMonitorService, SraoshaMonitorService>();

        // Register Scheduled Task
        serviceCollection.AddSingleton<IScheduledTask, SraoshaScheduledTask>();
    }
}
