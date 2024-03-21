using System;
using Manager.Shared.Entities;

namespace Manager.UI.Models.Data;

public class FolderModel
{
    public event EventHandler? FolderClicked;
    
    private readonly DirectoryItem _directoryItem;
    
    public string Name => _directoryItem.Name;
    public string Path => _directoryItem.FullPath;
    public bool IsBack { get; set; }

    public FolderModel(DirectoryItem directoryItem, bool isBack = false)
    {
        _directoryItem = directoryItem;
        IsBack = isBack;
    }

    public void OnFolderClicked()
    {
        FolderClicked?.Invoke(this, EventArgs.Empty);
    }
}