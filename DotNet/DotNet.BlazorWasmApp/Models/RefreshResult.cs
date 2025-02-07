namespace DotNet.BlazorWasmApp.Models;

public class RefreshResult 
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime ValidTo { get; set; }

    public string? Message { get; set; }
    
    public int StatusCode { get; set; }
    
    
}