# Task Management API

A RESTful task management API built with ASP.NET Core 10 Minimal API and Entity Framework Core 10 (SQLite). Developed as ADO #33 — the first controlled local pilot of the `ai-sdlc-starter-kit` governance framework.

## Quick start

```bash
dotnet run --project TaskManagementApi
```

The API starts on `http://localhost:5000` (or the port shown in the terminal).

## Endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/tasks` | Create a task |
| `GET` | `/tasks` | List tasks (paged, optional `?status=` filter) |
| `GET` | `/tasks/{id}` | Get a task by ID |
| `PATCH` | `/tasks/{id}` | Update a task |
| `DELETE` | `/tasks/{id}` | Delete a task |

### Example — create a task

```bash
curl -X POST http://localhost:5000/tasks \
  -H "Content-Type: application/json" \
  -d '{"title":"Write tests","priority":"High"}'
```

### Example — list in-progress tasks

```bash
curl "http://localhost:5000/tasks?status=InProgress&page=1&pageSize=10"
```

## Running tests

```bash
dotnet test
```

25 tests — 13 integration (end-to-end via `WebApplicationFactory`) and 12 unit (validator).

## Data

SQLite database file `tasks.db` is created automatically on first run in the project directory. The file is excluded from source control.

## Tech stack

- ASP.NET Core 10 Minimal API (`net10.0`)
- EF Core 10 + Microsoft.EntityFrameworkCore.Sqlite 10.0.12
- xUnit + FluentAssertions 8.x (community licence)

## Governance

Approval records for this pilot are in `ai-sdlc-starter-kit/plans/33/` (AR-33-001 through AR-33-005).
