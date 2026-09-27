using Asp.Versioning;
using BuildingBlocks.Shared.Auth;
using Ledger.API.Extensions;
using Ledger.Application.DTOs;
using Ledger.Application.Features.Commands.RunReconciliation;
using Ledger.Application.Features.Queries.GetLatestReconciliation;
using Ledger.Application.Features.Queries.ListReconciliationReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledger.API.Controllers;

/// <summary>
/// Admin ledger reconciliation: verifies each account's stored balances
/// against its journal entries and exposes the audit report history.
/// State-changing runs are Admin-only; report reads are Admin + Support.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reconciliation")]
[Authorize]
public class ReconciliationController : ControllerBase
{
    /// <summary>
    /// Run a reconciliation now and persist the audit report (Admin only).
    /// </summary>
    [HttpPost("run")]
    [Authorize(Roles = RolePolicies.AdminOnly)]
    [ProducesResponseType(typeof(ReconciliationReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Run(
        [FromServices] RunReconciliationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RunReconciliationCommand(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get the most recent reconciliation report (Admin + Support read).
    /// </summary>
    [HttpGet("latest")]
    [Authorize(Roles = RolePolicies.ReadOnly)]
    [ProducesResponseType(typeof(ReconciliationReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Latest(
        [FromServices] GetLatestReconciliationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetLatestReconciliationQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// List reconciliation reports, newest first, paginated (Admin + Support read).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = RolePolicies.ReadOnly)]
    [ProducesResponseType(typeof(List<ReconciliationReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromServices] ListReconciliationReportsHandler handler,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.Handle(new ListReconciliationReportsQuery(skip, take), cancellationToken);
        return result.ToActionResult();
    }
}