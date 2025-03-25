using Microsoft.AspNetCore.Mvc;

namespace AnalyticsService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthCheckController : ControllerBase
{
    [HttpGet]
    public ActionResult<object> Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0"
        });
    }
}