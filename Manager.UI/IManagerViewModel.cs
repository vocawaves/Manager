using Manager.Shared.Interfaces.General;

namespace Manager.UI;

public interface IManagerViewModel
{
    public IManagerComponent ManagerComponent { get; set; }
}