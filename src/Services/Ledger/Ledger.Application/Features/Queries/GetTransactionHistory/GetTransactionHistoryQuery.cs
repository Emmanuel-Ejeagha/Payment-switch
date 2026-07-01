namespace Ledger.Application.Features.Queries.GetTransactionHistory;

public record GetTransactionHistoryQuery(Guid MerchantId, int Skip, int Take);

