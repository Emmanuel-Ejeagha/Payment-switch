namespace Identity.Application.Commands.Auth.Register;

public record RegisterUserCommand(string Email, string Password, string FullName);