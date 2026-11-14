using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Sraosha.Models;

public class TraktIds
{
    [JsonPropertyName("trakt")]
    public int? Trakt { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("imdb")]
    public string? Imdb { get; set; }

    [JsonPropertyName("tmdb")]
    public int? Tmdb { get; set; }

    [JsonPropertyName("tvdb")]
    public int? Tvdb { get; set; }
}

public class TraktMovieItem
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("ids")]
    public TraktIds? Ids { get; set; }
}

public class TraktWatchedMovie
{
    [JsonPropertyName("movie")]
    public TraktMovieItem? Movie { get; set; }
}

public class TraktEpisodeItem
{
    [JsonPropertyName("number")]
    public int Number { get; set; }
}

public class TraktSeasonItem
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("episodes")]
    public List<TraktEpisodeItem>? Episodes { get; set; }
}

public class TraktShowItem
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("ids")]
    public TraktIds? Ids { get; set; }
}

public class TraktWatchedShow
{
    [JsonPropertyName("show")]
    public TraktShowItem? Show { get; set; }

    [JsonPropertyName("seasons")]
    public List<TraktSeasonItem>? Seasons { get; set; }
}

public class TraktWatchHistory
{
    public HashSet<int> WatchedMovieTmdbIds { get; set; } = new();

    /// <summary>
    /// Key format: "{tvdbId}:{seasonNumber}:{episodeNumber}"
    /// </summary>
    public HashSet<string> WatchedEpisodeKeys { get; set; } = new();
}
