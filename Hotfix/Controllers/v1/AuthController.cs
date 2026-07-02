using Microsoft.AspNetCore.Mvc;

namespace Hotfix.Controllers.V1;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login()
    {
        return Ok(new {});
    }

    [HttpPost("register")]
    public IActionResult Register()
    {
        return Ok(new {});
    }
}
