#!/bin/bash
set -e

echo "Setting up Citus cluster..."

# Wait for all nodes to be ready
until PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c '\q'; do
  echo "Waiting for coordinator..."
  sleep 2
done

until PGPASSWORD=postgres psql -h citus_worker1 -U postgres -c '\q'; do
  echo "Waiting for worker1..."
  sleep 2
done

until PGPASSWORD=postgres psql -h citus_worker2 -U postgres -c '\q'; do
  echo "Waiting for worker2..."
  sleep 2
done

# Check if nodes are already added
NODE_COUNT=$(PGPASSWORD=postgres psql -h citus_coordinator -U postgres -t -c "SELECT COUNT(*) FROM pg_dist_node;" | xargs)

# Only add worker nodes if they don't exist
if [ "$NODE_COUNT" -eq "0" ]; then
  echo "Adding worker nodes to the coordinator..."
  PGPASSWORD=postgres psql -h citus_coordinator -U postgres <<-EOSQL
    SELECT master_add_node('citus_worker1', 5432);
    SELECT master_add_node('citus_worker2', 5432);
EOSQL
else
  echo "Worker nodes already exist, skipping node addition..."
fi

# Verify the node configuration
echo "Verifying node configuration..."
PGPASSWORD=postgres psql -h citus_coordinator -U postgres -c "SELECT nodename, nodeport FROM pg_dist_node;"

echo "Citus cluster setup complete."