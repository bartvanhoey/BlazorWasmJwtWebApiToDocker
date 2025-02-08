// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace DotNet.Shared;

public class RegisterResponse 
{
    public RegisterResponse()
    {
        // do not remove
    }

    public RegisterResponse(string? code, string? userId) : this()
    {
        Code = code;
        UserId = userId;
    }
    
    
    public string? Code { get; set; }
    public string? UserId { get; set; }
}