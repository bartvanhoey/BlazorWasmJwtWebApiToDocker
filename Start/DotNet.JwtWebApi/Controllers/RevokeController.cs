using DotNet.JwtWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace DotNet.JwtWebApi.Controllers;

[Route("api/account")]
[ApiController]
public class RevokeController(UserManager<ApplicationUser> userManager, ILogger<RevokeController> logger) : ControllerBase
{
    [Authorize]
    [HttpDelete("Revoke")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status401Unauthorized)]
    public async Task<IActionResult> Revoke()
    {
        try
        {
            var username = HttpContext.User.Identity?.Name;
            if (username is null) return Unauthorized();

            var user = await userManager.FindByNameAsync(username);
            if (user is null) return Unauthorized();
 
            user.RefreshToken = null;
            await userManager.UpdateAsync(user);
            return Ok();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, nameof(Revoke));
            return Ok();
        }
    }
}