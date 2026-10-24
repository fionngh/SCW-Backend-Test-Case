# CloudWorks Door Access API

Backend API for cloud-based door access management: remote door opening, user and permission management, and restricted access to audit history.

Stack: ASP.NET Core 10 (C#), EF Core, PostgreSQL.

## Repository layout

| Path | Purpose |
|---|---|
| `src/CloudWorks.Common` | Types shared by every layer, such as the exceptions that map to HTTP errors. |
| `src/CloudWorks.Domain` | Entities, invariants and domain rules. No framework dependencies. |
| `src/CloudWorks.Application` | Use cases and the interfaces (ports) they depend on. |
| `src/CloudWorks.Infrastructure` | EF Core persistence, migrations, hardware adapters. |
| `src/CloudWorks.Api` | HTTP endpoints, authentication, error handling, OpenAPI. |
| `tests/CloudWorks.UnitTests` | Domain and application tests. No I/O. |
| `tests/CloudWorks.IntegrationTests` | API tests against a real PostgreSQL database. |

Dependencies point inward: `Api -> Infrastructure -> Application -> Domain -> Common`.

## Prerequisites

- .NET SDK 10.0.402 or later feature band (pinned in `global.json`)
- PostgreSQL 16 or later, running locally

## Configuration

Copy `.env.example` and set the values as environment variables (or use `dotnet user-secrets` for the API project). Real secrets are never committed.

## Build, format, test

```powershell
dotnet restore CloudWorks.sln --locked-mode
dotnet build CloudWorks.sln
dotnet format CloudWorks.sln --verify-no-changes
dotnet test tests/CloudWorks.UnitTests
dotnet test tests/CloudWorks.IntegrationTests   # needs CLOUDWORKS_TEST_POSTGRES
```

Integration tests create and drop a uniquely named database per run, so the role in `CLOUDWORKS_TEST_POSTGRES` must be allowed to create databases.
