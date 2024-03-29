﻿using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Entities;

/// <summary>
/// An extension of <see cref="MediaItem"/> that represents a subtitle item.
/// </summary>
public class SubtitleItem : MediaItem
{
    /// <summary>
    /// Name of the subtitle item.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Language of the subtitle item.
    /// </summary>
    public string? Language { get; }

    /// <summary>
    /// Duration of the subtitle item.
    /// </summary>
    public TimeSpan Duration { get; }

    public SubtitleItem(ILoggerFactory lf, IDataService dataService, ulong id, string sourcePath, string pathTitle, string name,
        string? language, TimeSpan duration) : base(lf, dataService, id, sourcePath, pathTitle)
    {
        this.Name = name;
        this.Language = language;
        this.Duration = duration;
    }
}