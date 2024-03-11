using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Data;

public interface IFileSystemSource : IDataService
{
    public string MountName { get; }

    public ValueTask<DirectoryItem[]> GetDirectoriesAsync(string? path = null);
    public ValueTask<FileItem[]> GetFilesAsync(DirectoryItem? item = null, params string[] extensions);

}