using Manager.Shared.Interfaces.Data;

namespace Manager.Shared.Entities;

public class SubtitleItem : MediaItem
{
    public string Name { get; }
    public string? Language { get; }

    public TimeSpan Duration { get; }

    public SubtitleItem(IDataService dataService, ulong id, string sourcePath, string pathTitle, string name,
        string? language, TimeSpan duration) : base(dataService, id, sourcePath, pathTitle)
    {
        this.Name = name;
        this.Language = language;
        this.Duration = duration;
    }
}