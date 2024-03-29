using Manager.Shared.Events.General;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.General;

/// <summary>
/// Base interface for all manager components.
/// </summary>
public interface IManagerComponent
{
    /// <summary>
    /// Fired when the component is successfully initialized.
    /// </summary>
    public event AsyncEventHandler<InitSuccessEventArgs>? InitSuccess;
    /// <summary>
    /// Fired when the component failed to initialize.
    /// </summary>
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed;
    
    /// <summary>
    /// Is the component initialized?
    /// </summary>
    public bool Initialized { get; }
    
    /// <summary>
    /// Name of the component.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Unused for now. (always 0)
    /// Meant for remote components.
    /// </summary>
    public ulong Parent { get; }

    /// <summary>
    /// Initializes the component.
    /// </summary>
    public ValueTask<bool> InitializeAsync(params string[] options);
}