namespace Manager.Shared.Entities;

public class FileItem
{
    public string Name { get; }
    public string Path { get; }

    public FileItem(string name, string path)
    {
        this.Name = name;
        this.Path = path;
    }
}