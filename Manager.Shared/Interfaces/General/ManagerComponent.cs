using Manager.Shared.Events.General;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.General;

/// <summary>
/// Base interface for all manager components.
/// </summary>
public abstract class ManagerComponent : IManagerComponent
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
    public bool Initialized { get; protected set; } = false;

    /// <summary>
    /// The ComponentManager that created this component.
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

    /// <summary>
    /// Component configuration, if any.
    /// </summary>
    public IComponentConfiguration? Configuration { get; }

    /// <summary>
    /// Initializes the component.
    /// </summary>
    public abstract ValueTask<bool> InitializeAsync(params string[] options);

    public ManagerComponent(ComponentManager componentManager, string name, ulong parent, IComponentConfiguration? configuration = null)
    {
        ComponentManager = componentManager;
        Name = name;
        Parent = parent;
        Configuration = configuration;
    }

    protected void OnInitSuccess(string? reason = null)
    {
        InitSuccess?.InvokeAndForget(this, new InitSuccessEventArgs(reason));
    }
    
    protected void OnInitFailed(string reason)
    {
        InitFailed?.InvokeAndForget(this, new InitFailedEventArgs(reason));
    }
}