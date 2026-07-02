using Ledger.API.Extensions;
using Ledger.Application.DTOs;
using Ledger.Application.Features.Queries.GetBalance;
using Ledger.Application.Features.Queries.GetTransactionHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledger.API.Controllers;

[Authorize]
public class LedgerController : BaseApiController
{
    /// <summary>
    /// Get the current balance for a merchant.
    /// </summary>
    [HttpGet("balance")]
    [ProducesResponseType(typeof(BalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance(
        [FromQuery] Guid merchantId,
        [FromServices] GetBalanceHandler handler)
    {
        var result = await handler.Handle(new GetBalanceQuery(merchantId));
        return result.ToActionResult();
    }

    /// <summary>
    /// Get paginated transaction history for a merchant.
    /// </summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(List<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] Guid merchantId,
        [FromServices] GetTransactionHistoryHandler handler,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10)
    {
        var result = await handler.Handle(new GetTransactionHistoryQuery(merchantId, skip, take));
        return result.ToActionResult();
    }
}