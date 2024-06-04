namespace Manager.Shared.Enums;

[Flags]
public enum ItemType
{
    Guess = 0,
    Audio = 1,
    Video = 2,
    Subtitle = 4,
    Image = 8,
    Misc = 16
}