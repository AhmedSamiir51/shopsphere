#!/usr/bin/env bash
# Runs all services + the gateway directly with `dotnet`, against Postgres/RabbitMQ/Redis on localhost.
# Use this when Docker is not available. Infra defaults: postgres:5432 (user postgres), rabbitmq:5672 (guest/guest), redis:6379.
set -euo pipefail
cd "$(dirname "$0")/.."; ROOT=$PWD
PGUSER_=${PGUSER_:-postgres}; PGPASS_=${PGPASS_:-}
mkdir -p .run
# keep each process small so the whole stack fits in ~1 GB
export DOTNET_GCHeapHardLimit=0x8000000 DOTNET_TieredPGO=0 DOTNET_TieredCompilation=1
declare -A PORTS=([Identity]=5101 [Catalog]=5102 [Cart]=5103 [Ordering]=5104 [Inventory]=5105 [Payment]=5106 [Notification]=5107)
for S in "${!PORTS[@]}"; do
  db=$(echo "$S" | tr 'A-Z' 'a-z')
  cd "src/Services/$S/ShopSphere.$S.Api/bin/Release/net10.0" && ASPNETCORE_URLS="http://localhost:${PORTS[$S]}" \
  ConnectionStrings__Default="Host=localhost;Database=$db;Username=$PGUSER_;Password=$PGPASS_" \
  ConnectionStrings__RabbitMQ="amqp://guest:guest@localhost:5672" \
  ConnectionStrings__Redis="localhost:6379,abortConnect=false" \
  setsid nohup dotnet "ShopSphere.$S.Api.dll" > "$ROOT/.run/$S.log" 2>&1 < /dev/null &
  cd "$ROOT"
done
cd src/ApiGateway/bin/Release/net10.0 && ASPNETCORE_URLS="http://localhost:8088" ASPNETCORE_ENVIRONMENT=Development \
  setsid nohup dotnet ShopSphere.ApiGateway.dll > "$ROOT/.run/Gateway.log" 2>&1 < /dev/null &
echo "started; logs in .run/"
