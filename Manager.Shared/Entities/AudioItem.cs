﻿using Manager.Shared.Interfaces.Data;
using Microsoft.Extensions.Logging;

namespace Manager.Shared.Entities;

/// <summary>
/// An extension of <see cref="MediaItem"/> that represents an audio item.
/// </summary>
public class AudioItem : MediaItem
{
    /// <summary>
    /// Title of the audio item.
    /// </summary>
    public string? Title { get; }
    /// <summary>
    /// Artist of the audio item.
    /// </summary>
    public string? Artist { get; }

    /// <summary>
    /// Duration of the audio item.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Album art in bytes. (if available)
    /// </summary>
    public byte[]? AlbumArt { get; private set; }
    /// <summary>
    /// Mime type of the album art. (if available)
    /// </summary>
    public string? AlbumArtMimeType { get; }

    /// <summary>
    /// Display title of the audio item. Custom title if available, otherwise title or path title.
    /// Truly a chain of nullables lol.
    /// </summary>
    public override string DisplayTitle => GetDisplayTitle();

    private string GetDisplayTitle()
    {
        if (this.CustomTitle != null) return this.CustomTitle;
        if (this.Title == null) return this.PathTitle;
        if (this.Artist != null) return $"{this.Artist} - {this.Title}";
        return this.Title;
    }

    public AudioItem(ILoggerFactory lf, IDataService dataService, ulong ownerId, string sourcePath, string pathTitle, string title,
        string artist, TimeSpan duration) : base(lf, dataService, ownerId, sourcePath, pathTitle)
    {
        Title = title;
        Artist = artist;
        Duration = duration;
    }

    public AudioItem(ILoggerFactory lf, IDataService dataService, ulong ownerId, string sourcePath, string pathTitle, string title,
        string artist, TimeSpan duration, byte[] albumArt, string albumArtMimeType) : base(lf, dataService, ownerId,
        sourcePath, pathTitle)
    {
        Title = title;
        Artist = artist;
        Duration = duration;
        AlbumArt = albumArt;
        AlbumArtMimeType = albumArtMimeType;
    }
}