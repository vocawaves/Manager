namespace Manager.Shared.Entities;

public class DirectoryItem
{
    public string Name { get; }
    public string FullPath { get; }
    
    public DirectoryItem(string name, string fullPath)
    {
        this.Name = name;
        this.FullPath = fullPath;
    }
}