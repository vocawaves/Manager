using Manager.Shared.Entities;

namespace Manager.Shared.Interfaces.Data;

/// <summary>
/// Adds to the IDataService by providing a way to get files and directories from a file system.
/// </summary>
public interface IFileSystemSource : IDataService
{
    /// <summary>
    /// Get the mount points of the file system.
    /// For example, on Windows, this would return the drives.
    /// </summary>
    public ValueTask<DirectoryItem[]> GetMountPointsAsync();
    
    /// <summary>
    /// Gets the files in a directory.
    /// Additionally only returns files with the specified extensions.
    /// </summary>
    public ValueTask<FileItem[]> GetFilesAsync(string uri, params string[] extensions);
    /// <summary>
    /// Gets the directories in a directory.
    /// </summary>
    public ValueTask<DirectoryItem[]> GetDirectoriesAsync(string uri);

}