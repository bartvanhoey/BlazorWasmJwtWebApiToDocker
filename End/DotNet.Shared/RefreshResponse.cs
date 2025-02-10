
// ReSharper disable MemberCanBePrivate.Global

namespace DotNet.Shared;

public class RefreshResponse  {

    public RefreshResponse() 
    {
        // do not remove    
    }

    public RefreshResponse(string? accessToken, string refreshToken, DateTime validTo) : this()
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ValidTo = validTo;
    }
    
    public string? AccessToken { get; set; } 
    public string? RefreshToken { get; set; }
    public DateTime ValidTo { get; set; }
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    
    
}