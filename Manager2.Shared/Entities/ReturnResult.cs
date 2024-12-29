namespace Manager2.Shared.Entities;

public class ReturnResult
{
    public bool Success { get; set; }
    public List<ReturnMessage> Messages { get; set; } = new();

    public static implicit operator bool(ReturnResult result) => result.Success;
}

public class ReturnResult<T> : ReturnResult
{
    public T? Value { get; set; }
}