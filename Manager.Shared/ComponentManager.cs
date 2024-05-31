using Manager.Shared.Interfaces.General;
using Microsoft.Extensions.Logging;

namespace Manager.Shared;

public class ComponentManager
{
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<ComponentManager>? _logger;

    public List<IManagerComponent> Components { get; } = new();

    public ComponentManager(ILoggerFactory? loggerFactory)
    {
        _loggerFactory = loggerFactory;
        if (loggerFactory != null)
            _logger = loggerFactory.CreateLogger<ComponentManager>();
    }

    public T? CreateManagerComponent<T, TConf>(string name, ulong parent, TConf configuration)
        where T : class, IManagerComponent<TConf> where TConf : class, IComponentConfiguration
    {
        try
        {
            var actObject = T.CreateWithConfiguration(this, name, parent, configuration);
            if (actObject is not T component)
            {
                _logger?.LogError($"Failed to create component of type {typeof(T).Name}");
                return null;
            }

            Components.Add(component);
            return component;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, $"Failed to create component of type {typeof(T).Name}");
        }

        return null;
    }
    
    public T? CreateManagerComponent<T>(string name, ulong parent)
        where T : class, IManagerComponent
    {
        try
        {
            var actObject = T.Create(this, name, parent);
            if (actObject is not T component)
            {
                _logger?.LogError($"Failed to create component of type {typeof(T).Name}");
                return null;
            }

            Components.Add(component);
            return component;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, $"Failed to create component of type {typeof(T).Name}");
        }

        return null;
    }

    public ILogger<T>? CreateLogger<T>() where T : class
    {
        return _loggerFactory?.CreateLogger<T>();
    }
}