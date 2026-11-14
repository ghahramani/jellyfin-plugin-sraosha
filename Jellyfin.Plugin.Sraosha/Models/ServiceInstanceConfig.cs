namespace Jellyfin.Plugin.Sraosha.Models;

/// <summary>
/// Configuration for a remote service instance (Radarr or Sonarr).
/// </summary>
public class ServiceInstanceConfig
{
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;
}
