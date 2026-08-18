using System.Globalization;
using BuildingBlocks.Shared.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.API.Extensions;
using Payment.Application.DTOs;
using Payment.Application.Features.Command.CreateCustomer;
using Payment.Application.Features.Command.DeleteCustomer;
using Payment.Application.Features.Command.UpdateCustomer;
using Payment.Application.Features.Queries.GetCustomerById;
using Payment.Application.Features.Queries.ListCustomersByMerchant;

namespace Payment.API.Controllers;

[Authorize]
public class CustomersController : BaseApiController
{
    /// <summary>
    /// Create a new customer for a merchant.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerCommand command,
        [FromServices] CreateCustomerHandler handler)
    {
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Retrieve a single customer by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromQuery] Guid merchantId,
        [FromServices] GetCustomerByIdHandler handler)
    {
        var result = await handler.Handle(new GetCustomerByIdQuery(id, merchantId));
        return result.ToActionResult();
    }

    /// <summary>
    /// List customers for a merchant (paginated).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid merchantId,
        [FromServices] ListCustomersByMerchantHandler handler,
        [FromQuery] int skip = PageBounds.DefaultSkip,
        [FromQuery] int take = PageBounds.DefaultTake)
    {
        var (normalizedSkip, normalizedTake) = PageBounds.Normalize(skip, take);
        var result = await handler.Handle(new ListCustomersByMerchantQuery(merchantId, normalizedSkip, normalizedTake));
        if (result.IsFailure) return result.ToActionResult();

        Response.Headers["X-Total-Count"] = result.Value!.TotalCount.ToString(CultureInfo.InvariantCulture);
        return Ok(result.Value.Items);
    }

    /// <summary>
    /// Update a customer. Omitted fields are left unchanged.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCustomerCommand command,
        [FromServices] UpdateCustomerHandler handler)
    {
        command = new UpdateCustomerCommand(id, command.MerchantId, command.Email, command.Name, command.Phone, command.Description);
        var result = await handler.Handle(command);
        return result.ToActionResult();
    }

    /// <summary>
    /// Soft-delete a customer.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromQuery] Guid merchantId,
        [FromServices] DeleteCustomerHandler handler)
    {
        var result = await handler.Handle(new DeleteCustomerCommand(id, merchantId));
        return result.ToActionResult();
    }
}
