using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace DotNet.BlazorWasmApp.Infra;


public class LogoutService(IHttpClientFactory clientFactory, ILocalStorageService localStorage, AuthenticationStateProvider authenticationStateProvider)
    : ILogoutService
{
    public async Task LogoutAsync()
    {
        
        var httpClient = clientFactory.CreateClient("ApiHttpClient");
        try
        {
            await httpClient.DeleteAsync("api/account/revoke");
        }
        catch(Exception)
        {
            // ...
        }
        await localStorage.RemoveItemAsync("accessToken");
        await localStorage.RemoveItemAsync("refreshToken");
        await authenticationStateProvider.GetAuthenticationStateAsync();
    }
}