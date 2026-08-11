namespace Identity.Application.Commands.Auth.VerifyEmail;

public record VerifyEmailCommand(string Email, string Token);
