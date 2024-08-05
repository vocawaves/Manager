namespace Manager.Shared.Interfaces.General;

public interface IManagerComponent
{
    /// <summary>
    /// The instancer that created this component.
    /// </summary>
    public ComponentManager ComponentManager { get; }

    /// <summary>
    /// Name of the component.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Unused for now. (always 0)
    /// Meant for remote components.
    /// </summary>
    public ulong Parent { get; }
}