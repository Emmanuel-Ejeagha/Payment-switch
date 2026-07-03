using Microsoft.AspNetCore.Mvc;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
}