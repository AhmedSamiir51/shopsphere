#!/usr/bin/env bash
# End-to-end smoke test through the gateway: register, browse, cart, order, watch the saga finish.
set -euo pipefail
GW=${GW:-http://localhost:8088}
EMAIL="e2e$RANDOM@example.com"
J='-H Content-Type:application/json'
AUTH=$(curl -sf $J -d "{\"email\":\"$EMAIL\",\"userName\":\"E2E\",\"password\":\"Passw0rd!x\"}" $GW/api/auth/register)
TOKEN=$(echo "$AUTH" | python3 -c 'import sys,json;print(json.load(sys.stdin)["accessToken"])')
B="Authorization: Bearer $TOKEN"
PRODUCT=$(curl -sf "$GW/api/products?pageSize=1&sort=price-asc")
PID=$(echo "$PRODUCT" | python3 -c 'import sys,json;print(json.load(sys.stdin)["items"][0]["id"])')
echo "product: $PID"
curl -sf -H "$B" $J -d "{\"productId\":\"$PID\",\"productName\":\"x\",\"unitPrice\":449,\"imageUrl\":\"\",\"quantity\":2}" $GW/api/cart/items >/dev/null
OID=$(curl -sf -H "$B" $J -d "{\"currency\":\"EGP\",\"items\":[{\"productId\":\"$PID\",\"productName\":\"x\",\"quantity\":2,\"unitPrice\":449}]}" $GW/api/orders | python3 -c 'import sys,json;print(json.load(sys.stdin)["id"])')
echo "order: $OID"
for i in $(seq 1 20); do
  S=$(curl -sf -H "$B" $GW/api/orders/$OID | python3 -c 'import sys,json;print(json.load(sys.stdin)["status"])')
  echo "  status: $S"; [ "$S" = Confirmed -o "$S" = Cancelled ] && break; sleep 1
done
echo "cart after order: $(curl -sf -H "$B" $GW/api/cart | python3 -c 'import sys,json;print(len(json.load(sys.stdin)["lines"]),"lines")')"
[ "$S" = Confirmed ]
