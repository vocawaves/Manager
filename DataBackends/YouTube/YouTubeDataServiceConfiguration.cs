using Manager.Shared.Interfaces.Data;
using Manager.Shared.Interfaces.General;

namespace Manager.DataBackends.YouTube;

public class YouTubeDataServiceConfiguration : IComponentConfiguration
{
    public ICacheStrategy? CacheStrategy { get; set; }
}