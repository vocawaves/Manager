using System;
using System.IO;
using Manager.Shared.Entities;

namespace Manager.UI.Models.Data;

public class FileModel
{
    public event EventHandler? Clicked;
    
    private FileItem _fileItem;
    
    public string Name => _fileItem.Name;
    public string FullPath => _fileItem.Path;
    public string Extension => Path.GetExtension(_fileItem.Path);

    public FileModel(FileItem fileItem)
    {
        this._fileItem = fileItem;
    }
    
    public void OnFileClicked()
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }
}