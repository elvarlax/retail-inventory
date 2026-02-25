# Retail Inventory API

[![Build and
Test](https://github.com/elvarlax/retail-inventory/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/elvarlax/retail-inventory/actions)

Retail Inventory API is a backend-focused project built with **ASP\.NET
Core (.NET 10)** and **PostgreSQL**.

The project demonstrates layered architecture, transactional domain
logic, pagination, sorting, JWT-based authentication, role-based
authorization, structured logging, repository abstraction, automated
test coverage, and Dockerized infrastructure. It reflects real-world
backend engineering practices rather than simple CRUD scaffolding.

------------------------------------------------------------------------

## Architecture

**Controller → Service → Repository → DbContext → Database**

-   Controllers handle HTTP concerns only (routing, status codes,
    request/response shaping)
-   Services encapsulate domain logic and enforce business invariants
-   Repositories abstract query and persistence logic
-   Entity Framework Core manages database access and migrations
-   PostgreSQL is used for runtime
-   SQLite (in-memory) is used for automated testing

------------------------------------------------------------------------

## Authentication & Authorization

The API uses JWT Bearer authentication.

-   Token-based authentication using symmetric signing key
-   Role-based authorization (Admin / User)
-   Protected endpoints via `[Authorize]` attributes
-   Swagger integration with JWT support

Default seeded credentials:

| Role  | Email         | Password  |
|-------|---------------|-----------|
| Admin | admin@local   | Admin123! |
| User  | user@local    | User123!  |

Example:

POST /auth/login

Protected admin endpoint:

GET /admin/secret

------------------------------------------------------------------------

## Domain Overview

### Products

-   Unique constraint on `SKU`
-   DTO projection via AutoMapper
-   Pagination and sorting support

Endpoints:

-   GET /api/products
-   GET /api/products/{id}

------------------------------------------------------------------------

### Customers

-   Unique constraint on `Email`
-   DTO projection via AutoMapper
-   Pagination and sorting support

Endpoints:

-   GET /api/customers
-   GET /api/customers/{id}

------------------------------------------------------------------------

### Orders

The order aggregate contains the core business logic of the system.

Implemented behavior:

-   Transactional order creation
-   Stock deduction on create
-   Stock restoration on cancel
-   Controlled state transitions (Pending → Completed / Cancelled)
-   Revenue aggregation via summary endpoint
-   Paginated, filtered, and sorted order retrieval

Endpoints:

-   POST /api/orders
-   GET /api/orders/{id}
-   POST /api/orders/{id}/complete
-   POST /api/orders/{id}/cancel
-   GET /api/orders
-   GET /api/orders/summary

------------------------------------------------------------------------

### Admin Data Generation

High-volume test data generation using **Bogus**. Generators bypass the
service layer and write directly to the database in batches with
`AutoDetectChangesEnabled` disabled for performance.

-   Customers: realistic names and unique emails
-   Products: category-prefixed SKUs, randomized prices and stock
-   Orders: 60% Completed / 20% Cancelled / 20% Pending, dates spread
    over 12 months, correct `TotalAmount` computed from items

Endpoints (Admin role required):

-   POST /admin/generate/customers
-   POST /admin/generate/products
-   POST /admin/generate/orders

------------------------------------------------------------------------

## Pagination, Filtering & Sorting

Orders support:

-   pageNumber
-   pageSize (capped at 50)
-   status filter (Pending, Completed, Cancelled)
-   sortBy (createdAt, totalAmount, status)
-   sortDirection (asc / desc)

Products and Customers support:

-   pageNumber
-   pageSize
-   sortBy
-   sortDirection (asc / desc)

Example:

- GET /api/orders?pageNumber=1&pageSize=10&status=Completed
- GET /api/products?pageNumber=1&pageSize=10&sortBy=price&sortDirection=desc

------------------------------------------------------------------------

## Structured Logging

The API uses Serilog for structured logging.

-   Console logging
-   Environment-aware configuration
-   Suitable for containerized environments
-   Ready for extension to centralized log platforms

------------------------------------------------------------------------

## Transaction Handling

Order creation is wrapped in a database transaction to ensure:

-   Stock updates and order creation are atomic
-   Partial writes are prevented
-   Business invariants are preserved

------------------------------------------------------------------------

## Performance

-   N+1 queries eliminated via batch `WHERE id IN (...)` on product lookups
-   Order summary uses a single `GROUP BY` aggregation query
-   Covering index on `orders(status) INCLUDE (total_amount)` for summary
-   Indexes on `orders.created_at`, `orders.customer_id`, and
    `order_items.order_id` for pagination and JOIN performance
-   `AsNoTracking` on all read-only repository queries

------------------------------------------------------------------------

## Testing Strategy

### Unit Tests

-   SQLite in-memory relational provider
-   Business rule validation
-   State transition enforcement
-   Pagination and sorting coverage
-   Data generator correctness (count, uniqueness, field integrity)

### Integration Tests

-   WebApplicationFactory
-   Full HTTP pipeline execution
-   Fresh database per test (deterministic isolation)
-   Verification using separate DbContext scopes

------------------------------------------------------------------------

## Docker Support

Run with:

```bash
docker-compose up --build
```

This starts PostgreSQL and the API container, applies migrations
automatically, and exposes the API on port 8080.

Swagger:

http://localhost:8080/swagger

------------------------------------------------------------------------

## Tech Stack

-   .NET 10
-   ASP\.NET Core Web API
-   Entity Framework Core
-   PostgreSQL (Npgsql)
-   SQLite (Testing)
-   AutoMapper
-   Bogus
-   Serilog
-   xUnit
-   FluentAssertions
-   Docker & Docker Compose
-   GitHub Actions (CI)

------------------------------------------------------------------------

## Input Validation

Request DTOs use Data Annotations validated automatically by `[ApiController]`:

-   `POST /auth/login` — email format enforced via `[EmailAddress]`
-   `POST /api/orders` — item quantity must be ≥ 1 via `[Range]`
-   `POST /admin/generate/*` — count must be between 1 and 100000 via `[Range]`

Invalid requests return `400 Bad Request` before reaching the service layer.
Business rule violations (insufficient stock, invalid state transitions) are enforced in
the service layer and surfaced via `BadRequestException`.

------------------------------------------------------------------------

## Design Principles

-   Business logic lives in services, not controllers
-   Repositories isolate data access concerns
-   Transactions protect aggregate invariants
-   DTOs separate API contracts from persistence models
-   Tests validate both domain rules and HTTP boundaries
-   Infrastructure is environment-aware (PostgreSQL vs SQLite)
-   Read-only queries use `AsNoTracking` to reduce EF Core overhead
