using AnalyticsService.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace AnalyticsService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController(Services.AnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("vehicles")]
    public async Task<ActionResult<List<VehicleUsageStatisticsDto>>> GetAllVehicles()
    {
        var results = await analyticsService.GetAllVehicleUsageStatisticsAsync();
        return Ok(results);
    }

    [HttpGet("vehicles/active")]
    public async Task<ActionResult<List<VehicleUsageStatisticsDto>>> GetActiveVehicles([FromQuery] int hours = 24)
    {
        if (hours <= 0 || hours > 168)
        {
            return BadRequest("Hours must be between 1 and 168");
        }
        
        var results = await analyticsService.GetActiveVehiclesAsync(hours);
        return Ok(results);
    }

    [HttpGet("vehicles/{vehicleId}/battery")]
    public async Task<ActionResult<VehicleBatteryStatisticsDto>> GetVehicleBatteryStatistics(string vehicleId)
    {
        var result = await analyticsService.GetVehicleBatteryStatisticsAsync(vehicleId);
        
        if (result == null)
        {
            return NotFound($"No battery statistics found for vehicle ID: {vehicleId}");
        }
        
        return Ok(result);
    }

    [HttpGet("vehicles/{vehicleId}/usage")]
    public async Task<ActionResult<VehicleUsageStatisticsDto>> GetVehicleUsageStatistics(string vehicleId)
    {
        var result = await analyticsService.GetVehicleUsageStatisticsAsync(vehicleId);
        
        if (result == null)
        {
            return NotFound($"No usage statistics found for vehicle ID: {vehicleId}");
        }
        
        return Ok(result);
    }

    [HttpGet("vehicles/{vehicleId}/hourly")]
    public async Task<ActionResult<List<HourlyTelemetryAggregateDto>>> GetVehicleHourlyStats(
        string vehicleId, 
        [FromQuery] DateTime? startDate = null, 
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-1);
        var end = endDate ?? DateTime.UtcNow;
        
        if (end < start)
        {
            return BadRequest("End date must be after start date");
        }
        
        var results = await analyticsService.GetVehicleHourlyStatsAsync(vehicleId, start, end);
        return Ok(results);
    }

    [HttpGet("vehicles/nearby")]
    public async Task<ActionResult<List<VehicleNearbyDto>>> GetVehiclesNearLocation(
        [FromQuery] double latitude, 
        [FromQuery] double longitude, 
        [FromQuery] double radiusKm = 5.0)
    {
        if (radiusKm <= 0 || radiusKm > 100)
        {
            return BadRequest("Radius must be between 0.1 and 100 km");
        }
        
        var results = await analyticsService.FindVehiclesNearLocationAsync(latitude, longitude, radiusKm);
        return Ok(results);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult> RefreshMaterializedViews()
    {
        var success = await analyticsService.RefreshMaterializedViewsAsync();
        
        if (!success)
        {
            return StatusCode(500, "Failed to refresh materialized views");
        }
        
        return Ok(new { success = true, message = "Materialized views refreshed successfully" });
    }
}