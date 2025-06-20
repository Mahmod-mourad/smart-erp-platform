# NexaFlow

**AI-powered, multi-tenant SaaS ERP** for small & medium businesses — **.NET 10 (Clean Architecture) API** + **Angular 22 SPA**, with JWT auth, ASP.NET Core Identity, and per-tenant data isolation.

> **Demo account:** `demo@nexaflow.com` / `Demo@2025` (seeded automatically when `DemoData:Enabled` is on — see [docs/AZURE_SETUP.md](docs/AZURE_SETUP.md)).

## ✨ Features

- 🔐 **Multi-tenant SaaS** — register a company, isolated per-tenant data via EF Core global query filters
- 📇 **CRM** — customers, leads, a drag-and-drop pipeline (Kanban) and activity timeline
- 👥 **HR** — employees, attendance, leave requests, payroll & payslips
- 📦 **Inventory** — products and stock movements with a full audit trail
- ⚡ **Automation engine** — no-code "when [trigger] → run [actions]" rules on Hangfire
- 🔌 **Integrations** — Email, WhatsApp, Slack, Google Sheets (per-tenant credentials, encrypted at rest)
- 🤖 **AI chatbot** — answers business questions via a local Ollama model (zero API cost)
- 🧠 **ML.NET predictions** — sales forecast (SSA time-series), customer churn risk (FastTree), stock depletion
- 📊 **Dashboard** — KPI cards, forecast chart, churn list and stock alerts in one view

---

## 🏗️ Architecture

```
NexaFlow.API            → presentation: minimal-API endpoints, middleware, DI, auth
NexaFlow.Application    → use cases: DTOs, interfaces, validators, mapping  (no infra deps)
NexaFlow.Infrastructure → EF Core, Identity, JWT, services, persistence
NexaFlow.Core           → domain: entities, enums, constants  (zero dependencies)
NexaFlow.Tests          → xUnit + Moq + FluentAssertions (in-memory EF)
nexaflow-web            → Angular 22 standalone SPA (signals, lazy routes, Material 3)
```

Dependency rule: `API → Infrastructure → Application → Core`. The domain never depends outward.
Data model & multi-tenancy strategy: [docs/erd.md](docs/erd.md).

## 🧰 Tech stack
| Layer | Tech |
|---|---|
| API | .NET 10, Minimal APIs, ASP.NET Core Identity, JWT Bearer, Serilog, FluentValidation, AutoMapper |
| Data | EF Core 10 + SQL Server (Azure SQL Edge locally) |
| Frontend | Angular 22, Angular Material 3, RxJS, standalone + signals, Chart.js |
| AI / ML | ML.NET (SSA forecasting, FastTree churn), Ollama (local LLM) |
| Realtime / Jobs | SignalR, Hangfire |
| DevOps | Docker Compose, GitHub Actions CI + CD, Azure App Service + Static Web Apps |

---

## 🚀 Getting started (≤ 10 steps)

**Prerequisites:** .NET 10 SDK · Node 20+ · Docker · (`dotnet-ef` is restored as a local tool).

> The whole stack below is verified working end-to-end: DB migrated, API healthy, test suite green, SPA serving on :4200.

```bash
# 1. Clone
git clone https://github.com/Mahmod-mourad/nexaflow.git && cd nexaflow

# 2. Start the database (Azure SQL Edge — runs natively on Apple Silicon)
cp .env.example .env             # SA_PASSWORD must match appsettings → Your_strong_Passw0rd
docker compose up -d sqledge     # (`docker compose up -d` also starts Ollama, a large optional LLM image)

# 3. Set the API JWT secret (dev secrets — must be ≥ 32 chars)
dotnet user-secrets set "Jwt:SecretKey" "a-long-random-dev-secret-at-least-32-bytes-please-0123456789" --project NexaFlow.API
#    (ConnectionStrings:Default already targets localhost,1433 with TrustServerCertificate)

# 4. Apply the database migration (wait ~30s after step 2 for SQL Server to finish booting)
dotnet tool restore
dotnet dotnet-ef database update -p NexaFlow.Infrastructure -s NexaFlow.API

# 5. Run the API  → http://localhost:5283  (Swagger UI at /swagger, health at /health)
dotnet run --project NexaFlow.API

# 6. Run the tests (separate terminal)
dotnet test

# 7. Frontend deps
cd nexaflow-web && npm install

# 8. Start the SPA → http://localhost:4200
npm start
```

Open <http://localhost:4200>, click **Create a company**, and you're in.

**Smoke-test the API directly** (the SPA does the same under the hood):

```bash
curl -X POST http://localhost:5283/api/auth/register-company \
  -H "Content-Type: application/json" \
  -d '{"companyName":"Acme Inc","adminFirstName":"Ada","adminLastName":"Lovelace","adminEmail":"admin@acme.test","password":"Passw0rd!"}'
# → 200 with { accessToken, refreshToken, user{ roles:["CompanyAdmin"], … } }
```

> **Note:** migrations are driven by `NexaFlow.Infrastructure/Persistence/AppDbContextFactory.cs`, which uses the same local-dev connection string as the API (override with the `ConnectionStrings__Default` env var if your SQL host/password differ).

---

## 📁 API surface

Minimal-API endpoint groups under `NexaFlow.API/Endpoints` (all bearer-auth unless noted):

| Group | Base route |
|---|---|
| Auth (register/login/refresh/invite) | `/api/auth` *(anon)* |
| Tenants · Invitations | `/api/tenants` · `/api/invitations` |
| CRM | `/api/customers` · `/api/leads` |
| HR | `/api/employees` · `/api/attendance` · `/api/leaves` · `/api/payroll` |
| Inventory | `/api/products` |
| Automation · Integrations · Chat | `/api/automation` · `/api/integrations` · `/api/chat` |
| **Predictions** | `/api/predictions/{sales-forecast, churn-risk, stock-depletion, dashboard-summary}` |
| Health | `/health` *(anon)* |

OpenAPI/Swagger UI is served at `/swagger` in **Development**. A ready-to-use Postman collection is in [docs/NexaFlow.postman_collection.json](docs/NexaFlow.postman_collection.json) (login/register auto-capture the token).

## 🔐 Roles
`SuperAdmin` (platform) · `CompanyAdmin` · `Manager` · `Employee` — see [docs/erd.md](docs/erd.md).

## 🧪 Testing & CI/CD
`dotnet test` runs the backend xUnit suite (auth flows, multi-tenant isolation, automation engine, ML predictions, demo seeding); `npm test` runs the Angular vitest specs. CI ([.github/workflows/ci.yml](.github/workflows/ci.yml)) builds & tests backend + frontend on every PR. CD ([.github/workflows/deploy.yml](.github/workflows/deploy.yml)) applies EF Core migrations and deploys the API (Azure App Service) + SPA (Azure Static Web Apps) on push to `main` — setup in [docs/AZURE_SETUP.md](docs/AZURE_SETUP.md). Branch protection: [docs/setup/branch-protection.md](docs/setup/branch-protection.md).

## 🤝 Contributing
Branch from `dev` as `feature/T-0xx-short-desc`, open a PR (template auto-loads), keep CI green.
