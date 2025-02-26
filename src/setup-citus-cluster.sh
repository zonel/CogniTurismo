#!/bin/bash
set -e

echo "Setting up Citus cluster with proper authentication..."

# Create a Docker network for the Citus cluster
docker network create citus-network || true

# Launch the coordinator node
docker run -d --name citus_coordinator \
    --network citus-network \
    -p 5432:5432 \
    -e POSTGRES_PASSWORD=postgres \
    -e POSTGRES_HOST_AUTH_METHOD=trust \
    citusdata/citus:12.0

# Launch two worker nodes
docker run -d --name citus_worker1 \
    --network citus-network \
    -e POSTGRES_PASSWORD=postgres \
    -e POSTGRES_HOST_AUTH_METHOD=trust \
    citusdata/citus:12.0

docker run -d --name citus_worker2 \
    --network citus-network \
    -e POSTGRES_PASSWORD=postgres \
    -e POSTGRES_HOST_AUTH_METHOD=trust \
    citusdata/citus:12.0

# Wait for the coordinator to be ready
echo "Waiting for coordinator to be ready..."
until docker exec citus_coordinator pg_isready -U postgres; do
  sleep 1
done

# Setup Citus extension on the coordinator
echo "Setting up Citus extension on coordinator..."
docker exec citus_coordinator psql -U postgres -c "CREATE EXTENSION IF NOT EXISTS citus;"

# Get the IP addresses of the worker nodes
echo "Getting worker1 IP..."
WORKER1_IP=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' citus_worker1)
echo "Getting worker2 IP..."
WORKER2_IP=$(docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' citus_worker2)

# Wait for the worker nodes to be ready
echo "Waiting for worker1 to be ready..."
until docker exec citus_worker1 pg_isready -U postgres; do
  sleep 1
done

echo "Waiting for worker2 to be ready..."
until docker exec citus_worker2 pg_isready -U postgres; do
  sleep 1
done

# Setup Citus extension on the worker nodes
echo "Setting up Citus extension on worker1..."
docker exec citus_worker1 psql -U postgres -c "CREATE EXTENSION IF NOT EXISTS citus;"
echo "Setting up Citus extension on worker2..."
docker exec citus_worker2 psql -U postgres -c "CREATE EXTENSION IF NOT EXISTS citus;"

# Configure pg_hba.conf to trust connections from all nodes in the cluster
echo "Configuring pg_hba.conf on all nodes for trust authentication..."

# Update pg_hba.conf on coordinator
docker exec citus_coordinator bash -c "echo 'host all postgres all trust' >> /var/lib/postgresql/data/pg_hba.conf"
docker exec citus_coordinator bash -c "echo 'host all all 172.18.0.0/16 trust' >> /var/lib/postgresql/data/pg_hba.conf"

# Update pg_hba.conf on worker1
docker exec citus_worker1 bash -c "echo 'host all postgres all trust' >> /var/lib/postgresql/data/pg_hba.conf"
docker exec citus_worker1 bash -c "echo 'host all all 172.18.0.0/16 trust' >> /var/lib/postgresql/data/pg_hba.conf"

# Update pg_hba.conf on worker2
docker exec citus_worker2 bash -c "echo 'host all postgres all trust' >> /var/lib/postgresql/data/pg_hba.conf"
docker exec citus_worker2 bash -c "echo 'host all all 172.18.0.0/16 trust' >> /var/lib/postgresql/data/pg_hba.conf"

# Reload PostgreSQL configuration on all nodes
echo "Reloading PostgreSQL configuration on all nodes..."
docker exec citus_coordinator psql -U postgres -c "SELECT pg_reload_conf();"
docker exec citus_worker1 psql -U postgres -c "SELECT pg_reload_conf();"
docker exec citus_worker2 psql -U postgres -c "SELECT pg_reload_conf();"

# Add the workers to the coordinator
echo "Adding workers to coordinator..."
docker exec citus_coordinator psql -U postgres -c "
DO \$\$
BEGIN
    -- Remove existing nodes to avoid duplicates
    PERFORM master_remove_node(nodename, nodeport) 
    FROM pg_dist_node 
    WHERE nodename IN ('$WORKER1_IP', '$WORKER2_IP');
    
    -- Add workers
    PERFORM master_add_node('$WORKER1_IP', 5432);
    PERFORM master_add_node('$WORKER2_IP', 5432);
END \$\$;"

# Verify the node configuration
echo "Verifying node configuration..."
docker exec citus_coordinator psql -U postgres -c "SELECT nodename, nodeport FROM pg_dist_node;"

echo "Citus cluster setup complete."