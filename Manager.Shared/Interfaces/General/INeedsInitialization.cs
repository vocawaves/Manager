using Manager.Shared.Events.General;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.General;

public interface INeedsInitialization : IManagerComponent
{
    /// <summary>
    /// Fired when the component is successfully initialized.
    /// </summary>
    public event AsyncEventHandler? InitSuccess;
    /// <summary>
    /// Fired when the component failed to initialize.
    /// </summary>
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed;
    
    /// <summary>
    /// Is the component initialized?
    /// </summary>
    public bool Initialized { get; }

    /// <summary>
    /// Initializes the component.
    /// </summary>
    public ValueTask<bool> InitializeAsync(params string[] options);
}