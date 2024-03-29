namespace Manager.Shared.Entities;

/// <summary>
/// Simple wrapper for a file item.
/// </summary>
public class FileItem
{
    /// <summary>
    /// File name.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Full path of the file.
    /// </summary>
    public string Path { get; }

    public FileItem(string name, string path)
    {
        this.Name = name;
        this.Path = path;
    }
}