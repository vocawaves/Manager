using Manager.Shared.Events.General;
using Manager.Shared.Helpers;

namespace Manager.Shared.Interfaces.General;

public interface IManagerComponent
{
    public event AsyncEventHandler<InitSuccessEventArgs>? InitSuccess;
    public event AsyncEventHandler<InitFailedEventArgs>? InitFailed;
    
    public bool Initialized { get; }
    
    public string Name { get; }

    public ulong Parent { get; }

    public ValueTask<bool> InitializeAsync(params string[] options);
}