namespace Identity.Application.Commands.ApiKey;

public record ApiKeyResponse(Guid KeyId, string PlainTextKey, string Environment, DateTime CreatedAt);
