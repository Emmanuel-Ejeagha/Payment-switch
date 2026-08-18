using BuildingBlocks.Shared.Paging;
using BuildingBlocks.Shared.Results;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain;

namespace Payment.Application.Features.Queries.ListWebhookEvents;

public class ListWebhookEventsHandler
{
    private readonly IWebhookEventRepository _repository;
    private readonly IMerchantService _merchantService;
    private readonly ILogger<ListWebhookEventsHandler> _logger;

    public ListWebhookEventsHandler(
        IWebhookEventRepository repository,
        IMerchantService merchantService,
        ILogger<ListWebhookEventsHandler> logger)
    {
        _repository = repository;
        _merchantService = merchantService;
        _logger = logger;
    }

    public async Task<Result<PagedData<WebhookEventDto>>> Handle(ListWebhookEventsQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {QueryName} for Merchant {MerchantId}", nameof(ListWebhookEventsQuery), query.MerchantId);

        var caller = query.Caller ?? Auth.CallerContext.Anonymous;
        var ownerResult = await _merchantService.GetMerchantOwnerAsync(query.MerchantId, cancellationToken);
        if (!ownerResult.IsSuccess || !caller.CanAccess(ownerResult.Value))
            return PaymentErrors.Unauthorized();

        var events = await _repository.ListByMerchantAsync(query.MerchantId, query.Skip, query.Take, cancellationToken);
        var total = await _repository.CountByMerchantAsync(query.MerchantId, cancellationToken);
        var dtos = events.Select(e => new WebhookEventDto(
            e.Id,
            e.EventType,
            e.Status,
            e.Attempts,
            e.CreatedAt,
            e.LastAttemptAt,
            e.LastError,
            e.CorrelationId)).ToList();

        return new PagedData<WebhookEventDto>(dtos, total);
    }
}
