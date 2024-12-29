using Microsoft.Extensions.Logging;

namespace Manager2.Shared.Entities;

public class ReturnMessage
{
    public LogLevel LogLevel { get; }
    
    public string Message { get; }
    
    
    public ReturnMessage(LogLevel logLevel, string message, params object?[] args)
    {
        LogLevel = logLevel;
        Message = string.Format(message, args);
    }
}