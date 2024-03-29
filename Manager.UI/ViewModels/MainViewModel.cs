using Microsoft.Extensions.Logging;

namespace Manager.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{    
    public MainViewModel()
    {
        var lf = new LoggerFactory();
    }
}
