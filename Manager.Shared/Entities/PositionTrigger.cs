namespace Manager.Shared.Entities;

/// <summary>
/// Represents a trigger that is activated at a specific position in a media item playback.
/// </summary>
public class PositionTrigger
{
    /// <summary>
    /// The position in the media item where the trigger is activated.
    /// </summary>
    public TimeSpan Position { get; }
    /// <summary>
    /// The name of the trigger.
    /// </summary>
    public string? Name { get; }
    
    public PositionTrigger(TimeSpan position, string? name = null)
    {
        this.Position = position;
        this.Name = name;
    }
}