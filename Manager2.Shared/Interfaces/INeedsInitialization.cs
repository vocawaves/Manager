using Manager2.Shared.Entities;

namespace Manager2.Shared.Interfaces;

public interface INeedsInitialization
{
    public bool IsInitialized { get; set; } 
    
    public ValueTask<ReturnResult> InitializeAsync();
}