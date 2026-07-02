using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using Notification.Application.Interfaces;

namespace Notification.Application.Features.Commands.SendPendingNotification;

public class SendPendingNotificationHandler
{
    private readonly INotificationRepository _repository;
    private readonly INotificationSender _sender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;

    public SendPendingNotificationHandler(
        INotificationRepository repository,
        INotificationSender sender,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher)
    {
        _repository = repository;
        _sender = sender;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task<Result> Handle(SendPendingNotificationCommand command, CancellationToken cancellationToken = default)
    {
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
