using BuildingBlocks.Shared.Events;
using BuildingBlocks.Shared.Results;
using FluentValidation;
using Notification.Application.Interfaces;
using Notification.Domain.ValueObjects;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Application.Features.Commands.CreateNotification;

public class CreateNotificationHandler
{
    private readonly INotificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IValidator<CreateNotificationCommand> _validator;

    public CreateNotificationHandler(
        INotificationRepository repository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IValidator<CreateNotificationCommand> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _validator = validator;
    }

    public async Task<Result<Guid>> Handle(CreateNotificationCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var channel = NotificationChannel.FromString(command.Channel);
        var notification = new NotificationEntity(
            Guid.NewGuid(),
            command.Recipient,
            channel,
            command.Subject,
            command.Body,
            command.WebhookUrl,
            command.Payload,
            command.MaxRetries ?? 5);

        await _repository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _dispatcher.DispatchAsync(notification.DomainEvents, cancellationToken);

        return notification.Id;
    }
}
