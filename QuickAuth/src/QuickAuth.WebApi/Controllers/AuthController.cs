using Microsoft.AspNetCore.Mvc;

namespace QuickAuth.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAuth()
    {
        return Ok(new { message = "Acesso Validado!" });
    }
}