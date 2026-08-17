using BuildingBlocks.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Payment.Application.Features.Queries.ListWebhookEvents;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Application.Features.Command.ReplayWebhookEvent;

public class ReplayWebhookEventHandler
{
    private readonly IWebhookEventRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ReplayWebhookEventCommand> _validator;
    private readonly ILogger<ReplayWebhookEventHandler> _logger;

    public ReplayWebhookEventHandler(
        IWebhookEventRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<ReplayWebhookEventCommand> validator,
        ILogger<ReplayWebhookEventHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<WebhookEventDto>> Handle(ReplayWebhookEventCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Event {EventId}", nameof(ReplayWebhookEventCommand), command.EventId);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var webhookEvent = await _repository.GetByIdAsync(command.EventId, cancellationToken);
        if (webhookEvent is null || webhookEvent.MerchantId != command.MerchantId)
            return new Error("Webhook.EventNotFound", "Webhook event not found.");

        webhookEvent.ResetForReplay();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new WebhookEventDto(
            webhookEvent.Id,
            webhookEvent.EventType,
            webhookEvent.Status,
            webhookEvent.Attempts,
            webhookEvent.CreatedAt,
            webhookEvent.LastAttemptAt,
            webhookEvent.LastError,
            webhookEvent.CorrelationId);

        return Result<WebhookEventDto>.Success(dto);
    }
}
