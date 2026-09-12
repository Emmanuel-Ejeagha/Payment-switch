using BuildingBlocks.Shared.Aggregate;
using Settlement.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Settlement.Domain.Entities;

public class Payout : BaseEntity
{
    public Guid MerchantId { get; private set; }
    public Money GrossVolume { get; private set; } = default!;
    public Money Fees { get; private set; } = default!;
    public Money NetAmount { get; private set; } = default!;
    public string Currency { get; private set; } = default!;

    private Payout() : base() { }

    public Payout(Guid merchantId, Money grossVolume, Money fees) : base()
    {
        if (grossVolume.Currency != fees.Currency)
            throw new ArgumentException("Currencies must match.");

        MerchantId = merchantId;
        GrossVolume = grossVolume;
        Fees = fees;
        // Processing fees are non-refundable: when refunds wipe the day's
        // gross, the payout floors at zero instead of going negative (and
        // throwing in Money). The unrecovered fee remains platform income.
        NetAmount = new Money(Math.Max(0, grossVolume.Amount - fees.Amount), grossVolume.Currency);
        Currency = grossVolume.Currency;
    }
}
