using Manager.Shared.Interfaces.General;
using Microsoft.Extensions.Logging;

namespace Manager.Shared;

public class Instancer
{
    private readonly ILoggerFactory _loggerFactory;

    public Instancer(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }
    
    public T CreateManagerComponent<T>(string name, ulong parent) where T : class, IManagerComponent
    {
        return T.Create(this, name, parent) as T ?? throw new InvalidOperationException("Failed to create manager component");
    }

    public ILogger<T> CreateLogger<T>() where T : class
    {
        return _loggerFactory.CreateLogger<T>();
    }
}