using System.Diagnostics.CodeAnalysis;
using Manager.Shared.Interfaces.General;
using Microsoft.Extensions.Logging;

namespace Manager.Shared;

public class ComponentManager
{
    public static ComponentManager? MainInstance { get; private set; }
    
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<ComponentManager>? _logger;

    public List<IManagerComponent> Components { get; } = new();

    public ComponentManager(ILoggerFactory? loggerFactory)
    {
        _loggerFactory = loggerFactory;
        if (loggerFactory != null)
            _logger = loggerFactory.CreateLogger<ComponentManager>();
        MainInstance ??= this;
    }

    public T? CreateComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        string name, ulong parentId) where T : IManagerComponent
    {
        try
        {
            var bareObject = Activator.CreateInstance(typeof(T), this, name, parentId);
            if (bareObject is not T component)
            {
                _logger?.LogError("Failed to create component {ComponentName}", name);
                return default;
            }

            Components.Add(component);
            return component;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to create component {ComponentName}", name);
            return default;
        }
    }

    public T? CreateComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T,TConf>(
        string name, ulong parentId, TConf configuration) where T : IManagerComponent
        where TConf : IComponentConfiguration
    {
        try
        {
            var bareObject = Activator.CreateInstance(typeof(T), this, name, parentId, configuration);
            if (bareObject is not T component)
            {
                _logger?.LogError("Failed to create component {ComponentName}", name);
                return default;
            }

            Components.Add(component);
            return component;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to create component {ComponentName}", name);
            return default;
        }
    }

    public ILogger<T>? CreateLogger<T>() where T : class
    {
        return _loggerFactory?.CreateLogger<T>();
    }
}