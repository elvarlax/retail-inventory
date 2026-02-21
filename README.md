# Retail Inventory API

Retail Inventory API is a backend-focused project built with ASP.NET
Core (.NET 10) and PostgreSQL.\
It demonstrates layered architecture, transactional domain logic,
pagination, aggregation queries, and structured test coverage.

The goal of this project is to reflect real-world backend engineering
practices rather than simple CRUD scaffolding.

------------------------------------------------------------------------

## Architecture

Controller → Service → Repository → DbContext → PostgreSQL

-   Controllers handle HTTP requests and responses
-   Services encapsulate business rules and domain logic
-   Repositories abstract data access
-   Entity Framework Core manages persistence
-   PostgreSQL is used for development and production
-   SQLite (in-memory) is used for testing

This separation keeps business logic independent from infrastructure
concerns and makes the application easier to test and evolve.

------------------------------------------------------------------------

## Domain Features

### Products

-   Import products from DummyJSON (`/products`)
-   Unique constraints on `ExternalId` and `SKU`
-   Endpoints:
    -   `GET /api/products`
    -   `GET /api/products/{id}`
    -   `POST /api/products/import`

### Customers

-   Import users from DummyJSON (`/users`)
-   Unique constraints on `ExternalId` and `Email`
-   Endpoints:
    -   `GET /api/customers`
    -   `GET /api/customers/{id}`
    -   `POST /api/customers/import`

### Orders

The order aggregate contains the core business logic of the system.

Implemented behavior includes:

-   Transactional order creation
-   Stock deduction during order creation
-   Stock restoration when cancelling an order
-   Controlled state transitions (Pending → Completed / Cancelled)
-   Revenue aggregation via summary endpoint
-   Paginated and filtered order retrieval
-   AutoMapper-based projection to DTOs

Endpoints:

-   `POST /api/orders`
-   `GET /api/orders/{id}`
-   `PUT /api/orders/{id}/complete`
-   `PUT /api/orders/{id}/cancel`
-   `GET /api/orders`
-   `GET /api/orders/summary`

------------------------------------------------------------------------

## Pagination & Filtering

Orders support:

-   `pageNumber`
-   `pageSize` (with enforced upper bound)
-   `status` filter (Pending, Completed, Cancelled)

Example:

    GET /api/orders?pageNumber=1&pageSize=10&status=Completed

Pagination is implemented using `Skip`/`Take` with total count
calculation to simulate production-ready API behavior.

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
-   Validation of business rules
-   Verification of stock mutation
-   Enforcement of state transitions
-   Tests for failure scenarios (invalid customer, insufficient stock,
    etc.)

### Integration Tests

-   WebApplicationFactory
-   Full HTTP pipeline execution
-   Environment-based database override (Testing → SQLite)
-   Real routing, middleware, and serialization validation

This setup ensures both domain logic correctness and application
boundary correctness.

------------------------------------------------------------------------

## AutoMapper

AutoMapper is used for:

-   Entity → DTO projection
-   Order and OrderItem mapping
-   Separation between persistence models and API contracts

Mapping profiles are centrally defined and injected via DI.

------------------------------------------------------------------------

## Tech Stack

-   .NET 10
-   ASP.NET Core Web API
-   Entity Framework Core
-   PostgreSQL (Npgsql)
-   SQLite (Testing)
-   AutoMapper
-   xUnit
-   FluentAssertions
-   Swagger

------------------------------------------------------------------------

## Configuration

-   `appsettings.json` -- base configuration
-   `appsettings.Development.json` -- local secrets (ignored by Git)
-   `Testing` environment switches provider from PostgreSQL to SQLite

------------------------------------------------------------------------

## Getting Started

### Prerequisites

-   .NET 10 SDK
-   PostgreSQL

### Setup

1.  Clone repository
2.  Create `appsettings.Development.json` inside `RetailInventory.Api`
3.  Apply migrations:

```{=html}
<!-- -->
```
    dotnet ef database update

4.  Run application:

```{=html}
<!-- -->
```
    dotnet run

5.  Open Swagger:

```{=html}
<!-- -->
```
    https://localhost:7182/swagger/index.html

------------------------------------------------------------------------

## Project Focus

This project is designed to demonstrate:

-   Clean layered architecture
-   Transactional consistency
-   Business rule enforcement
-   Pagination and aggregation
-   Structured testing (unit + integration)
-   Environment-aware infrastructure configuration