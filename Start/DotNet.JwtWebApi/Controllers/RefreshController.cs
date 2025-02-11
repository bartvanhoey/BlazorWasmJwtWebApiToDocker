using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotNet.JwtWebApi.Models;
using DotNet.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace DotNet.JwtWebApi.Controllers;

[Route("api/account")]
[ApiController]
public class RefreshController(
    UserManager<ApplicationUser> userManager, IConfiguration configuration)
#pragma warning disable CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
    : ControllerBase
#pragma warning restore CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
{
    [HttpPost("Refresh")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status500InternalServerError)]
    public async Task<IActionResult> Refresh([FromBody] RefreshInputModel model)
    {
        try
        {
            if (IsNullOrWhiteSpace(model.AccessToken)) return StatusCode(400, "Access token empty");
            if (IsNullOrWhiteSpace(model.RefreshToken)) return StatusCode(400, "Refresh token empty");

            var origin = HttpContext.Request.Headers.Origin.FirstOrDefault();
            var principal = GetPrincipalFromExpiredToken(model.AccessToken, configuration, origin);
            if (principal?.Identity?.Name is null) return StatusCode(500, "Could not get user name");

            var user = await userManager.FindByNameAsync(principal.Identity.Name);
            if (user == null) return StatusCode(400, "User not found");

            if (user.RefreshToken != model.RefreshToken || user.RefreshTokenExpiry < DateTime.UtcNow)
                return StatusCode(500, "Refresh token expired");

            if (IsNullOrWhiteSpace(origin)) return StatusCode(400, "Origin is null or whitespace");

            var (accessToken, validTo) = await user.GenerateAccessToken(userManager, configuration, origin);

            return Ok(new RefreshResponse(accessToken, model.RefreshToken, validTo));
        }
        catch (Exception exception)
        {
            return StatusCode(500, exception.Message);
        }
    }

    private static ClaimsPrincipal? GetPrincipalFromExpiredToken(string token, IConfiguration configuration,
        string? origin)
    {

        
        
        var validIssuer = configuration["Jwt:ValidIssuer"] ??
                             throw new InvalidOperationException("ValidIssuer not set");
         
        var validAudience  = string.Empty ;
        var validAudiences = configuration.GetSection("Jwt:ValidAudiences").Get<List<string>>();
        if (validAudiences is { Count: > 0 } &&
            validAudiences.Contains(origin ?? throw new InvalidOperationException())) validAudience = origin; 
        
        validAudience = validAudience ?? throw new InvalidOperationException("ValidAudience not set");
        
        var securityKey = configuration["Jwt:SecurityKey"] ??
                             throw new InvalidOperationException("SecurityKey not set");
        
        var validation = new TokenValidationParameters
        {
            ValidIssuer = validIssuer,
            ValidAudience = validAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey)),
            ValidateLifetime = false
        };
        return new JwtSecurityTokenHandler().ValidateToken(token, validation, out _);
    }
}