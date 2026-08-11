namespace Identity.Application.Commands.Auth.ChangePassword;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword);
