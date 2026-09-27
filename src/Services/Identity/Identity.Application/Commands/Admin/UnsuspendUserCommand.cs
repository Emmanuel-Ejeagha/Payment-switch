namespace Identity.Application.Commands.Admin;

public record UnsuspendUserCommand(Guid AdminUserId, Guid TargetUserId);
