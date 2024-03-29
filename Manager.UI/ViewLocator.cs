using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Manager.UI.ViewModels;

namespace Manager.UI;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        return null;
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}