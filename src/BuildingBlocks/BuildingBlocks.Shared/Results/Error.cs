namespace BuildingBlocks.Shared.Results;

public record Error 
{
    public string Code { get; } = string.Empty;
    public string Message { get; } = string.Empty;

    public Error(string code, string message)
    {
        Code = code;
        Message = message;
    }
}
