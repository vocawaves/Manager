namespace Manager.Shared.Interfaces;

public interface ISubtitleChannel : IMediaChannel
{
    public ISubtitleBackendService AssociatedSubtitleBackend { get; }
}