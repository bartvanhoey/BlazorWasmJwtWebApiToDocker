using DotNet.JwtWebApi.Models;
using DotNet.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using static System.Activator;
using static System.Text.Encoding;
using static Microsoft.AspNetCore.WebUtilities.WebEncoders;

namespace DotNet.JwtWebApi.Controllers;

[ApiController]
[Route("api/account")]
public class RegisterController(UserManager<ApplicationUser> userManager) : ControllerBase
#pragma warning disable CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
    
#pragma warning restore CS9107 // Parameter is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
{
    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody] RegisterInputModel model)
    {
        try
        {
            if (IsNullOrEmpty(model.Email)) return StatusCode(500, "Email is null");
            if (IsNullOrEmpty(model.Password)) return StatusCode(500, "Password is null");

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user != null) return StatusCode(500, "User already exists");

            var newUser = CreateApplicationUser();
            if (newUser == null) return StatusCode(500, "Could not create user");

            newUser.Email = model.Email;
            newUser.UserName = model.Email;

            var createUserResult = await userManager.CreateAsync(newUser, model.Password);
            if (!createUserResult.Succeeded) return StatusCode(500, "Could not create user");
            
            var userId = await userManager.GetUserIdAsync(newUser);
            if (IsNullOrWhiteSpace(userId)) return StatusCode(500, "Could not get user id");
            
            var code = await userManager.GenerateEmailConfirmationTokenAsync(newUser);
            code = Base64UrlEncode(UTF8.GetBytes(code));
            
            return Ok(new RegisterResponse(code, userId));
        }
        catch (Exception exception)
        {
            return StatusCode(500, exception.Message);
        }
    }
    
    private static ApplicationUser? CreateApplicationUser()
    {
        try
        {
            return CreateInstance<ApplicationUser>();
        }
        catch
        {
            return null;
        }
    }

    
}