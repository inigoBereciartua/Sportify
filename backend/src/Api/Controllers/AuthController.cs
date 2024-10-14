using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string redirectUri)
    {
        var authenticationProperties = new AuthenticationProperties
        {
            RedirectUri = redirectUri
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
