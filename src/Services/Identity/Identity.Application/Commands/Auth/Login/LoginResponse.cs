namespace Identity.Application.Commands.Auth.Login;

public record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);
