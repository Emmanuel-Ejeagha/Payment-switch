namespace Identity.Application.Commands.Role;

public record AssignRoleCommand(Guid AdminUserId, Guid TargetUserId, string Role);

