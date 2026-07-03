using Entity.DTOs;
using Hotfix.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hotfix.Controllers.V1;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService = new();

    [HttpPost("login")]
    public IActionResult Login()
    {
        return Ok(new {});
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        var (success, message) = _authService.Register(request);

        if (!success)
        {
            return BadRequest(new { code = (int)ResponseCode.Success, message });
        }

        return Ok(new { code = (int)ResponseCode.Success, message });
    }
}
