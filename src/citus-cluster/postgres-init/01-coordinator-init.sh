#!/bin/bash
set -e

# Create Citus extension
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
  CREATE EXTENSION IF NOT EXISTS citus;
EOSQL

# Configure pg_hba.conf for trust authentication within the cluster
echo "host all all 172.25.0.0/16 trust" >> /var/lib/postgresql/data/pg_hba.conf
echo "host all postgres all trust" >> /var/lib/postgresql/data/pg_hba.conf

# Reload PostgreSQL configuration
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
  SELECT pg_reload_conf();
EOSQL

# The initialization is complete
echo "Coordinator node initialized with Citus extension and trust authentication"