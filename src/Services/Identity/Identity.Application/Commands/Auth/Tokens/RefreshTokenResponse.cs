namespace Identity.Application.Commands.Auth.Tokens;

public record RefreshTokenResponse(string AccessToken, string RefreshToken, int ExpiresIn);
