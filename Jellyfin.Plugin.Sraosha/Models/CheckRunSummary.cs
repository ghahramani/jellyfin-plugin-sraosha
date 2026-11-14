using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Sraosha.Models;

/// <summary>
/// Detailed summary of a Sraosha check run.
/// </summary>
public class CheckRunSummary
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public bool IsDryRun { get; set; }

    public bool IsSuccess { get; set; } = true;

    public string? ErrorMessage { get; set; }

    public int StalledRequestsCount { get; set; }

    public int MoviesChecked { get; set; }

    public int MoviesTriggered { get; set; }

    public int MoviesSkipped { get; set; }

    public int MoviesErrors { get; set; }

    public int ShowsChecked { get; set; }

    public int ShowsTriggered { get; set; }

    public int EpisodesTriggered { get; set; }

    public int ShowsSkipped { get; set; }

    public int ShowsErrors { get; set; }

    public List<string> TriggeredItems { get; set; } = new();
}
