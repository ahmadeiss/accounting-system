# Architecture

## Pattern: Modular Monolith

This system uses a **Modular Monolith** architecture deployed as a single .NET 8 Web API.

The monolith is organized into clean, decoupled layers that can be independently reasoned about,
tested, and eventually extracted into services if business scale demands it.

---

## Layer Structure

```
┌──────────────────────────────────────────────────────┐
│                   Accounting.API                     │
│   Controllers │ Middleware │ Program.cs │ DI Setup   │
├──────────────────────────────────────────────────────┤
│              Accounting.Application                  │
│   Services │ DTOs │ Interfaces │ Validators │ Maps   │
├──────────────────────────────────────────────────────┤
│               Accounting.Infrastructure              │
│   EF Core │ DbContext │ Repositories │ Jobs │ Excel  │
├──────────────────────────────────────────────────────┤
│                  Accounting.Core                     │
│   Entities │ Enums │ Exceptions │ Interfaces │ Rules │
└──────────────────────────────────────────────────────┘
              ↓ depends on ↑ (inner layers only)
```

### Dependency Rule
- `Core` has NO dependencies on any other project
- `Application` depends on `Core` only
- `Infrastructure` depends on `Core` and `Application`
- `API` depends on all layers (wires everything together)

---

## Projects

| Project | Responsibility |
|---|---|
| `Accounting.Core` | Domain entities, enums, domain exceptions, core interfaces |
| `Accounting.Application` | Business logic, services, DTOs, validators, mappings |
| `Accounting.Infrastructure` | EF Core DbContext, migrations, repository impls, background jobs, Excel parser |
| `Accounting.API` | HTTP controllers, middleware, DI config, Swagger, health checks |

---

## Module Organization (inside projects)

Modules map to business domains. Code within each layer is grouped by module:

```
Application/
├── Auth/
├── Branches/
├── Warehouses/
├── Items/
├── Batches/
├── Purchasing/
├── Sales/
├── Stock/
├── Alerts/
├── Import/
└── Audit/
```

Each module contains its own DTOs, service interfaces, and service implementations.

---

## Key Design Decisions

### 1. No Repositories Forced Everywhere
EF Core DbContext is used directly in Application services when simple queries suffice.
Repository pattern is applied for complex aggregate queries or when testability demands it.

### 2. Domain Services in Application Layer
Business rules that span multiple entities live in Application service classes, not controllers.
Controllers are thin: validate HTTP request → call service → return response.

### 3. Domain Exceptions
Business rule violations throw typed exceptions (`DomainException`, `ExpiredBatchException`, etc.)
These are caught by global error handling middleware and returned as structured problem details.

### 4. Stock Movements as Source of Truth
Stock quantity for any item/warehouse combination is derived from `StockMovement` records.
A `StockBalance` table is maintained as a performance-optimized summary and must always
match the sum of movements. Both are updated in the same transaction.

### 5. Audit Log
Every controller action for critical mutations writes to `AuditLog` via a service.
This is not optional for inventory and financial operations.

---

## Technology Stack

| Concern | Choice | Reason |
|---|---|---|
| Runtime | .NET 8 | LTS, performance, ecosystem |
| Web framework | ASP.NET Core Web API | Industry standard |
| ORM | Entity Framework Core 8 | Migrations, LINQ, PostgreSQL support |
| Database | PostgreSQL 15+ | ACID, JSON support, open source |
| Auth | JWT Bearer + Refresh Tokens | Stateless, scalable |
| Validation | FluentValidation | Clean, testable rule chains |
| Mapping | AutoMapper | DTO mapping |
| Logging | Serilog → structured JSON | Queryable logs, production-ready |
| Background jobs | Hangfire (PostgreSQL backend) | Scheduled alerts, import jobs |
| API docs | Swashbuckle (Swagger UI) | Auto-generated, try-it-out |
| Excel parsing | EPPlus | Reliable .xlsx parsing |
| Testing | xUnit + Moq + EF InMemory | Unit + integration tests |
| Containerization | Docker + docker-compose | On-premise and cloud deployment |
| Frontend | React 18 + TypeScript + Vite | Modern, fast dev experience |
| UI library | shadcn/ui + Tailwind CSS | Clean, maintainable components |

---

## Authentication Flow

```
Client → POST /api/auth/login
       → API validates credentials
       → Issues JWT (15min) + Refresh Token (7 days, stored in DB)
       → Client stores tokens
       → Subsequent requests: Authorization: Bearer {jwt}
       → On expiry: POST /api/auth/refresh → new JWT issued
```

---

## Error Handling Strategy

All errors return RFC 7807 Problem Details format:

```json
{
  "type": "https://accounting.system/errors/domain-violation",
  "title": "Business Rule Violation",
  "status": 422,
  "detail": "Batch EXP-2024-001 has expired and cannot be sold.",
  "traceId": "0HN3K..."
}
```

---

## Configuration Strategy

| File | Purpose |
|---|---|
| `appsettings.json` | Default (non-secret) configuration |
| `appsettings.Development.json` | Dev overrides (local DB, verbose logging) |
| `appsettings.Production.json` | Production settings skeleton (no secrets) |
| Environment variables | Secrets (DB password, JWT key) in production |
| `.env` | Docker compose local dev secrets |

---

## Future Architecture Considerations
- Module boundaries are clean enough to extract services if needed
- Background job infrastructure (Hangfire) supports AI prediction jobs later
- Stock movement time-series data is queryable for forecasting
- Schema is additive: AI-specific columns or tables can be added without breaking existing logic

