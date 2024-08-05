namespace SimplePlayer.Entities;

public class Sound
{
    public string? MediaPath { get; set; }
    public int Column { get; set; }
    public int Row { get; set; }
    public bool Loop { get; set; }
    public bool Fade { get; set; }
    public double FadeDuration { get; set; } = 3d;
    public double Volume { get; set; } = 100d;
    public string? Device { get; set; } = null;
}