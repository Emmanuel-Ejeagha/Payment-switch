using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;

namespace Notification.Application.Features.Commands.SendPendingNotification;

public class SendPendingNotificationHandler
{
    private readonly INotificationRepository _repository;
    private readonly INotificationSender _sender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly ILogger<SendPendingNotificationHandler> _logger;

    public SendPendingNotificationHandler(
        INotificationRepository repository,
        INotificationSender sender,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        ILogger<SendPendingNotificationHandler> logger)
    {
        _repository = repository;
        _sender = sender;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(notification.DomainEvents, cancellationToken);

        return Result.Success();
    }
}
