﻿namespace Manager.Shared.Interfaces.General;

public interface IManagerComponent<out TConf> : IManagerComponent
    where TConf : class, IComponentConfiguration
{
    /// <summary>
    /// Component configuration, if any.
    /// </summary>
    public TConf Configuration { get; }
}