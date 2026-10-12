using Greenhouse.API.Services;
using Greenhouse.API.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Greenhouse.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(TokenService tokenService, IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var adminUser = configuration["AdminCredentials:Username"];
        var adminPass = configuration["AdminCredentials:Password"];

        if (model.Username != adminUser || model.Password != adminPass)
            return Unauthorized(new { message = "Credenciais inválidas" });

        var (token, expiration) = tokenService.GenerateToken(model.Username);

        return Ok(new TokenResponseViewModel { Token = token, Expiration = expiration });
    }
}
