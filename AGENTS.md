# AGENTS.md

Repository instructions for Codex and other coding agents working in this
codebase.

## Project Overview

This repository contains an inventory management application with a .NET 8
ASP.NET Core Web API backend and a separate React/Vite frontend.

The backend follows Clean Architecture with vertical feature slices:

- `InventoryManagement.API`: HTTP boundary, controllers, middleware, startup
  configuration, CORS, Swagger, rate limiting, health checks, and auth policy
  registration.
- `InventoryManagement.Application`: business use cases, CQRS requests,
  handlers, validators, response contracts, mapping helpers, authorization
  constants, and application abstractions.
- `InventoryManagement.Domain`: entities and enums only.
- `InventoryManagement.Infrastructure`: EF Core DbContext, migrations,
  repositories, ASP.NET Identity implementation, services, and persistence
  integrations.
- `InventoryManagement.Tests`: unit and integration tests grouped by feature.
- `inventory-web`: React + TypeScript + Vite SPA, organized by pages, feature
  modules, and shared utilities/components.

The database is PostgreSQL through EF Core/Npgsql. Local development uses
`inventorydatabase_dev` through
`InventoryManagement.API/appsettings.Development.json`.

## Architecture Rules

- Keep changes scoped to the requested vertical slice.
- Do not refactor unrelated modules while implementing a feature or bug fix.
- Prefer existing feature-folder patterns over introducing new architecture.
- Controllers should stay thin and delegate work to MediatR commands/queries.
- Application feature folders usually contain `Command.cs` or `Query.cs`,
  `Handler.cs`, `Validator.cs`, and sometimes `Response.cs` or mapping helpers.
- Business validation belongs in FluentValidation validators or handlers,
  depending on whether it needs persistence access.
- Persistence-specific code belongs in Infrastructure repositories or
  `ApplicationDbContext`, not in API controllers.
- Domain entities should not depend on API, Infrastructure, or frontend code.
- Use existing repository interfaces under
  `InventoryManagement.Application/Common/Persistence` when adding persistence
  behavior.
- Keep frontend API contracts in the matching `inventory-web/src/features/*/*Api.ts`
  file and mirror backend response/request shapes carefully.

## Backend Conventions

- Target framework is `net8.0`; nullable reference types and implicit usings are
  enabled.
- Use MediatR `IRequest<TResponse>` handlers for application operations.
- Use FluentValidation and the existing validation pipeline for request
  validation.
- Throw existing application exceptions such as `BadRequestException`,
  `NotFoundException`, `ForbiddenException`, or the custom validation exception
  so `GlobalExceptionMiddleware` can format responses consistently.
- Preserve authorization policies defined in
  `InventoryManagement.Application/Authorization/AuthorizationPolicies.cs` and
  registered in `InventoryManagement.API/DependencyInjection.cs`.
- Use UTC timestamps for persisted audit fields.
- Keep money precision and rounding behavior consistent with nearby handlers.
- When changing EF Core entity shape, add a migration under
  `InventoryManagement.Infrastructure/Migrations`.
- Do not bypass `ApplicationDbContext` transaction patterns already used by the
  relevant repository/handler.

## Frontend Conventions

- The frontend is a separate Vite app in `inventory-web`.
- Route definitions live in `inventory-web/src/app/App.tsx`.
- Auth state and current-user loading live in
  `inventory-web/src/features/auth/AuthContext.tsx`.
- Role-gated frontend access is centralized in
  `inventory-web/src/features/auth/roleAccess.ts`.
- Shared API behavior, token refresh, and error wrapping live in
  `inventory-web/src/shared/api/apiClient.ts`.
- Feature-specific API calls and TypeScript contracts belong beside that feature
  under `inventory-web/src/features`.
- Pages live under `inventory-web/src/pages`.
- Shared display formatting belongs in
  `inventory-web/src/shared/utils/formatters.ts`.
- Preserve existing UI wording, table order, and route behavior unless the task
  explicitly asks for a design/content change.
- Do not re-add inactive frontend routes or navigation entries unless requested.

## Important Business Rules

- Draft documents should not affect stock or balances.
- Posting a purchase increases stock and updates weighted-average cost.
- Posting a delivery challan decreases stock without creating customer debt.
- Posting a direct sales invoice decreases stock and updates customer debt.
- Creating an invoice from posted challans must not reduce stock a second time.
- Posted document cancellation should preserve history through reversal
  movements.
- Payments and payment reversals must keep balances consistent.
- Gross profit should use historical costs stored with sales/returns, not the
  product's current average cost.
- Company profile is stored as an Admin-only singleton and should not be wired
  into PDFs or other documents unless explicitly requested.

## Current Feature Notes

- Backend delivery challan support still exists.
- The active frontend route table currently does not expose `/app/challans`.
- Sales invoice UI and backend behavior have existing support for manual invoice
  numbers, invoice dates, delivery address, driver, other charges, labor charge,
  and delivery-charge paid state.
- Driver delivery history uses sales invoices with driver charge or labor charge
  information.
- Party ledgers and PDF generation are implemented in the frontend using
  `pdfmake`.

## Configuration

- Backend configuration files are in `InventoryManagement.API`:
  `appsettings.json`, `appsettings.Development.json`,
  `appsettings.Testing.json`, and `appsettings.Production.json`.
- Frontend runtime build configuration uses `VITE_API_BASE_URL`.
- Do not put secrets in any `VITE_` variable because Vite embeds them into
  browser assets.
- Production startup requires explicit JWT, CORS, database, rate-limit, and
  reverse-proxy settings. See `README` and `production.env.example`.
- Production automatic migrations are disabled by default. Apply migrations as a
  controlled deployment step unless a task explicitly changes that policy.
- PostgreSQL backup/restore guidance is documented in
  `docs/postgresql-backup-restore.md`.

## Build And Test Commands

Use focused verification whenever possible.

Backend build:

```powershell
dotnet build InventoryManagement.sln --disable-build-servers -m:1 -p:RestoreDisableParallel=true
```

All backend tests:

```powershell
dotnet test InventoryManagement.sln --disable-build-servers -m:1 -p:RestoreDisableParallel=true
```

Focused backend tests:

```powershell
dotnet test InventoryManagement.Tests --filter "FullyQualifiedName~<FeatureOrTestName>"
```

Frontend build:

```powershell
cd inventory-web
npm run build
```

Frontend lint:

```powershell
cd inventory-web
npm run lint
```

Known environment note: Vite/esbuild `spawn EPERM` can be sandbox-related. If
the command fails only with that symptom, rerun the same command outside the
sandbox before diagnosing source code.

## Git And Workspace Safety

- Check `git status --short` before making edits.
- Preserve user changes in the working tree. Do not revert or overwrite changes
  you did not make.
- Do not use destructive Git commands such as `git reset --hard` or
  `git checkout --` unless explicitly requested.
- Keep generated build outputs, logs, local databases, and dependency folders out
  of unrelated changes.
- Do not edit `.env` with real secrets unless the user explicitly asks.

## Documentation To Read When Relevant

- `README`: project overview, workflow rules, API route summary, Docker and
  production guidance.
- `docs/postgresql-backup-restore.md`: database backup and restore process.
- `inventory-web/README.md`: frontend development, build, and Docker hosting.
- `.github/workflows/dotnet.yml`: CI build/test expectations.

## Before Implementing

Before making code changes:

1. Read this file.
2. Check `git status --short`.
3. Inspect the current implementation for the specific feature being changed.
4. Confirm backend request/response contracts and frontend TypeScript contracts
   match.
5. Identify the smallest safe vertical slice.
6. State clearly which verification commands were run. If tests were not run,
   say so and provide the focused command the user can run.
