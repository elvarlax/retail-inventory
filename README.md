# Retail Inventory API

[![Build and Test](https://github.com/elvarlax/retail-inventory/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/elvarlax/retail-inventory/actions)

Retail Inventory API is a backend-focused project built with **ASP\.NET Core (.NET 10)** and **PostgreSQL**.

The project demonstrates layered architecture, event-driven design via the transactional outbox pattern, transactional domain logic, pagination, sorting, JWT-based authentication, role-based authorization, structured logging, repository abstraction, automated test coverage, and Dockerized infrastructure — reflecting real-world backend engineering practices.

---

## Architecture

**Controller → Service → Repository → DbContext → Database**

- Controllers handle HTTP concerns only (routing, status codes, request/response shaping)
- Services encapsulate domain logic and enforce business invariants
- Repositories abstract query and persistence logic
- Entity Framework Core manages database access and migrations
- PostgreSQL is used for runtime; SQLite (in-memory) is used for automated testing

---

## Event-Driven Architecture — Transactional Outbox Pattern

Every domain mutation writes an event to an `outbox_messages` table in the **same database transaction** as the state change. A background service (`OutboxPublisher`) polls the table and publishes unpublished messages to an **Azure Service Bus topic**.

**Pipeline:**

```
API → PostgreSQL (outbox_messages) → OutboxPublisher → Azure Service Bus topic
```

**Events emitted:**

| Event | Trigger |
|-------|---------|
| `CustomerCreatedV1` | `POST /auth/register` or seed |
| `ProductCreatedV1` | `POST /api/products` or seed |
| `OrderPlacedV1` | `POST /api/orders` |
| `OrderStatusChangedV1` | `POST /api/orders/{id}/complete` or `/cancel` |

**`OutboxPublisher` behaviour:**

- Polls every 3 seconds for unpublished messages
- Sends batches of up to 20 messages per cycle
- Exponential backoff on failure (5s × attempt, capped at 60s)
- `published_at_utc` is stamped only after a confirmed send

**Local setup** uses the [Azure Service Bus Emulator](https://learn.microsoft.com/en-us/azure/service-bus-messaging/test-locally-with-service-bus-emulator) running in Docker alongside the API.

---

## Authentication & Authorization

The API uses JWT Bearer authentication.

- Token-based authentication using a symmetric signing key
- Role-based authorization (Admin / User)
- Protected endpoints via `[Authorize]` and `[Authorize(Roles = "Admin")]`
- Swagger UI with JWT support

Default seeded credentials:

| Role  | Email       | Password  |
|-------|-------------|-----------|
| Admin | admin@local | Admin123! |
| User  | user@local  | User123!  |

Endpoints:

- `POST /auth/login` — returns a JWT
- `POST /auth/register` — creates a user + customer profile, emits `CustomerCreatedV1`

---

## Domain Overview

### Products

- Unique constraint on `SKU`
- DTO projection via AutoMapper
- Pagination and sorting support
- Emits `ProductCreatedV1` on creation

Endpoints:

- `POST /api/products`
- `GET /api/products`
- `GET /api/products/{id}`

---

### Customers

- Unique constraint on `Email`
- DTO projection via AutoMapper
- Pagination and sorting support
- Customer profile is created automatically on registration

Endpoints:

- `GET /api/customers`
- `GET /api/customers/{id}`

---

### Orders

The order aggregate contains the core business logic.

Implemented behaviour:

- Transactional order creation (order + stock deduction + outbox event in one transaction)
- Stock restoration on cancel
- Controlled state transitions: `Pending → Completed` or `Pending → Cancelled`
- Revenue aggregation via summary endpoint
- Paginated, filtered, and sorted order retrieval
- Emits `OrderPlacedV1` on creation and `OrderStatusChangedV1` on status change

Endpoints:

- `POST /api/orders`
- `GET /api/orders`
- `GET /api/orders/{id}`
- `GET /api/orders/summary`
- `POST /api/orders/{id}/complete`
- `POST /api/orders/{id}/cancel`

---

### Admin — Data Seeding

Generates realistic test data using **Bogus**, routing through the full service layer so every created entity also emits its corresponding outbox event.

- Customers with realistic names and unique emails → `CustomerCreatedV1` per customer
- Products with category-prefixed SKUs, randomised prices and stock → `ProductCreatedV1` per product
- Orders with 1–3 items each, distributed ~60% Completed / 20% Cancelled / 20% Pending → `OrderPlacedV1` + `OrderStatusChangedV1` per order

Endpoint (Admin role required):

- `POST /admin/seed` — body: `{ "customers": 10, "products": 10, "orders": 20 }`

Each count is independently configurable. Response includes counts of entities created and total events emitted.

---

## Pagination, Filtering & Sorting

Orders support:

- `pageNumber`, `pageSize` (capped at 50)
- `status` filter (`Pending`, `Completed`, `Cancelled`)
- `sortBy` (`createdAt`, `totalAmount`, `status`)
- `sortDirection` (`asc` / `desc`)

Products and Customers support `pageNumber`, `pageSize`, `sortBy`, `sortDirection`.

Examples:

```
GET /api/orders?pageNumber=1&pageSize=10&status=Completed
GET /api/products?pageNumber=1&pageSize=10&sortBy=price&sortDirection=desc
```

---

## Structured Logging

The API uses **Serilog** for structured logging with environment-aware configuration and Serilog request logging middleware.

---

## Transaction Handling

Order creation is wrapped in a database transaction that atomically covers:

- Stock deduction across all ordered products
- Order and order items persistence
- Outbox message insertion

All succeed together or nothing is committed.

---

## Performance

- N+1 queries eliminated via batch `WHERE id IN (...)` on product lookups
- Order summary uses a single `GROUP BY` aggregation query
- Covering index on `orders(status) INCLUDE (total_amount)` for summary
- Indexes on `orders.created_at`, `orders.customer_id`, `order_items.order_id` for pagination and JOIN performance
- `AsNoTracking()` on all read-only repository queries
- Index on `outbox_messages.published_at_utc` for efficient unpublished message polling

---

## Testing Strategy

### Unit Tests

- SQLite in-memory relational provider
- Business rule validation
- State transition enforcement
- Pagination and sorting coverage

### Integration Tests

- `WebApplicationFactory` with full HTTP pipeline
- Fresh database per test for deterministic isolation
- Verification via separate `DbContext` scopes
- **Outbox tests** verify that each domain operation writes the correct event type, source, and payload to `outbox_messages`

---

## Running Locally

### Prerequisites

- Docker & Docker Compose
- .NET 10 SDK (for running tests outside Docker)

### Start the full stack

```bash
docker-compose up --build -d
```

Swagger UI is available at [http://localhost:8080/swagger](http://localhost:8080/swagger) once the api container is healthy.

### Seed test data (Admin token required)

```bash
# 1. Login
curl -s -X POST http://localhost:8080/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@local","password":"Admin123!"}' | jq .

# 2. Seed
curl -s -X POST http://localhost:8080/admin/seed \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"customers": 50, "products": 100, "orders": 500}' | jq .
```

### Run tests

```bash
dotnet test
```

---

## Docker

Run the full stack (API + PostgreSQL + Azure Service Bus Emulator + SQL Edge):

```bash
docker-compose up --build
```

Startup order:
1. `sqledge` starts and becomes healthy (SQL Edge is the storage backend for the Service Bus emulator)
2. `servicebus` starts once sqledge is healthy
3. `postgres` starts independently
4. `api` starts once both `postgres` and `servicebus` are started

Swagger UI: [http://localhost:8080/swagger](http://localhost:8080/swagger)

---

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core + EFCore.NamingConventions
- PostgreSQL (Npgsql)
- Azure Service Bus (`Azure.Messaging.ServiceBus`)
- Azure Service Bus Emulator (local development)
- SQLite (testing)
- AutoMapper
- Bogus
- BCrypt.Net
- Serilog
- xUnit + FluentAssertions
- Docker & Docker Compose
- GitHub Actions (CI)

---

## Input Validation

Request DTOs use Data Annotations validated by `[ApiController]` before reaching the service layer:

- `POST /auth/login` — `[EmailAddress]` on email, `[Required]` on password
- `POST /api/orders` — `[Range(1, int.MaxValue)]` on item quantity
- `POST /admin/seed` — `[Range(1, 1000)]` on customers/products, `[Range(1, 5000)]` on orders

Business rule violations (insufficient stock, invalid state transitions, duplicate email) are enforced in the service layer and surfaced via structured exception middleware.

---

## Design Principles

- Business logic lives in services, not controllers
- Repositories isolate data access concerns
- Transactions protect aggregate invariants
- Outbox pattern guarantees at-least-once event delivery without distributed transactions
- DTOs decouple API contracts from persistence models
- Tests validate both domain rules and HTTP boundaries
- Infrastructure is environment-aware (PostgreSQL vs SQLite, Service Bus vs disabled)
- Read-only queries use `AsNoTracking()` to reduce EF Core overhead
