# Retail Inventory API

A RESTful API built with **ASP.NET Core (.NET 10)** and **PostgreSQL**
for managing retail inventory.

This project demonstrates clean layered architecture, external API
ingestion, DTO separation, environment-based configuration, and database
integrity constraints.

------------------------------------------------------------------------

## Architecture

Controller → Service → Repository → DbContext → PostgreSQL

-   Controllers handle HTTP requests\
-   Services contain business logic\
-   Repositories abstract data access\
-   EF Core manages persistence\
-   PostgreSQL stores data

------------------------------------------------------------------------

## Implemented Features

### Product Ingestion

-   Imports all products from DummyJSON (`/products`)
-   Handles pagination dynamically
-   Unique constraints on `ExternalId` and `SKU`
-   Endpoints:
    -   `GET /api/products`
    -   `GET /api/products/{id}`
    -   `POST /api/products/import`

### Customer Ingestion

-   Imports all users from DummyJSON (`/users`)
-   Paginated import logic
-   Unique constraints on `ExternalId` and `Email`
-   Endpoint:
    -   `POST /api/customers/import`

### Configuration

-   `appsettings.json` contains non-sensitive defaults
-   `appsettings.Development.json` stores local secrets (ignored by Git)
-   Environment-based configuration loading

------------------------------------------------------------------------

## Tech Stack

-   .NET 10
-   ASP.NET Core Web API
-   Entity Framework Core
-   PostgreSQL (Npgsql)
-   Swagger
-   Layered Architecture Pattern

------------------------------------------------------------------------

## Database Schema

### Products

-   Id (Guid)
-   ExternalId (unique)
-   Name
-   SKU (unique)
-   StockQuantity
-   Price

### Customers

-   Id (Guid)
-   ExternalId (unique)
-   FirstName
-   LastName
-   Email (unique)

------------------------------------------------------------------------

## Getting Started

### Prerequisites

-   .NET 10 SDK
-   PostgreSQL

### Setup

1.  Clone the repository

2.  Create `appsettings.Development.json` inside `RetailInventory.Api`

3.  Apply migrations:

        dotnet ef database update

4.  Run the application:

        dotnet run

5.  Open Swagger at:

        https://localhost:7182/swagger/index.html

------------------------------------------------------------------------

## Next Steps

-   Implement Order aggregate with transaction handling
-   Add stock validation
-   Introduce AutoMapper
-   Add validation middleware

------------------------------------------------------------------------

## Purpose

This project is designed as a portfolio-grade backend system
demonstrating practical backend engineering patterns and clean
architecture principles.