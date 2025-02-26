#!/bin/bash
set -e

echo "Creating optimized telemetry table for high-volume data..."

# Enable pgcrypto extension
PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "CREATE EXTENSION IF NOT EXISTS pgcrypto;"

# Clean up any existing objects that might cause conflicts
# First check if the table exists
TABLE_EXISTS=$(PGPASSWORD=postgres psql -h citus_coordinator -U postgres -t -c "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'telemetry_data');" | xargs)

if [ "$TABLE_EXISTS" = "t" ]; then
  # Drop dependent functions first
  echo "Dropping dependent functions..."
  PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "DROP FUNCTION IF EXISTS get_vehicle_telemetry(character varying, timestamp with time zone, timestamp with time zone);"
  PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "DROP FUNCTION IF EXISTS find_vehicles_near_location(double precision, double precision, double precision);"
  
  # Now drop the table
  echo "Dropping existing telemetry_data table..."
  PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "DROP TABLE IF EXISTS telemetry_data CASCADE;"
else
  echo "Table telemetry_data doesn't exist, proceeding with creation."
fi

# Create the telemetry table
echo "Creating telemetry_data table..."
PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "
CREATE TABLE telemetry_data (
    id UUID NOT NULL,
    vehicle_id VARCHAR(50) NOT NULL,
    latitude DOUBLE PRECISION NOT NULL,
    longitude DOUBLE PRECISION NOT NULL,
    speed DOUBLE PRECISION NOT NULL,
    battery_percentage DOUBLE PRECISION NOT NULL,
    battery_temperature DOUBLE PRECISION NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL,
    
    -- Make the primary key include the partition column
    PRIMARY KEY (vehicle_id, id),
    
    -- Add a unique constraint for vehicle_id + timestamp
    UNIQUE (vehicle_id, recorded_at)
);"

# Distribute the table
echo "Distributing the table..."
PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "SELECT create_distributed_table('telemetry_data', 'vehicle_id');"

# Optimize for write-heavy workloads
echo "Optimizing table settings..."
PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "
ALTER TABLE telemetry_data SET (
    autovacuum_vacuum_scale_factor = 0.05,
    autovacuum_analyze_scale_factor = 0.02
);"

# Create indexes
echo "Creating indexes..."
PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "CREATE INDEX idx_telemetry_vehicle_time ON telemetry_data (vehicle_id, recorded_at DESC);"
PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "CREATE INDEX idx_telemetry_vehicle_battery ON telemetry_data (vehicle_id, battery_percentage);"
PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "CREATE INDEX idx_telemetry_location ON telemetry_data (latitude, longitude);"

# Create a function to get vehicle telemetry within a time range
echo "Creating get_vehicle_telemetry function..."
PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "
CREATE OR REPLACE FUNCTION get_vehicle_telemetry(
    vehicle_id_param VARCHAR,
    start_time TIMESTAMPTZ,
    end_time TIMESTAMPTZ
) 
RETURNS SETOF telemetry_data AS 
\$\$
BEGIN
    RETURN QUERY
    SELECT *
    FROM telemetry_data
    WHERE vehicle_id = vehicle_id_param
      AND recorded_at BETWEEN start_time AND end_time
    ORDER BY recorded_at DESC;
END;
\$\$ LANGUAGE plpgsql;"

# Create a function to find vehicles near a location
echo "Creating find_vehicles_near_location function..."
PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "
CREATE OR REPLACE FUNCTION find_vehicles_near_location(
    lat DOUBLE PRECISION, 
    lon DOUBLE PRECISION,
    radius_km DOUBLE PRECISION
)
RETURNS TABLE(
    vehicle_id VARCHAR,
    latitude DOUBLE PRECISION,
    longitude DOUBLE PRECISION,
    distance_km DOUBLE PRECISION,
    recorded_at TIMESTAMPTZ
) AS
\$\$
BEGIN
    -- Simple distance calculation using the Haversine formula
    RETURN QUERY
    SELECT 
        t.vehicle_id,
        t.latitude,
        t.longitude,
        2 * 6371 * asin(sqrt(
            sin(radians(t.latitude - lat)/2)^2 + 
            cos(radians(lat)) * cos(radians(t.latitude)) * 
            sin(radians(t.longitude - lon)/2)^2
        )) as distance_km,
        t.recorded_at
    FROM 
        telemetry_data t
    WHERE 
        -- Fast coarse filter to limit candidates (about 111km per degree at equator)
        t.latitude BETWEEN lat - (radius_km/111.0) AND lat + (radius_km/111.0)
        AND t.longitude BETWEEN lon - (radius_km/111.0/cos(radians(lat))) AND lon + (radius_km/111.0/cos(radians(lat)))
        -- Move the distance calculation to WHERE clause instead of HAVING
        AND (2 * 6371 * asin(sqrt(
            sin(radians(t.latitude - lat)/2)^2 + 
            cos(radians(lat)) * cos(radians(t.latitude)) * 
            sin(radians(t.longitude - lon)/2)^2
        )) <= radius_km)
    ORDER BY 
        distance_km ASC, 
        t.recorded_at DESC;
END;
\$\$ LANGUAGE plpgsql;"

# Insert test data for multiple vehicles with the current timestamp
echo "Inserting test data..."
PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "
INSERT INTO telemetry_data (id, vehicle_id, latitude, longitude, speed, battery_percentage, battery_temperature, recorded_at)
VALUES 
    (gen_random_uuid(), 'vehicle-001', 52.5200, 13.4050, 45.5, 78.2, 24.5, '2025-02-26 15:36:51'),
    (gen_random_uuid(), 'vehicle-001', 52.5201, 13.4052, 42.0, 77.9, 24.7, '2025-02-26 15:36:52'),
    (gen_random_uuid(), 'vehicle-001', 52.5203, 13.4055, 40.5, 77.5, 24.8, '2025-02-26 15:35:53'),
    (gen_random_uuid(), 'vehicle-002', 48.8566, 2.3522, 38.2, 65.3, 23.1, '2025-02-26 15:36:54'),
    (gen_random_uuid(), 'vehicle-002', 48.8567, 2.3525, 39.1, 64.9, 23.3, '2025-02-26 15:36:55'),
    (gen_random_uuid(), 'vehicle-003', 40.7128, -74.0060, 22.5, 90.1, 22.0, '2025-02-26 15:36:56');"

# Show shard distribution information
echo "Shard distribution:"
PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "
SELECT 
  s.shardid, 
  n.nodename, 
  n.nodeport
FROM 
  pg_dist_shard s
JOIN 
  pg_dist_placement p ON s.shardid = p.shardid
JOIN 
  pg_dist_node n ON p.groupid = n.groupid
WHERE 
  s.logicalrelid = 'telemetry_data'::regclass
ORDER BY 
  s.shardid;"

# Show count per vehicle_id to verify sharding works
echo "Records per vehicle:"
PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "
SELECT vehicle_id, count(*) AS record_count
FROM telemetry_data 
GROUP BY vehicle_id
ORDER BY vehicle_id;"

# Test the get_vehicle_telemetry function
echo "Testing get_vehicle_telemetry function for vehicle-001:"
PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "
SELECT vehicle_id, latitude, longitude, speed, battery_percentage, recorded_at 
FROM get_vehicle_telemetry('vehicle-001', '2025-02-26 12:00:00', '2025-02-26 16:00:00');"

# Test the find_vehicles_near_location function
echo "Testing find_vehicles_near_location function (Berlin area):"
PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "
SELECT vehicle_id, latitude, longitude, distance_km, recorded_at 
FROM find_vehicles_near_location(52.52, 13.40, 10);"

echo "Telemetry table setup complete with all optimizations!"