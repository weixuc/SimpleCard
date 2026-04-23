# Card Transaction API

A production-ready REST API for managing credit cards and foreign-currency transactions, built with .NET 10 Clean Architecture and CQRS.

---

## Table of Contents

- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Running Locally](#running-locally)
  - [With Docker (recommended)](#with-docker-recommended)
  - [Without Docker](#without-docker)
- [API Reference](#api-reference)
  - [Create Card](#create-card)
  - [Store Transaction](#store-transaction)
  - [Get Transaction](#get-transaction)
  - [Get Card Balance](#get-card-balance)
  - [Health Check](#health-check)
- [Currency Conversion](#currency-conversion)
- [Error Handling](#error-handling)
- [CI/CD](#cicd)
- [Cloud Infrastructure (Terraform)](#cloud-infrastructure-terraform)
- [Testing](#testing)
- [Design Decisions](#design-decisions)

---

## Architecture

The solution follows **Clean Architecture** with strict one-way dependency flow:

```
Domain  <--  Application  <--  Infrastructure
Domain  <--  ExchangeRateClient
Application  <--  Api
```

| Layer | Responsibility |
|---|---|
| **Domain** | Entities (`Card`, `Transaction`), the `IExchangeRateService` interface |
| **Application** | CQRS commands and queries via MediatR, business logic, custom exceptions |
| **Infrastructure** | EF Core `AppDbContext`, Npgsql (production) / InMemory (tests) |
| **ExchangeRateClient** | HTTP client for the US Treasury Fiscal Data API |
| **Api** | ASP.NET Core controllers, exception middleware, Swagger |

---

## Project Structure

```
SimpleCard.sln
|-- src/
|   |-- SimpleCard.Domain/               # Entities and interfaces
|   |-- SimpleCard.Application/          # CQRS handlers, exceptions, interfaces
|   |   |-- Cards/
|   |   |   |-- Commands/
|   |   |   |   |-- CreateCard/
|   |   |   |   +-- CreateTransaction/
|   |   |   +-- Queries/
|   |   |       +-- GetCardBalance/
|   |   +-- Transactions/
|   |       +-- Queries/
|   |           +-- GetTransaction/
|   |-- SimpleCard.Infrastructure/       # EF Core, database configurations
|   |-- SimpleCard.ExchangeRateClient/   # US Treasury API integration
|   +-- SimpleCard.Api/                  # Controllers, middleware, Dockerfile
+-- tests/
    |-- SimpleCard.Application.Tests/        # Handler unit tests (InMemory DB)
    |-- SimpleCard.ExchangeRateClient.Tests/ # HTTP client unit tests (fake handler)
    +-- SimpleCard.Api.Tests/                # Integration tests (WebApplicationFactory)
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for the Docker path)
- PostgreSQL 16+ (for the non-Docker path)

---

## Running Locally

### With Docker (recommended)

```bash
docker compose up --build
```

The API starts on **http://localhost:8080**. Swagger UI is available at **http://localhost:8080/swagger**.

To stop and remove volumes:

```bash
docker compose down -v
```

### Without Docker

**1. Start PostgreSQL and create the database**

```bash
psql -U postgres -c "CREATE DATABASE simplecard;"
```

**2. Update the connection string**

Edit `src/SimpleCard.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=simplecard;Username=postgres;Password=yourpassword"
  }
}
```

**3. Run the API**

```bash
dotnet run --project src/SimpleCard.Api
```

The API starts on **http://localhost:5000** (or the port shown in the terminal). Swagger UI is at **/swagger**.

> The database schema is created automatically via `EnsureCreated()` on startup -- no migrations needed.

---

## API Reference

All responses use `application/json`. Errors follow [RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457).

### Create Card

```
POST /api/cards
```

**Request body**

```json
{
  "creditLimit": 1500.00
}
```

**Response** `201 Created`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "creditLimit": 1500.00
}
```

The `Location` header points to `GET /api/cards/{id}/balance`.

---

### Store Transaction

```
POST /api/cards/{cardId}/transactions
```

**Request body**

```json
{
  "description": "Lunch at diner",
  "transactionDate": "2024-03-15",
  "amount": 24.75
}
```

**Response** `201 Created`

```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "cardId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "description": "Lunch at diner",
  "transactionDate": "2024-03-15",
  "amount": 24.75
}
```

The `Location` header points to `GET /api/transactions/{id}`.

| Status | Condition |
|---|---|
| `404 Not Found` | Card ID does not exist |

---

### Get Transaction

```
GET /api/transactions/{transactionId}?currency=USD
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `transactionId` | `guid` | -- | Transaction ID |
| `currency` | `string` | `USD` | Target currency (see [Currency Conversion](#currency-conversion)) |

**Response** `200 OK`

```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "description": "Lunch at diner",
  "transactionDate": "2024-03-15",
  "originalAmountUsd": 24.75,
  "currency": "Canada-Dollar",
  "exchangeRate": 1.3512,
  "convertedAmount": 33.44
}
```

Converted amount is **rounded to 2 decimal places**.

| Status | Condition |
|---|---|
| `404 Not Found` | Transaction ID does not exist |
| `400 Bad Request` | No exchange rate found within 6 months before the transaction date |

---

### Get Card Balance

```
GET /api/cards/{cardId}/balance?currency=USD
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `cardId` | `guid` | -- | Card ID |
| `currency` | `string` | `USD` | Target currency (see [Currency Conversion](#currency-conversion)) |

**Response** `200 OK`

```json
{
  "cardId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "creditLimitUsd": 1500.00,
  "totalTransactionsUsd": 24.75,
  "availableBalanceUsd": 1475.25,
  "currency": "Canada-Dollar",
  "exchangeRate": 1.3512,
  "convertedAvailableBalance": 1993.14
}
```

| Status | Condition |
|---|---|
| `404 Not Found` | Card ID does not exist |
| `400 Bad Request` | No exchange rate found for the requested currency |

---

### Health Check

```
GET /health
```

Returns `200 OK` with status `Healthy` when the service is running. Used by the load balancer and container orchestrator to determine readiness.

---

## Currency Conversion

Exchange rates are sourced from the **US Treasury Fiscal Data API** -- [Rates of Exchange](https://fiscaldata.treasury.gov/datasets/treasury-reporting-rates-exchange/treasury-reporting-rates-of-exchange).

**For `GET /api/transactions/{id}`:** uses the most recent rate published on or before the transaction date, within a 6-month lookback window. If no rate is found in that window, `400 Bad Request` is returned.

**For `GET /api/cards/{id}/balance`:** uses the most recent available rate for the currency, with no date restriction.

**Currency names** must match the `country_currency_desc` field in the Treasury API. Examples:

| Currency | Value to pass |
|---|---|
| Canadian Dollar | `Canada-Dollar` |
| Euro | `Euro Zone-Euro` |
| British Pound | `United Kingdom-Pound` |
| Japanese Yen | `Japan-Yen` |
| Australian Dollar | `Australia-Dollar` |

Passing `USD` (case-insensitive) always returns a rate of `1.0` with no external API call.

---

## Error Handling

All errors return an [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) Problem Details body:

```json
{
  "status": 404,
  "title": "Not Found",
  "detail": "Card with id '3fa85f64-...' was not found."
}
```

| Status | Cause |
|---|---|
| `400 Bad Request` | Exchange rate unavailable for the requested currency / date range |
| `404 Not Found` | Card or transaction ID does not exist |
| `500 Internal Server Error` | Unexpected server-side error |

---

## CI/CD

A GitHub Actions pipeline runs on every push and pull request to `main`.

It restores, builds, and runs all 36 tests:

```
.github/workflows/ci.yml
```

Test results are published as a check on each PR via the `dorny/test-reporter` action.

To extend the pipeline to deploy after a successful build, push the Docker image to ECR and apply Terraform:

```yaml
- name: Build and push image
  run: |
    aws ecr get-login-password | docker login --username AWS --password-stdin $ECR_URI
    docker build -t $ECR_URI:$GITHUB_SHA -f src/SimpleCard.Api/Dockerfile .
    docker push $ECR_URI:$GITHUB_SHA

- name: Deploy
  run: terraform apply -auto-approve -var="image_tag=$GITHUB_SHA"
  working-directory: terraform
```

---

## Cloud Infrastructure (Terraform)

The `terraform/` directory provisions a production-ready AWS environment:

```
terraform/
|-- main.tf       # VPC, subnets, ECS Fargate, RDS PostgreSQL, ALB, CloudWatch
|-- variables.tf  # All configurable values
+-- outputs.tf    # ALB URL, DB endpoint, cluster name
```

**Resources created:**

| Resource | Details |
|---|---|
| VPC | Public + private subnets across 2 availability zones |
| RDS PostgreSQL 16 | Private subnet, security group locked to ECS tasks only |
| ECS Fargate | Cluster + service with configurable replica count |
| Application Load Balancer | Public-facing, forwards to ECS tasks on port 8080 |
| CloudWatch Logs | 30-day retention for container logs |

**Deploy:**

```bash
cd terraform

terraform init
terraform plan -var="ecr_image_uri=<your-ecr-uri>" -var="db_password=<secret>"
terraform apply -var="ecr_image_uri=<your-ecr-uri>" -var="db_password=<secret>"
```

The ALB URL is printed as an output. The `/health` endpoint is used for both ALB and ECS health checks.

---

## Testing

Run the full test suite (36 tests across 3 projects):

```bash
dotnet test SimpleCard.sln
```

Run a specific project:

```bash
dotnet test tests/SimpleCard.Application.Tests
dotnet test tests/SimpleCard.ExchangeRateClient.Tests
dotnet test tests/SimpleCard.Api.Tests
```

### Test coverage

| Project | What is tested |
|---|---|
| `SimpleCard.Application.Tests` | All CQRS handlers: USD and foreign currency paths, not-found cases, exchange rate unavailable, multi-transaction sums, rounding |
| `SimpleCard.ExchangeRateClient.Tests` | `TreasuryExchangeRateService`: successful parse, empty response, HTTP errors, 6-month date range in the filter query |
| `SimpleCard.Api.Tests` | Full HTTP integration tests via `WebApplicationFactory`: all endpoints, happy paths, 404 and 400 error paths |

The API test project uses a `TestWebApplicationFactory` that:
- Switches the database to EF Core InMemory (no PostgreSQL needed for tests)
- Replaces `IExchangeRateService` with a configurable `FakeExchangeRateService`

---

## Design Decisions

**Clean Architecture with CQRS** -- Commands and queries are kept separate, each in their own folder with command/query, handler, and response types. MediatR decouples the API layer from business logic so controllers are thin dispatchers.

**`IExchangeRateService` in Domain** -- The interface lives in the Domain project so the Application layer can depend on it without knowing anything about HTTP or the Treasury API. The concrete implementation lives in `SimpleCard.ExchangeRateClient`, which is only wired up in the API host.

**`DatabaseProvider` config key** -- Infrastructure's DI extension reads `configuration["DatabaseProvider"]`. Setting it to `InMemory` switches the entire EF Core setup without any test-only code paths inside production projects. The test factory sets this via `UseSetting`, keeping test concerns out of the source code.

**Hand-rolled `FakeExchangeRateService`** -- Uses `Func<>` delegate properties instead of a mocking library. This avoids Castle.Core/Moq runtime compatibility issues on .NET 10 and produces simpler, more readable test setup code.

**No migrations** -- `EnsureCreated()` is called at startup for simplicity. In a production system with evolving schema, EF Core migrations or a tool like Flyway would be used instead.

**REST over event-driven for this scope** -- This service exposes a synchronous REST API because the assignment is self-contained and all operations are request/response by nature. In a real payments platform, transaction creation would likely publish a `TransactionCreated` event to a message broker (e.g. Kafka) so that downstream services -- fraud detection, ledger reconciliation, notification delivery -- can react independently without tight coupling. The CQRS structure already separates writes from reads, making it straightforward to add an event publisher inside a command handler without changing the API contract.
