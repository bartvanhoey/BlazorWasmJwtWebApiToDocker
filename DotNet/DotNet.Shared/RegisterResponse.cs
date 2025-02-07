namespace DotNet.Shared;

public class RegisterResponse 
{
    public RegisterResponse()
    {
        // do not remove
    }

    public RegisterResponse(string? code, string? token) : this()
    {
        Code = code;
        Token = token;
    }

    
    public string? Code { get; set; }
    public string? Token { get; set; }
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    
    
}