using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using static System.Threading.Tasks.Task;

namespace DotNet.BlazorWasmApp.Infra
{
    public class CustomAuthenticationStateProvider(IHttpClientFactory clientFactory, IJwtTokenService jwtTokenService) : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _unAuthenticated = new(new ClaimsIdentity());

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var authTokens = await jwtTokenService.GetAuthTokensAsync();
                var refreshService = new RefreshService(clientFactory, jwtTokenService);
                
                var refreshResult = await refreshService.RefreshAsync(authTokens.accessToken, authTokens.refreshToken);
                if (refreshResult.Success)
                {
                    var authStateResult = await UpdateAuthenticationState(refreshResult.AccessToken, refreshResult.RefreshToken);
                    if (authStateResult == false) throw new InvalidOperationException();
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(jwtTokenService.GetClaims(refreshResult.AccessToken ?? throw new InvalidOperationException()), "jwt")));
                }

                await MarkUserAsLoggedOut();
                return new AuthenticationState(_unAuthenticated);
            }
            catch { return new AuthenticationState(_unAuthenticated); }
        }

        public async Task<bool> UpdateAuthenticationState(string? accessToken, string? refreshToken)
        {
            try
            {
                if ( jwtTokenService.IsValid(accessToken))
                {
                    await jwtTokenService.SaveAuthTokensAsync(accessToken, refreshToken);
                    NotifyAuthenticationStateChanged(FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(jwtTokenService.GetClaims(accessToken ?? throw new InvalidOperationException()), "jwt")))));
                    return true;
                }

                await jwtTokenService.RemoveAuthTokensAsync();
                NotifyAuthenticationStateChanged(FromResult(new AuthenticationState(_unAuthenticated)));
                return false;
            }
            catch { NotifyAuthenticationStateChanged(FromResult(new AuthenticationState(_unAuthenticated))); }
            return false;
        }

        private async Task MarkUserAsLoggedOut()
        {
            await jwtTokenService.RemoveAuthTokensAsync();
            NotifyAuthenticationStateChanged(FromResult(new AuthenticationState(_unAuthenticated)));
        }
    }
}