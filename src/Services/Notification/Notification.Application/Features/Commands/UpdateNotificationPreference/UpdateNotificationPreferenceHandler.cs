using BuildingBlocks.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Features.Commands.UpdateNotificationPreference;

public class UpdateNotificationPreferenceHandler
{
    private readonly INotificationPreferenceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateNotificationPreferenceCommand> _validator;
    private readonly ILogger<UpdateNotificationPreferenceHandler> _logger;

    public UpdateNotificationPreferenceHandler(
        INotificationPreferenceRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateNotificationPreferenceCommand> validator,
        ILogger<UpdateNotificationPreferenceHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(UpdateNotificationPreferenceCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for {Recipient}", nameof(UpdateNotificationPreferenceCommand), command.Recipient);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var recipient = command.Recipient.Trim().ToLowerInvariant();
        var existing = await _repository.FindAsync(recipient, command.Channel, command.EventType, cancellationToken);

        Guid id;
        if (existing is null)
        {
            var preference = new NotificationPreference(
                Guid.NewGuid(),
                recipient,
                NotificationChannel.FromString(command.Channel),
                command.EventType,
                command.Enabled);
            await _repository.AddAsync(preference, cancellationToken);
            id = preference.Id;
        }
        else
        {
            existing.Update(command.Enabled);
            id = existing.Id;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return id;
    }
}
