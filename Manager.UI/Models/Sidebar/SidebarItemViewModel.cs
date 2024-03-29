using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Manager.UI.ViewModels;

namespace Manager.UI.Models.Sidebar;

public partial class SidebarItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isSelected;
    
    [ObservableProperty]
    private bool _isInitialized;
    
    [ObservableProperty]
    private IManagerViewModel? _viewModel;
    
    
    public SidebarItemViewModel(IManagerViewModel viewModel)
    {
        var managerComponent = viewModel.ManagerComponent;
        IsInitialized = managerComponent?.Initialized ?? false;
        if (managerComponent is not null) 
            managerComponent.InitSuccess += async (sender, args) => await Dispatcher.UIThread.InvokeAsync(() => this.IsInitialized = true);
        ViewModel = viewModel;
        if (!IsInitialized && managerComponent is not null)
            Task.Run(async () => await managerComponent.InitializeAsync());
    }
    
}