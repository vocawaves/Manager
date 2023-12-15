using Manager.Shared.Delegates;

namespace Manager.Shared;

public abstract class ManagerComponent(string name, ulong parent)
{
    public event ManagerComponentErrorEventHandler? ComponentErrored;

    public bool Initialized { get; protected set; } = false;
    
    public string Name { get; } = name;

    public ulong Parent { get; } = parent;

    public virtual ValueTask<bool> InitializeAsync(params string[] options) =>
        new(true);
    
    
    protected void SendError(ManagerComponent sender, string method, Exception exception, params object[] args)
    {
        ComponentErrored?.Invoke(
            sender,
            $"{method} on {this.Name} client {this.Parent} failed!\n" +
            $"args: {string.Join(", ", args)}",
            exception);
    }

    protected void SendError(ManagerComponent sender, string method, string message, params object[] args)
    {
        ComponentErrored?.Invoke(
            sender,
            $"{method} on {this.Name} client {this.Parent} failed!\n" +
            $"args: {string.Join(", ", args)}\n" +
            $"message: {message}");
    }
}