using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Manager.UI.ViewModels;
using Manager.UI.ViewModels.Data.Components;

namespace Manager.UI;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is ViewModelBase viewModel)
        {
            return viewModel switch
            {
                MainViewModel => new Views.MainView(),
                CacheItemViewModel => new Views.Data.Components.CacheItemView(),
                _ => null
            };
        }
        return null;
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}