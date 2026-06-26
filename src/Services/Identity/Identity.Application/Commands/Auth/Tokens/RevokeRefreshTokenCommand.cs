namespace Identity.Application.Commands.Auth.Tokens;

public record RevokeRefreshTokenCommand(Guid UserId, string RefreshToken);
