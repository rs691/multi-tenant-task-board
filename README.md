# Multi-Tenant Task Board API

A minimal ASP.NET Core Web API demonstrating tenant-isolated data access —
the same multi-tenant architecture pattern used in production systems I've
built with Node.js and Laravel, implemented here in C#/.NET.

## What this demonstrates

- **Multi-tenant isolation via EF Core global query filters** — every query
  against `Tasks` is automatically scoped to the current tenant. No endpoint
  can accidentally leak another tenant's data by forgetting a `WHERE` clause;
  the isolation lives in one place (`TaskBoardContext`), not scattered across
  controllers.
- **JWT-based auth** with a `tenant_id` claim that drives the isolation.
- **Minimal API** structure (no MVC ceremony) with EF Core + SQLite for a
  zero-setup local dev experience.
- **CI pipeline** (GitHub Actions) that restores, builds, and tests on every
  push.

## Running locally

```bash
dotnet restore
dotnet run
```

The app auto-creates the SQLite database and seeds two demo tenants on first
run.

## Trying it out

1. `GET /tenants` — grab a tenant id from the seeded list
2. `POST /auth/token/{tenantId}` — get a JWT scoped to that tenant
3. Use the token as a Bearer header, `POST /tasks` to create a few tasks
4. Swap to the *other* tenant's token and `GET /tasks` — it returns none of
   the first tenant's data. That's the isolation working.

## Stack

ASP.NET Core (Minimal APIs) · EF Core · SQLite · JWT Bearer Auth · GitHub
Actions CI

## Why this exists

Built to demonstrate the same multi-tenant architecture pattern I've shipped
in production (Node.js/Express + MySQL, Laravel + MySQL) in the .NET stack —
proving the pattern transfers, not just the syntax.
