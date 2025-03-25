namespace AnalyticsService.Dtos;

public record VehicleBatteryStatisticsDto(
    string VehicleId,
    double AvgBatteryPercentage,
    double MinBatteryPercentage,
    double MaxBatteryPercentage,
    double AvgBatteryTemperature,
    DateTime LastUpdated
);

public record VehicleUsageStatisticsDto(
    string VehicleId,
    double TotalDistanceKm,
    double AvgSpeed,
    double MaxSpeed,
    double LastLocationLat,
    double LastLocationLon,
    DateTime LastActive,
    DateTime LastUpdated
);

public record HourlyTelemetryAggregateDto(
    DateTime HourStart,
    string VehicleId,
    int DataPoints,
    double AvgSpeed,
    double AvgBatteryPercentage,
    double AvgBatteryTemperature,
    double DistanceTraveledKm,
    DateTime LastUpdated
);

public record VehicleNearbyDto(
    string VehicleId,
    double Latitude,
    double Longitude,
    double DistanceKm,
    DateTime RecordedAt
);

public record DateRangeDto(DateTime StartDate, DateTime EndDate);