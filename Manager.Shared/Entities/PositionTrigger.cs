namespace Manager.Shared.Entities;

public class PositionTrigger
{
    public TimeSpan Position { get; }
    public string? Name { get; }
    
    public PositionTrigger(TimeSpan position, string? name = null)
    {
        this.Position = position;
        this.Name = name;
    }
}