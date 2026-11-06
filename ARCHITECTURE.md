# Architecture

## Style

Modular monolith with Clean Architecture.

## Layers

### Domain

Pure business concepts.

Allowed:

- Entities.
- Value objects.
- Domain events.
- Domain exceptions.

Forbidden:

- ASP.NET Core.
- EF Core.
- SignalR.
- SQL Server.
- MediatR.

### Application

Use cases and abstractions.

Allowed:

- Application services.
- Interfaces.
- DTOs/commands internal.
- Validation.

Forbidden:

- Infrastructure implementations.
- EF Core.
- SignalR.
- ASP.NET Core.

### Infrastructure

Implementations.

Includes:

- EF Core.
- SQL Server.
- JWT issuance.
- SignalR publisher.
- Outbox processor worker.
- Notification adapters.

### Api

HTTP and realtime entry points.

Includes:

- Controllers.
- Middleware.
- Swagger.
- Health checks.
- Hub mapping.

### Contracts

API request/response records.

Do not expose Domain entities.