namespace Manager.Shared.Interfaces.General;

public interface IManagerComponent<TConf> : IManagerComponent
    where TConf : class, IComponentConfiguration
{
    /// <summary>
    /// Component configuration, if any.
    /// </summary>
    public TConf? Configuration { get; }
    
    public static virtual IManagerComponent<TConf>? CreateWithConfiguration(ComponentManager componentManager, string name, ulong parent, TConf configuration) 
    {
        throw new NotImplementedException();
    }
}