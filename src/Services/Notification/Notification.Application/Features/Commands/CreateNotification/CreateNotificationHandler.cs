using BuildingBlocks.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;
using Notification.Domain.ValueObjects;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Application.Features.Commands.CreateNotification;

public class CreateNotificationHandler
{
    private readonly INotificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateNotificationCommand> _validator;
    private readonly ILogger<CreateNotificationHandler> _logger;

    public CreateNotificationHandler(
        INotificationRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<CreateNotificationCommand> validator,
        ILogger<CreateNotificationHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateNotificationCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName}", nameof(CreateNotificationCommand));

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

        return notification.Id;
    }
}
