namespace Manager.Shared.Entities;

/// <summary>
/// Simple wrapper for a directory item.
/// </summary>
public class DirectoryItem
{
    /// <summary>
    /// Directory name.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Full path of the directory.
    /// </summary>
    public string FullPath { get; }
    
    public DirectoryItem(string name, string fullPath)
    {
        this.Name = name;
        this.FullPath = fullPath;
    }
}