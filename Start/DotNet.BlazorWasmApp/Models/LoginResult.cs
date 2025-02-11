namespace DotNet.BlazorWasmApp.Models;

public class LoginResult 
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime ValidTo { get; set; }
}