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

Example:

POST /auth/login

Protected admin endpoint:

GET /admin/secret

------------------------------------------------------------------------

## Domain Overview

### Products

-   Import products from DummyJSON external API
-   Unique constraints on `ExternalId` and `SKU`
-   DTO projection via AutoMapper
-   Pagination and sorting support

Endpoints:

-   GET /api/products
-   GET /api/products/{id}
-   POST /api/products/import

------------------------------------------------------------------------

### Customers

-   Import users from DummyJSON external API
-   Unique constraints on `ExternalId` and `Email`
-   DTO projection via AutoMapper
-   Pagination and sorting support

Endpoints:

-   GET /api/customers
-   GET /api/customers/{id}
-   POST /api/customers/import

------------------------------------------------------------------------

### Orders

The order aggregate contains the core business logic of the system.

Implemented behavior:

-   Transactional order creation
-   Stock deduction on create
-   Stock restoration on cancel
-   Controlled state transitions (Pending → Completed / Cancelled)
-   Revenue aggregation via summary endpoint
-   Paginated and filtered order retrieval
-   Random order generation for simulation/demo

Endpoints:

-   POST /api/orders
-   GET /api/orders/{id}
-   POST /api/orders/{id}/complete
-   POST /api/orders/{id}/cancel
-   GET /api/orders
-   GET /api/orders/summary
-   POST /api/orders/generate

------------------------------------------------------------------------

## Pagination, Filtering & Sorting

Orders support:

-   pageNumber
-   pageSize (capped at 50)
-   status filter (Pending, Completed, Cancelled)

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

## Testing Strategy

### Unit Tests

-   SQLite in-memory relational provider
-   Business rule validation
-   State transition enforcement
-   Edge case coverage

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
-   Serilog
-   xUnit
-   FluentAssertions
-   Docker & Docker Compose
-   GitHub Actions (CI)

------------------------------------------------------------------------

## Design Principles

-   Business logic lives in services, not controllers
-   Repositories isolate data access concerns
-   Transactions protect aggregate invariants
-   DTOs separate API contracts from persistence models
-   Tests validate both domain rules and HTTP boundaries
-   Infrastructure is environment-aware (PostgreSQL vs SQLite)