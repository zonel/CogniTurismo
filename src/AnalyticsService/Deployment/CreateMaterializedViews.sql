-- Battery statistics materialized view
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_vehicle_battery_statistics AS
SELECT
    vehicle_id,
    AVG(battery_percentage) AS avg_battery_percentage,
    MIN(battery_percentage) AS min_battery_percentage,
    MAX(battery_percentage) AS max_battery_percentage,
    AVG(battery_temperature) AS avg_battery_temperature,
    NOW() AS last_updated
FROM
    telemetry_data
GROUP BY
    vehicle_id;

-- Vehicle usage statistics materialized view
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_vehicle_usage_statistics AS
WITH latest_positions AS (
    SELECT DISTINCT ON (vehicle_id) 
        vehicle_id, 
        latitude, 
        longitude, 
        recorded_at
    FROM 
        telemetry_data
    ORDER BY 
        vehicle_id, recorded_at DESC
),
distance_calc AS (
    SELECT 
        t1.vehicle_id,
        SUM(
            2 * 6371 * asin(sqrt(
                sin(radians(t2.latitude - t1.latitude)/2)^2 + 
                cos(radians(t1.latitude)) * cos(radians(t2.latitude)) * 
                sin(radians(t2.longitude - t1.longitude)/2)^2
            ))
        ) AS total_distance_km
    FROM 
        telemetry_data t1
    JOIN 
        telemetry_data t2
    ON 
        t1.vehicle_id = t2.vehicle_id AND
        t1.recorded_at < t2.recorded_at
    GROUP BY 
        t1.vehicle_id
)
SELECT
    t.vehicle_id,
    COALESCE(d.total_distance_km, 0) AS total_distance_km,
    AVG(t.speed) AS avg_speed,
    MAX(t.speed) AS max_speed,
    lp.latitude AS last_location_lat,
    lp.longitude AS last_location_lon,
    lp.recorded_at AS last_active,
    NOW() AS last_updated
FROM
    telemetry_data t
        LEFT JOIN
    distance_calc d ON t.vehicle_id = d.vehicle_id
        JOIN
    latest_positions lp ON t.vehicle_id = lp.vehicle_id
GROUP BY
    t.vehicle_id, d.total_distance_km, lp.latitude, lp.longitude, lp.recorded_at;

-- Hourly telemetry aggregates materialized view
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_hourly_telemetry_aggregates AS
WITH hourly_groups AS (
    SELECT 
        date_trunc('hour', recorded_at) AS hour_start,
        vehicle_id,
        COUNT(*) AS data_points,
        AVG(speed) AS avg_speed,
        AVG(battery_percentage) AS avg_battery_percentage,
        AVG(battery_temperature) AS avg_battery_temperature
    FROM 
        telemetry_data
    GROUP BY 
        date_trunc('hour', recorded_at), vehicle_id
),
hourly_distances AS (
    SELECT 
        date_trunc('hour', t1.recorded_at) AS hour_start,
        t1.vehicle_id,
        SUM(
            2 * 6371 * asin(sqrt(
                sin(radians(t2.latitude - t1.latitude)/2)^2 + 
                cos(radians(t1.latitude)) * cos(radians(t2.latitude)) * 
                sin(radians(t2.longitude - t1.longitude)/2)^2
            ))
        ) AS distance_traveled_km
    FROM 
        telemetry_data t1
    JOIN 
        telemetry_data t2
    ON 
        t1.vehicle_id = t2.vehicle_id AND
        t1.recorded_at < t2.recorded_at AND
        t2.recorded_at <= date_trunc('hour', t1.recorded_at) + interval '1 hour'
    GROUP BY 
        date_trunc('hour', t1.recorded_at), t1.vehicle_id
)
SELECT
    hg.hour_start,
    hg.vehicle_id,
    hg.data_points,
    hg.avg_speed,
    hg.avg_battery_percentage,
    hg.avg_battery_temperature,
    COALESCE(hd.distance_traveled_km, 0) AS distance_traveled_km,
    NOW() AS last_updated
FROM
    hourly_groups hg
        LEFT JOIN
    hourly_distances hd ON hg.hour_start = hd.hour_start AND hg.vehicle_id = hd.vehicle_id;

-- Create indexes for materialized views
CREATE UNIQUE INDEX IF NOT EXISTS idx_mv_vehicle_battery_stats_vehicleid ON mv_vehicle_battery_statistics (vehicle_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_mv_vehicle_usage_stats_vehicleid ON mv_vehicle_usage_statistics (vehicle_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_mv_hourly_telemetry_hour_vehicle ON mv_hourly_telemetry_aggregates (hour_start, vehicle_id);