namespace Manager.UI.Models;

public class FileDirectoryItem
{
    public required string ShortName { get; set; }
    public required string FullPath { get; set; }
    public bool IsDirectory { get; set; }
}