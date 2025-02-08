using System.Net;
using System.Net.Http.Headers;
using static System.Console;
using static System.String;

namespace DotNet.BlazorWasmApp.Infra;

public class CustomAuthenticationHandler(
    IConfiguration configuration,
    IJwtTokenService jwtTokenService,
    IHttpClientFactory clientFactory
)
    : DelegatingHandler
{
    private bool _refreshing;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var (accessToken, refreshToken) = await jwtTokenService.GetAuthTokensAsync(cancellationToken);
        var isToServer = request.RequestUri?.AbsoluteUri.StartsWith(configuration["ApiUrl"] ?? "") ?? false;

        if (isToServer && !IsNullOrEmpty(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var iShouldRefresh = false;
        HttpResponseMessage? response = null;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
            iShouldRefresh = response.StatusCode == HttpStatusCode.Unauthorized;
            if (iShouldRefresh == false) return response;
        }
        catch (HttpRequestException)
        {
            iShouldRefresh = true;
        }
        catch (Exception e)
        {
            WriteLine(e);
        }

        if (_refreshing || IsNullOrEmpty(accessToken) || !iShouldRefresh) return response ?? throw new InvalidOperationException();

        try
        {
            _refreshing = true;
            var refreshResult = await new RefreshService(clientFactory, jwtTokenService).RefreshAsync(accessToken, refreshToken);
            if (refreshResult.Failure) return response ?? throw new InvalidOperationException();

            (accessToken, _) = await jwtTokenService.GetAuthTokensAsync(cancellationToken);

            if (jwtTokenService.IsValid(accessToken) && isToServer)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            _refreshing = false;
        }
    }
}