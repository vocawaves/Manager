namespace Manager.Shared.Interfaces;

public interface IVideoChannel
{
    public IVideoBackendService AssociatedVideoBackend { get; }
}