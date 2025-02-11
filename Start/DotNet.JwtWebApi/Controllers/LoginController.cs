using DotNet.JwtWebApi.Models;
using DotNet.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace DotNet.JwtWebApi.Controllers;

[Route("api/account")]
[ApiController]
public class LoginController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost]
    [Route("login")]
    [ProducesResponseType(Status200OK, Type = typeof(LoginResponse))]
    [ProducesResponseType(Status500InternalServerError)]
    [ProducesResponseType(Status423Locked)]
    public async Task<IActionResult> Login([FromBody] LoginInputModel? model)
    {
        try
        {
            var user = await userManager.FindByEmailAsync(model?.Email ?? string.Empty);
            if (user == null) return StatusCode(404, "User not found");

            if (IsNullOrWhiteSpace(user.Email)) return StatusCode(400, "Email is null or whitespace");
            if (IsNullOrWhiteSpace(model?.Password)) return StatusCode(400, "Password is null or whitespace");

            var signInResult = await signInManager.CheckPasswordSignInAsync(user,
                model.Password ?? throw new InvalidOperationException(), true);
            if (signInResult is { Succeeded: false, IsLockedOut: true }) return StatusCode(423, "User is locked out");

            if (!signInResult.Succeeded) return StatusCode(401, "Invalid credentials");

            user.SetRefreshToken(configuration);

            await userManager.UpdateAsync(user);

            var origin = HttpContext.Request.Headers.Origin.FirstOrDefault();
            if (IsNullOrWhiteSpace(origin)) return StatusCode(400, "Origin is null or whitespace");

            var (accessToken, validTo) = await user.GenerateAccessToken(userManager, configuration, origin);

            if (IsNullOrWhiteSpace(user.RefreshToken)) return StatusCode(500, "RefreshToken is null or whitespace");
            if (IsNullOrWhiteSpace(accessToken)) return StatusCode(500, "AccessToken is null or whitespace");

            return validTo <= DateTime.UtcNow 
                ? StatusCode(500, "ValidTo is less than or equal to DateTime.UtcNow") 
                : Ok(new LoginResponse(accessToken, user.RefreshToken, validTo));
        }
        catch (Exception exception)
        {
            return StatusCode(500, exception.Message);
        }
    }
}