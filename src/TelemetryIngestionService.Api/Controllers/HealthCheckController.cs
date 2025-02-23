using Microsoft.AspNetCore.Mvc;

namespace TelemetryIngestionService.Api.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthCheckController : ControllerBase
    {
        [HttpGet]
        public IActionResult CheckHealth() => Ok("Telemetry Ingestion Service is running.");
    }
}