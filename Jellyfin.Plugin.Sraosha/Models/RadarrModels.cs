using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Sraosha.Models;

public class RadarrMovie
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("tmdbId")]
    public int TmdbId { get; set; }

    [JsonPropertyName("hasFile")]
    public bool HasFile { get; set; }

    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("qualityProfileId")]
    public int QualityProfileId { get; set; }
}

public class RadarrCommandRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "MoviesSearch";

    [JsonPropertyName("movieIds")]
    public List<int> MovieIds { get; set; } = new();
}

public class RadarrCommandResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
