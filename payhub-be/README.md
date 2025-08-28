# PayHub

A .NET 8 Web API solution with Clean Architecture.

## Projects
- **PayHub.Api**: Web API (Controllers, Startup, Middlewares)
- **PayHub.Application**: Services, Interfaces, DTOs
- **PayHub.Infrastructure**: EF Core, Adapters, Repositories, Configs
- **PayHub.Domain**: Entities, Enums, ValueObjects

## Integrations
- EF Core (PostgreSQL)
- Redis cache
- RabbitMQ
- Serilog
- Scalar
- Prometheus + Grafana (API latency, TPS, bank success rate, DB query durations, queue depth)
- Elastic (ELK/OpenSearch) (transaction logs, fraud detection, audit trail)
- Alertmanager + Opsgenie/PagerDuty (real-time alerting, metrics endpoints)

## Getting Started
1. Restore dependencies: `dotnet restore`
2. Build the solution: `dotnet build PayHub.sln`
3. Run the API: `dotnet run --project PayHub.Api/PayHub.Api.csproj`

## Migration & Database
- Migrations via EF Core tools.
- PostgreSQL connection string in `appsettings.json`.

## Monitoring & Logging
- Prometheus, Grafana, Serilog, Elastic integrations required.

## Alerts
- Alertmanager, Opsgenie/PagerDuty endpoints to be implemented.
