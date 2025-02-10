using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace DotNet.JwtWebApi.Models;

public static class ApplicationUserExtensions
{
    public static ApplicationUser SetRefreshToken(this ApplicationUser user, IConfiguration configuration)
    {
        var refreshTokenExpiryInHours = 24;
        if (int.TryParse(configuration["Jwt:RefreshTokenExpiryInHours"] ?? "24", out var expiry))
            refreshTokenExpiryInHours = expiry;

        var randomNumber = new byte[64];
        using var generator = RandomNumberGenerator.Create();
        generator.GetBytes(randomNumber);

        var refreshToken = Convert.ToBase64String(randomNumber);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddHours(refreshTokenExpiryInHours);
        return user;
    }


    public static async Task<(string accessToken, DateTime validTo)> GenerateAccessToken(this ApplicationUser user,
        UserManager<ApplicationUser> userManager, IConfiguration configuration, string origin)
    {
        var authClaims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Email ?? throw new InvalidOperationException()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var userRoles = await userManager.GetRolesAsync(user);
        if (userRoles is { Count: > 0 })
            authClaims.AddRange(userRoles.Select(userRole => new Claim(ClaimTypes.Role, userRole)));

        var expiryInSeconds = configuration["Jwt:AccessTokenExpiryInSeconds"] ??
                              throw new InvalidOperationException("AccessTokenExpiryInSeconds not set");

        var jwtValidIssuer = configuration["Jwt:ValidIssuer"] ??
                             throw new InvalidOperationException("ValidIssuer not set");
         
        var jwtValidAudience  = Empty ;
        var validAudiences = configuration.GetSection("Jwt:ValidAudiences").Get<List<string>>();
        if (validAudiences is { Count: > 0 } &&
            validAudiences.Contains(origin ?? throw new InvalidOperationException())) jwtValidAudience = origin; 
        
        jwtValidAudience = jwtValidAudience ?? throw new InvalidOperationException("ValidAudience not set");
        
        var jwtSecurityKey = configuration["Jwt:SecurityKey"] ??
                             throw new InvalidOperationException("SecurityKey not set");

        var token = new JwtSecurityToken(
            jwtValidIssuer,
            jwtValidAudience,
            expires: DateTime.UtcNow.AddSeconds(double.Parse(expiryInSeconds)), // 1 hour = 3600 sec
            claims: authClaims,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecurityKey)),
                SecurityAlgorithms.HmacSha256)
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), token.ValidTo);
    }
}