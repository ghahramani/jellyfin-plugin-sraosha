using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Sraosha.Models;

public enum MediaStatus
{
    Unknown = 1,
    Pending = 2,
    Processing = 3,
    PartiallyAvailable = 4,
    Available = 5
}

public enum RequestStatus
{
    PendingApproval = 1,
    Approved = 2,
    Declined = 3
}

public class JellyseerrMediaInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("tmdbId")]
    public int? TmdbId { get; set; }

    [JsonPropertyName("tvdbId")]
    public int? TvdbId { get; set; }

    [JsonPropertyName("status")]
    public MediaStatus Status { get; set; }

    [JsonPropertyName("mediaType")]
    public string MediaType { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }
}

public class JellyseerrSeasonRequest
{
    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    [JsonPropertyName("status")]
    public RequestStatus Status { get; set; }
}

public class JellyseerrRequest
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("status")]
    public RequestStatus Status { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("media")]
    public JellyseerrMediaInfo? Media { get; set; }

    [JsonPropertyName("seasons")]
    public List<JellyseerrSeasonRequest>? Seasons { get; set; }
}

public class JellyseerrPageInfo
{
    [JsonPropertyName("pages")]
    public int Pages { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("results")]
    public int Results { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }
}

public class JellyseerrPagedResponse<T>
{
    [JsonPropertyName("pageInfo")]
    public JellyseerrPageInfo PageInfo { get; set; } = new();

    [JsonPropertyName("results")]
    public List<T> Results { get; set; } = new();
}
