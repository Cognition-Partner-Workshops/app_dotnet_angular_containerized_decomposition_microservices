using Microsoft.AspNetCore.Mvc;

namespace KnowledgeAgent.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "SharePoint Knowledge Agent",
            version = "1.0.0",
            timestamp = DateTime.UtcNow
        });
    }
}
