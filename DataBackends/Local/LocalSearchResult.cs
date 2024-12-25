namespace Local;

public class LocalSearchResult
{
    public string FileName { get; set; }
    
    public string FilePath { get; set; }
    
    public bool IsDirectory { get; set; }

    public LocalSearchResult(string fileName, string filePath, bool isDirectory)
    {
        FileName = fileName;
        FilePath = filePath;
        IsDirectory = isDirectory;
    }
}