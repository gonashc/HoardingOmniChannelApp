#!/usr/bin/env bash
# Wipes the Postgres volume so the init scripts (schema + seed) run again.
# Use this if you started the platform once with bad seed mounts and the DB
# is now empty/incomplete.
set -euo pipefail
cd "$(dirname "$0")/infrastructure/docker"
echo "Stopping containers..."
docker compose down
echo "Removing the postgres data volume..."
docker volume rm docker_postgres_data 2>/dev/null || \
docker volume rm hoardly_postgres_data 2>/dev/null || \
docker volume ls --format '{{.Name}}' | grep -E 'postgres_data$' | xargs -r docker volume rm
echo "Bringing containers back up (schema + seed will run on fresh DB)..."
docker compose up -d
echo "Done. Tail logs with: docker compose logs -f postgres"
