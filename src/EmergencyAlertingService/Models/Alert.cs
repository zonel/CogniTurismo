namespace EmergencyAlertingService.Models
{
    public record Alert(
        Guid Id,
        Guid TelemetryId,
        string VehicleId,
        int Type,
        double Value,
        int Severity,
        DateTime DetectedAt
    );
}