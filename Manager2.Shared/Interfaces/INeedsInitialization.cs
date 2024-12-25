namespace Manager2.Shared.Interfaces;

public interface INeedsInitialization
{
    public bool IsInitialized { get; set; } 
    
    public ValueTask<bool> InitializeAsync();
}