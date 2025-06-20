<!--
This file outlines the architecture and coding standards for NexaFlow.
All team members must follow these guidelines.
-->

---

# Project Overview

NexaFlow is a multi-tenant SaaS platform with a **.NET 10 backend (Clean Architecture)** and an **Angular 22 frontend** (`nexaflow-web`).

**Backend layers (dependencies point inward):**
- `NexaFlow.Core` — domain Entities, Enums, Constants, shared Common. **Zero project dependencies.**
- `NexaFlow.Application` — DTOs, Validators (FluentValidation), Mapping (AutoMapper), service **interfaces** (`Common/Interfaces`). No EF Core / infrastructure concerns.
- `NexaFlow.Infrastructure` — interface **implementations**: Persistence (EF Core), Auth/Identity, Integrations, Jobs (Hangfire), Chatbot, PDF.
- `NexaFlow.API` — Minimal API Endpoints, SignalR Hubs, Middleware. Composition root + DI wiring.
- `NexaFlow.Tests` — xUnit tests for backend logic.

**Frontend (`nexaflow-web/src/app`):** `core/` (guards, interceptors, models, services, state, utils), `features/` (standalone, lazy-loaded), `shared/`, `layout/`.

---

# Section A — General Engineering Rules

## 1) Architecture & Separation of Concerns (YOU MUST FOLLOW)
- Follow the layer boundaries strictly. Backend: `API → Application → Core` and `Infrastructure → Application/Core`. Frontend: `components → services → API`.
- Dependencies point inward — `Core` depends on nothing; `Application` never references `Infrastructure` or EF Core; the API/UI layer holds ZERO business logic.
- Business logic lives in services (backend `Infrastructure/Services` behind `Application` interfaces; frontend `core/services` & feature services).
- Data access (EF Core, external APIs, storage) lives only in `Infrastructure` / the frontend `core` services layer.
- Do not introduce new abstractions or patterns (e.g. MediatR/CQRS) without justification — the project is service-based.

## 2) Shared Code (IMPORTANT)
- Backend: reusable domain logic/constants/enums used in 2+ places goes in `NexaFlow.Core`; cross-cutting app helpers in `NexaFlow.Application/Common`.
- Frontend: reusable logic/utilities/models used in 2+ features goes in `nexaflow-web/src/app/core` (or `shared/` for UI). Check these before creating new shared code — never duplicate across features.

## 3) Error Handling
- Errors flow cleanly across layers — never skip layers
- Handle null, empty, loading, and error states explicitly — no silent failures
- Catch errors at the boundary (data layer), not deep inside business logic

## 4) Change Discipline
- Make the smallest change that solves the problem
- Fix root causes, not symptoms
- Don't refactor unrelated code unless explicitly requested
- Never break existing functionality, APIs, flows, or UX unless explicitly instructed
- Read relevant code before modifying it — state assumptions when unclear

## 5) Dependencies
- Don't add new packages without justification
- Any new package must be: latest stable, well-maintained, production-grade

## 6) Security
- Never hardcode secrets, tokens, or credentials
- Never log sensitive information
- Validate all external and API input
- Proactively flag security risks when spotted

## 7) Testing
- Backend: write xUnit tests in `NexaFlow.Tests` for service/domain logic; run with `dotnet test`
- Frontend: write **vitest** specs for services and component logic; run with `npm test` (`ng test`)
- Bug fixes must include a reproducing test
- Tests must be deterministic — no flaky or timing-dependent tests; multi-tenant tests must assert tenant isolation
- One behavior per test case

## 8) Workflow (Mandatory)
- Code must pass CI checks before merging
- PR descriptions must always be in markdown (`.md`) format and explain the "Why"

---

# Section B — .NET Backend Rules

<!--
Follow standard C#/.NET conventions and the analyzers already enabled.
Rules below only cover things that OVERRIDE defaults or encode project decisions.
-->

## 1) Layering & Dependencies
- `NexaFlow.Core` has zero project references. `NexaFlow.Application` references only `Core`. `Infrastructure` and `API` reference inward only.
- Define service contracts as interfaces in `Application/Common/Interfaces`; implement them in `Infrastructure/Services`. Endpoints depend on the interface, never the concrete class.
- No MediatR/CQRS — keep the service-based pattern. Don't add new architectural layers without justification.

## 2) Multi-Tenancy (CRITICAL)
- Every tenant-scoped query and command MUST be filtered by the current tenant — never leak data across tenants.
- Resolve tenant context through the existing tenant service; never trust a tenant id from raw client input without validation.

## 3) API & Endpoints
- Use **Minimal API** endpoints grouped under `NexaFlow.API/Endpoints` — no MVC controllers.
- Validate all incoming DTOs with **FluentValidation** validators in `Application/Validators`.
- Map between entities and DTOs with **AutoMapper** profiles in `Application/Mapping` — no manual mapping in endpoints.

## 4) Persistence (EF Core)
- All DbContext/entity config lives in `Infrastructure/Persistence`. Schema changes go through EF Core migrations via `dotnet ef` (tool pinned in `dotnet-tools.json`).
- Never edit generated migration snapshots by hand. Create a new migration instead.

## 5) Background Jobs & Realtime
- Long-running / scheduled work uses **Hangfire** jobs in `Infrastructure/Jobs`.
- Realtime updates go through **SignalR** hubs in `NexaFlow.API/Hubs`.

## 6) Configuration & Logging
- Central Package Management — add/bump versions only in `Directory.Packages.props`, never inline `Version=` in `.csproj`.
- Nullable is enabled solution-wide — respect nullability annotations.
- Log with **Serilog**; never log secrets, tokens, passwords, or PII. Read secrets from configuration/env — never hardcode.

---

# Section C — Angular Frontend Rules (`nexaflow-web`)

<!--
Angular 22. Follow current Angular style guide. Rules below override defaults
or encode project decisions.
-->

## 1) Components & State
- Use **standalone** components (no NgModules). Prefer **signals** for component state; use RxJS for streams/HTTP.
- Components hold ZERO business logic — they render, observe state, and delegate to services.
- Feature routes are lazy-loaded; keep each feature self-contained under `features/{feature}`.

## 2) Services & Data Access
- All HTTP/API access goes through services in `core/services` (or feature services) — never call `HttpClient` directly from components.
- Cross-cutting concerns use the existing `core/interceptors` (auth/error) and `core/guards`. Reuse them; don't reimplement per feature.
- Realtime uses `@microsoft/signalr` wrapped in a service — not raw connections in components.

## 3) Models & Shared
- Shared DTO/model types live in `core/models` or `shared/models`. Reusable UI (e.g. dialogs) and pipes live in `shared/`.

## 4) Tooling
- Tests use **vitest** (`npm test`). Format with **prettier** before committing.
- Don't add npm packages without justification — latest stable, well-maintained only.
