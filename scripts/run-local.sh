#!/usr/bin/env bash
# Runs all services + the gateway directly with `dotnet`, against Postgres/RabbitMQ/Redis on localhost.
# Use this when Docker is not available. Infra defaults: postgres:5432 (user postgres), rabbitmq:5672 (guest/guest), redis:6379.
set -euo pipefail
cd "$(dirname "$0")/.."
PGUSER_=${PGUSER_:-postgres}; PGPASS_=${PGPASS_:-}
mkdir -p .run
declare -A PORTS=([Identity]=5101 [Catalog]=5102 [Cart]=5103 [Ordering]=5104 [Inventory]=5105 [Payment]=5106 [Notification]=5107)
for S in "${!PORTS[@]}"; do
  db=$(echo "$S" | tr 'A-Z' 'a-z')
  ASPNETCORE_URLS="http://localhost:${PORTS[$S]}" \
  ConnectionStrings__Default="Host=localhost;Database=$db;Username=$PGUSER_;Password=$PGPASS_" \
  ConnectionStrings__RabbitMQ="amqp://guest:guest@localhost:5672" \
  ConnectionStrings__Redis="localhost:6379,abortConnect=false" \
  setsid nohup dotnet run --no-build --no-launch-profile -c Release --project "src/Services/$S/ShopSphere.$S.Api" > ".run/$S.log" 2>&1 < /dev/null &
done
ASPNETCORE_URLS="http://localhost:8088" ASPNETCORE_ENVIRONMENT=Development \
  setsid nohup dotnet run --no-build --no-launch-profile -c Release --project src/ApiGateway > .run/Gateway.log 2>&1 < /dev/null &
echo "started; logs in .run/"
