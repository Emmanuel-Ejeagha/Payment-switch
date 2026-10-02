namespace Identity.Application.Commands.Admin;

public record SuspendUserCommand(Guid AdminUserId, Guid TargetUserId);
