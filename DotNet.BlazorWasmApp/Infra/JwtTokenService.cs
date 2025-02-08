using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.IdentityModel.Tokens;
using static DotNet.BlazorWasmApp.Infra.ApplicationConsts;

namespace DotNet.BlazorWasmApp.Infra;

public class JwtTokenService(ILocalStorageService localStorage, IConfiguration configuration) : IJwtTokenService
{
    public async Task SaveAuthTokensAsync(string? accessToken, string? refreshToken)
    {
        await SaveAccessTokenAsync(accessToken);
        await SaveRefreshTokenAsync(refreshToken);
    }
    public async Task<(string? accessToken, string? refreshToken)> GetAuthTokensAsync(CancellationToken cancellationToken = default)
    {
       var accessToken = await GetAccessTokenAsync(cancellationToken);
       var refreshToken= await GetRefreshTokenAsync(cancellationToken);
       return (accessToken, refreshToken);
    }

    public async Task RemoveAuthTokensAsync()
    {
        await RemoveAccessTokenAsync();
        await RemoveRefreshTokenAsync();
    }

    private async Task SaveAccessTokenAsync(string? accessToken) 
        => await localStorage.SetItemAsync( AccessToken, accessToken);

    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        => await localStorage.GetItemAsync<string>(AccessToken, cancellationToken);

    private async Task RemoveAccessTokenAsync() 
        => await localStorage.RemoveItemAsync(AccessToken);

    private async Task SaveRefreshTokenAsync(string? refreshToken)
        => await localStorage.SetItemAsync(RefreshToken, refreshToken);

    private async Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default)
        => await localStorage.GetItemAsync<string>(RefreshToken, cancellationToken);

    private async Task RemoveRefreshTokenAsync() 
        => await localStorage.RemoveItemAsync(RefreshToken);

    public bool IsValid(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken)) return false;
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = GetValidationParameters();

        // ReSharper disable once NotAccessedOutParameterVariable
        SecurityToken securityToken;
        try
        {
            // ReSharper disable once UnusedVariable
            IPrincipal principal = tokenHandler.ValidateToken(accessToken, validationParameters, out securityToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
        return true;
    }

    public bool IsInvalid(string? accessToken) => !IsValid(accessToken);

    public IEnumerable<Claim> GetClaims(string accessToken)
    {
        var claims = new List<Claim>();
        var payload = accessToken.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs == null) return claims;

        if (keyValuePairs.TryGetValue(ClaimTypes.Role, out var roles))
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (roles != null)
            {
                if ("[".StartsWith(roles.ToString()!.Trim()))
                {
                    var parsedRoles = JsonSerializer.Deserialize<string[]>(roles.ToString()!);
                    if (parsedRoles == null) return claims;
                    claims.AddRange(parsedRoles.Select(parsedRole => new Claim(ClaimTypes.Role, parsedRole)));
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, roles.ToString()!));
                }

                keyValuePairs.Remove(ClaimTypes.Role);
            }
        }

        claims.AddRange(keyValuePairs.Select(kvp =>
            new Claim(kvp.Key, kvp.Value.ToString() ?? throw new InvalidOperationException())));
        return claims;
    }
    
    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }

        return Convert.FromBase64String(base64);
    }


    private TokenValidationParameters GetValidationParameters() =>
        new()
        {
            ValidateLifetime = true, 
            ValidateAudience = true, 
            ValidateIssuer = true,  
            ValidIssuer = configuration["Jwt:ValidIssuer"],
            ValidAudience = configuration["Jwt:ValidAudience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecurityKey"] ?? throw new InvalidOperationException()))  // The same key as the one that generate the token
        };
}

public static class ApplicationConsts
{
    public static readonly string AccessToken = "accessToken";
    public static readonly string RefreshToken = "refreshToken";
    
}