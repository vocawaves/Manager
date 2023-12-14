namespace Manager.Shared.Entities;

public class PlayItem
{
    public string Title { get; set; } = "Untitled";
    
    public string Artist { get; set; } = string.Empty;

    public TimeSpan Duration { get; set; }

    public byte[]? Thumbnail { get; set; }
    
    public string? ThumbnailFormat { get; set; }

    public required string OwnerPath { get; set; }

    public byte[]? Data { get; set; }

    public required string Format { get; set; }

    public ulong OwnerId { get; set; }

    public bool Cached { get; set; }
}