namespace Identity.Application.Commands.Auth.ResetPassword;

public record ResetPasswordCommand(string Email, string Token, string NewPassword);
