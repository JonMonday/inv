using InvServer.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvServer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class SystemController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new ApiResponse<string> { Data = "Healthy" });
    }

    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        return Ok(new ApiResponse<string> { Data = "1.0.0" });
    }
}
