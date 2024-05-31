using Manager.Shared.Interfaces.General;

namespace Manager.Shared.Cache;

public class FolderCacheStrategyConfiguration : IComponentConfiguration
{
    public required string CacheFolder { get; set; }
}