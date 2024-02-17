using Manager.Shared.Events.Global;
using Manager.Shared.Helpers;

namespace Manager.Shared;

public abstract class ManagerComponent(string name, ulong parent)
{
    public event AsyncEventHandler<InitSuccessEventArgs>? InitSuccess;
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed; 
    
    public bool Initialized { get; protected set; } = false;
    
    public string Name { get; } = name;

    public ulong Parent { get; } = parent;

    public virtual async ValueTask<bool> InitializeAsync(params string[] options)
    {
        this.Initialized = true;
        await InitSuccess.InvokeAsync(this, new InitSuccessEventArgs());
        return true;
    }
}