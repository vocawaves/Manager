using Microsoft.Extensions.Logging;

namespace Manager2.Shared;

public class LoggingHelper
{
    private static ILoggerFactory? _loggerFactory;

    public static void SetLoggerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }
    
    public static ILogger<T>? CreateLogger<T>()
    {
        return _loggerFactory?.CreateLogger<T>();
    }
}