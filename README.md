# Retail Inventory API

Retail Inventory API is a backend-focused project built with **ASP\.NET Core (.NET 10)** and **PostgreSQL**.

The project demonstrates explicit layered architecture, transactional domain logic, pagination, aggregation queries, repository abstraction, structured test coverage, and Dockerized infrastructure. It is designed to reflect real-world backend engineering practices rather than simple CRUD scaffolding.

---

## Architecture

**Controller → Service → Repository → DbContext → Database**

- Controllers handle HTTP concerns only (routing, status codes, request/response shaping)
- Services encapsulate domain logic and enforce business invariants independent of infrastructure concerns
- Repositories abstract query and persistence logic
- Entity Framework Core manages database access and migrations
- PostgreSQL is used for runtime
- SQLite (in-memory) is used for automated testing

This separation keeps business logic testable, maintainable, and easy to evolve as the system grows.

---

## Domain Overview

### Products

- Import products from DummyJSON external API
- Unique constraints on `ExternalId` and `SKU`
- DTO projection
- Repository-based querying

Endpoints:

- `GET /api/products`
- `GET /api/products/{id}`
- `POST /api/products/import`

---

### Customers

- Import users from DummyJSON external API
- Unique constraints on `ExternalId` and `Email`
- DTO projection
- Repository-based querying

Endpoints:

- `GET /api/customers`
- `GET /api/customers/{id}`
- `POST /api/customers/import`

---

### Orders

The order aggregate contains the core business logic of the system.

Implemented behavior:

- Transactional order creation
- Stock deduction on create
- Stock restoration on cancel
- Controlled state transitions (Pending → Completed / Cancelled)
- Revenue aggregation via summary endpoint
- Paginated and filtered order retrieval
- Random order generation for simulation/demo

Endpoints:

- `POST /api/orders`
- `GET /api/orders/{id}`
- `POST /api/orders/{id}/complete`
- `POST /api/orders/{id}/cancel`
- `GET /api/orders`
- `GET /api/orders/summary`
- `POST /api/orders/generate`

---

## Pagination & Filtering

Orders support:

- `pageNumber`
- `pageSize` (capped at 50)
- `status` filter (`Pending`, `Completed`, `Cancelled`)

Example:

```bash
GET /api/orders?pageNumber=1&pageSize=10&status=Completed
```

Pagination is implemented using `Skip` / `Take` with total count calculation to simulate production-ready API behavior.

---

## Transaction Handling

Order creation is wrapped in a database transaction to ensure:

- Stock updates and order creation are atomic
- Partial writes are prevented
- Business invariants are preserved

---

## Testing Strategy

### Unit Tests

- SQLite in-memory relational provider
- Business rule validation
- State transition enforcement
- Edge case coverage

### Integration Tests

- `WebApplicationFactory`
- Full HTTP pipeline execution
- Fresh database per test (deterministic isolation)
- Verification using separate DbContext scopes

This setup validates both domain correctness and application boundary correctness.

---

## Docker Support

Run with:

```bash
docker-compose up --build
```

This starts PostgreSQL and the API container, applies migrations automatically, and exposes the API on port **8080**.

Swagger:

```bash
http://localhost:8080/swagger
```

---

## Demo Workflow

After starting the application, the database is empty by design.

Populate the system using the following flow:

```bash
POST /api/products/import
POST /api/customers/import
POST /api/orders/generate
```

Example generate request:

```json
{
  "count": 20
}
```

Then explore:

```bash
GET /api/orders?pageNumber=1&pageSize=10
GET /api/orders?status=Completed
GET /api/orders/summary
```

This demonstrates stock mutation, transactional behavior, aggregation logic, and paging.

---

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL (Npgsql)
- SQLite (Testing)
- AutoMapper
- xUnit
- FluentAssertions
- Docker & Docker Compose
- Swagger

---

## Design Principles

- Business logic lives in services, not controllers
- Repositories isolate data access concerns
- Transactions protect aggregate invariants
- DTOs separate API contracts from persistence models
- Tests validate both domain rules and HTTP boundaries
- Infrastructure is environment-aware (PostgreSQL vs SQLite)

---

## Future Improvements

- JWT authentication
- Role-based authorization
- Sorting support for paging
- CI pipeline
- Structured logging (e.g., Serilog)