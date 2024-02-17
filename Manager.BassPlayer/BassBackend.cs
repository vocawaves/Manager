using Manager.Shared;
using Manager.Shared.Entities;
using Manager.Shared.Interfaces;
using Manager.Shared.Interfaces.Audio;
using Manager.Shared.Interfaces.General;

namespace Manager.BassPlayer;

public class BassBackend : ManagerComponent
{
    public BassBackend(string name, ulong parent) : base(name, parent)
    {
    }
}