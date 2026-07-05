namespace Identity.Application.Commands.ApiKey;

public record GenerateApiKeyCommand(Guid UserId, string Environment);
