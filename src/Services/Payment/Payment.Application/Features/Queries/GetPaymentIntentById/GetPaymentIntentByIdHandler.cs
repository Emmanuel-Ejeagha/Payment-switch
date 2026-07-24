using BuildingBlocks.Shared.Results;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain;
using Payment.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Payment.Application.Features.Queries.GetPaymentIntentById;

public class GetPaymentIntentByIdHandler
{
    private readonly IPaymentIntentRepository _repository;
    private readonly ILogger<GetPaymentIntentByIdHandler> _logger;

    public GetPaymentIntentByIdHandler(IPaymentIntentRepository repository, ILogger<GetPaymentIntentByIdHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<PaymentIntentDto>> Handle(GetPaymentIntentByIdQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {CommandName} for Intent {IntentId}", nameof(GetPaymentIntentByIdQuery), query.IntentId);
        var intent = await _repository.GetByIdAsync(query.IntentId, cancellationToken);
        if (intent is null)
            return PaymentErrors.PaymentIntentNotFound(query.IntentId);

        return Map(intent);
    }

    private static PaymentIntentDto Map(PaymentIntent intent) =>
        new(intent.Id, intent.MerchantId, intent.Amount.Amount, intent.Amount.Currency,
            intent.Status.Value,
            intent.Transactions.Select(t => new TransactionDto(t.Id, t.Type.ToString(), t.Amount.Amount, t.Amount.Currency, t.Timestamp)).ToList());
}
