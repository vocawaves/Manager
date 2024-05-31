using Manager.Shared.Interfaces.Data;
using Manager.Shared.Interfaces.General;

namespace Manager.DataBackends.Local;

public class LocalDataServiceConfiguration : IComponentConfiguration
{
    public required ICacheStrategy CacheStrategy { get; init; }
}