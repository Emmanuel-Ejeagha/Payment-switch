using BuildingBlocks.Shared.Results;
using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;

namespace Notification.Application.Features.Commands.SendPendingNotification;

public class SendPendingNotificationHandler
{
    private readonly INotificationRepository _repository;
    private readonly INotificationSender _sender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendPendingNotificationHandler> _logger;

    public SendPendingNotificationHandler(
        INotificationRepository repository,
        INotificationSender sender,
        IUnitOfWork unitOfWork,
        ILogger<SendPendingNotificationHandler> logger)
    {
        _repository = repository;
        _sender = sender;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(SendPendingNotificationCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} with Id {Id}", nameof(SendPendingNotificationCommand), command.NotificationId);

        var notification = await _repository.GetByIdAsync(command.NotificationId, cancellationToken);
        if (notification is null)
            return new Error("Notification.NotFound", "Notification not found.");

        var sendResult = await _sender.SendAsync(notification, cancellationToken);
        if (sendResult.IsSuccess)
        {
            notification.MarkAsSent();
        }
        else
        {
            notification.MarkAsFailed();
        }

        // The worker attempt is over — drop the claim lease. NextRetryAt (set on
        // failure) or the terminal status gates any future pick-up, so the row
        // cannot be double-picked while this attempt is still in flight.
        notification.ReleaseLease();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
