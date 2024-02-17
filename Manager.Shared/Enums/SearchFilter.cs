namespace Manager.Shared.Enums;

[Flags]
public enum SearchFilter
{
    None = 0,
    Item = 1,
    Author = 2,
    Playlist = 4
}