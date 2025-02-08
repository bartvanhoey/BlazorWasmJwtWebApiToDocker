

// ReSharper disable MemberCanBePrivate.Global

namespace DotNet.Shared;

public class LoginResponse 
{
    public LoginResponse()
    {
        // do not remove
    }

    public LoginResponse(string? accessToken, string refreshToken, DateTime validTo) : this()
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ValidTo = validTo;
        StatusCode = 200;
    }

    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime ValidTo { get; set; }
    public int StatusCode { get; set; }
    public string? Message { get; set; }

    
}