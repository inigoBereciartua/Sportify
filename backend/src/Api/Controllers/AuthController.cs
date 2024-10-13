using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    [HttpGet("login")]
    public IActionResult Login()
    {
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = "http://localhost:8080/callback"
        };
        authenticationProperties.Items["show_dialog"] = "true";

        return Challenge(authenticationProperties, "Spotify");
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        Console.WriteLine("User logged out.");
        return Redirect("/");
    }
}
