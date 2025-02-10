using System.Security.Claims;

namespace DotNet.BlazorWasmApp.Infra;

public interface IJwtTokenService
{
    
    Task SaveAuthTokensAsync(string? accessToken, string? refreshToken);
    Task<(string? accessToken, string? refreshToken)> GetAuthTokensAsync(CancellationToken cancellationToken = default);
    Task RemoveAuthTokensAsync();
    
    // Task SaveAccessTokenAsync(string accessToken);
    // Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    // Task RemoveAccessTokenAsync(); 
    // Task SaveRefreshTokenAsync(string refreshToken);
    // Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default);
    // Task RemoveRefreshTokenAsync();
    bool IsValid(string? accessToken);
    bool IsInvalid(string? accessToken);
    IEnumerable<Claim> GetClaims(string accessToken);
}