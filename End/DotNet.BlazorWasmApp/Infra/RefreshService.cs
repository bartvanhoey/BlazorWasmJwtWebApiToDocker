using System.Net.Http.Json;
using DotNet.BlazorWasmApp.Models;
using DotNet.Shared;
using static System.String;
using static DotNet.BlazorWasmApp.Models.AuthRefreshMessage;

namespace DotNet.BlazorWasmApp.Infra;

public class RefreshService(
    IHttpClientFactory clientFactory,
    IJwtTokenService jwtTokenService,
    ILogoutService? logoutService = null)
{
    public async Task<AuthRefreshResult> RefreshAsync(string? accessToken, string? refreshToken)
    {
        if (IsNullOrWhiteSpace(accessToken) ) { return new AuthRefreshResult(InputAccessTokenNull); }
        if (IsNullOrWhiteSpace(refreshToken)) { return new AuthRefreshResult(InputRefreshTokenNull); }

        var httpClient = clientFactory.CreateClient("ApiHttpClient");
        
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/account/refresh", new RefreshInputModel(accessToken, refreshToken));
            if (response is { IsSuccessStatusCode: true })
            {
                var result = await response.Content.ReadFromJsonAsync<RefreshResult>();
                
                if (result == null) return new AuthRefreshResult(ResponseContentNull);
                if (IsNullOrWhiteSpace(result.AccessToken)) return new AuthRefreshResult(AccessTokenNull);
                if (IsNullOrWhiteSpace(result.RefreshToken)) return new AuthRefreshResult(RefreshTokenNull);
                if (!jwtTokenService.IsValid(result.AccessToken)) return new AuthRefreshResult(AccessTokenInvalid);

                await jwtTokenService.SaveAuthTokensAsync(result.AccessToken, result.RefreshToken);

                return new AuthRefreshResult(result.AccessToken, result.RefreshToken, result.ValidTo);
            }
        }
        catch (Exception)
        {
            if (logoutService != null) await logoutService.LogoutAsync();
            return new AuthRefreshResult(ExceptionThrown);
        }

        if (logoutService != null) await logoutService.LogoutAsync();
        return new AuthRefreshResult(SomethingWentWrong);
    }
}