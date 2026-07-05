namespace Identity.Application.Commands.ApiKey;

public record RevokeApiKeyCommand(Guid UserId, Guid KeyId);

