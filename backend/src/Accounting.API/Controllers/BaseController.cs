using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.API.Controllers;

/// <summary>
/// Base for all controllers. Requires authentication by default.
/// Use [AllowAnonymous] on controllers or actions that are public (e.g. AuthController).
/// Use [Authorize(Policy = PermissionNames.XxxYyy)] on actions that need a specific permission.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public abstract class BaseController : ControllerBase
{
    protected IActionResult OkPaged<T>(T result) => Ok(result);

    protected IActionResult Created<T>(string routeName, object routeValues, T result)
        => CreatedAtRoute(routeName, routeValues, result);
}

