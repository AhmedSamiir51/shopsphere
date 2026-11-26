# ShopSphere

An event-driven e-commerce platform built as a microservices showcase: **.NET 10**, **RabbitMQ**, **PostgreSQL**, **Redis**, **YARP** gateway and an **Angular 22** storefront (Tailwind CSS 4, shadcn-style components).

Each service follows **Domain-Driven Design** with a **Clean Architecture** layout, and the order flow is a **choreographed saga** over RabbitMQ with a transactional outbox and idempotent consumers.

## Architecture

```mermaid
flowchart LR
  Web[Angular 22 SPA] --> GW[YARP API Gateway]
  GW --> ID[Identity]
  GW --> CAT[Catalog]
  GW --> CART[Cart]
  GW --> ORD[Ordering]
  GW --> INV[Inventory]
  GW --> PAY[Payment]
  GW --> NOT[Notification]
  ID --- IDDB[(Postgres)]
  CAT --- CATDB[(Postgres)]
  CART --- REDIS[(Redis)]
  ORD --- ORDDB[(Postgres)]
  INV --- INVDB[(Postgres)]
  PAY --- PAYDB[(Postgres)]
  NOT --- NOTDB[(Postgres)]
  ID & CAT & CART & ORD & INV & PAY & NOT <-. integration events .-> MQ{{RabbitMQ}}
```

| Service | Responsibility | Store |
|---|---|---|
| Identity | Register, login, JWT issuing, roles | Postgres |
| Catalog | Products, categories, search, filters, sorting | Postgres |
| Cart | Per-user shopping cart | Redis |
| Ordering | Order aggregate and saga state | Postgres |
| Inventory | Stock levels and reservations | Postgres |
| Payment | Payment attempts (simulated gateway) | Postgres |
| Notification | Emails (logged) and in-app notifications | Postgres |

### Order saga

```mermaid
sequenceDiagram
  participant O as Ordering
  participant I as Inventory
  participant P as Payment
  participant N as Notification
  O->>I: OrderCreated
  alt stock available
    I->>O: StockReserved
    O->>P: PaymentRequested
    alt payment ok
      P->>O: PaymentSucceeded
      O->>I: OrderConfirmed (commit stock)
      O->>N: OrderConfirmed
    else payment declined
      P->>O: PaymentFailed
      O->>I: OrderCancelled (release stock)
      O->>N: OrderCancelled
    end
  else out of stock
    I->>O: StockReservationFailed
    O->>N: OrderCancelled
  end
```

Reliability patterns: **transactional outbox** (events are saved with the aggregate, a dispatcher publishes them), **inbox deduplication** (consumers are idempotent), **dead-letter queues** for poison messages, and publisher confirms.

### Layout of each service

```
src/Services/<Service>/
  ShopSphere.<Service>.Domain          aggregates, value objects, domain events (no dependencies)
  ShopSphere.<Service>.Application     CQRS commands/queries (MediatR), validators, integration handlers
  ShopSphere.<Service>.Infrastructure  EF Core, repositories, external adapters
  ShopSphere.<Service>.Api             minimal-API endpoints, DI composition root
src/BuildingBlocks                     shared kernel, MediatR pipeline behaviours, RabbitMQ event bus, outbox/inbox
```

Dependencies point inward only. This is enforced by architecture tests (NetArchTest).

## Run it

```bash
docker compose up --build
```

- Storefront: http://localhost:4200
- API gateway: http://localhost:8080
- RabbitMQ UI: http://localhost:15672 (guest / guest)

Create an account, add products to the cart and check out. The orders page shows the saga progressing live. In the simulated payment gateway, a total ending in `.99` is declined so you can see the compensation path.

### Develop without Docker for the SPA

```bash
cd web && npm install && npm start   # proxies /api to http://localhost:8080
```

## Tests

```bash
dotnet test ShopSphere.sln          # domain, saga/application and architecture tests
cd web && npx ng test --no-watch    # Angular unit tests (Vitest)
```

## Messaging and caching patterns

**RabbitMQ (topic exchange, one queue per service)**
- *Transactional outbox*: events are written to an `outbox` table in the same DB transaction as the state change; a background dispatcher (300 ms poll) publishes them. No lost events if the broker is down.
- *Inbox / idempotent consumers*: each consumer records processed message ids, so redelivery never double-applies.
- *Dead-letter queues*: messages that keep failing go to a DLQ instead of blocking or vanishing.
- *Publisher confirms and manual acks*: a message is acked only after it is handled.
- *Choreographed saga*: `OrderCreated -> StockReserved -> PaymentRequested -> PaymentSucceeded -> OrderConfirmed`, with compensation (`PaymentFailed -> OrderCancelled -> stock released`).

**Redis**
- Cart storage (Cart service).
- Catalog *cache-aside* for product lists and details; admin changes bump a version key so all stale entries are invalidated at once. If Redis is down, Catalog falls back to Postgres.

**Other system-design points**
- Server-side pricing: Ordering keeps its own `product_snapshots` read model fed by Catalog events (HTTP fallback). The client sends only product id and quantity.
- Database per service, JWT with roles, auth rate limit (10 req/min per IP, then 429).

## Run without Docker

`scripts/run-local.sh` starts the 7 services and the gateway against local Postgres, Redis and RabbitMQ (gateway on :8088); `node scripts/serve-web.mjs` serves the built SPA on :4200; `scripts/e2e.sh` runs a full order through the gateway.

Demo credentials: admin `admin@shopsphere.dev` / `Admin123!` (override with `Admin:Email` / `Admin:Password`). Customers register themselves. The product "Demo: Payment Decline Test Item" is always declined by the simulated bank.

## Honest limits

- `docker compose up` is provided but was not run in the authoring sandbox (no Docker there); the stack was verified end to end with local Postgres, Redis and RabbitMQ.
- Payment is simulated; no real bank or card is involved.
- Testcontainers integration tests are planned and run only where Docker exists (CI).

## Tech notes

- .NET 10, C# latest, EF Core 10, FluentValidation 12, RabbitMQ.Client 7 (async API).
- MediatR is pinned to 12.5.0 and FluentAssertions to 7.2.0: later major versions need a commercial licence, and this repo uses free tooling only.
- UI components follow the shadcn/ui approach (copy-in components using Tailwind tokens and class-variance-authority) rather than a component library dependency.

## Roadmap

Testcontainers integration tests, OpenTelemetry tracing, Kubernetes manifests.

## License

MIT
