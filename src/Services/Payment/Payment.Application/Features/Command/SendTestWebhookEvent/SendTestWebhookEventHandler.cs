using BuildingBlocks.Shared.Results;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Payment.Application.Features.Queries.ListWebhookEvents;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using System.Text.Json;

namespace Payment.Application.Features.Command.SendTestWebhookEvent;

public class SendTestWebhookEventHandler
{
    private readonly IWebhookEventRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<SendTestWebhookEventCommand> _validator;
    private readonly ILogger<SendTestWebhookEventHandler> _logger;

    public SendTestWebhookEventHandler(
        IWebhookEventRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<SendTestWebhookEventCommand> validator,
        ILogger<SendTestWebhookEventHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<WebhookEventDto>> Handle(SendTestWebhookEventCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Merchant {MerchantId}", nameof(SendTestWebhookEventCommand), command.MerchantId);

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.Select(e => new Error(e.PropertyName, e.ErrorMessage)).ToList();

        var payload = JsonSerializer.Serialize(new
        {
            id = Guid.NewGuid(),
            type = command.EventType,
            occurred_on = DateTime.UtcNow,
            data = new { message = "This is a test webhook event from Payment Switch." }
        });

        var webhookEvent = new WebhookEvent(
            Guid.NewGuid(),
            command.MerchantId,
            command.EventType,
            payload,
            command.CorrelationId);

        await _repository.AddAsync(webhookEvent, cancellationToken);
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
