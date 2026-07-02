using Microsoft.AspNetCore.Mvc;

namespace Ledger.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
}