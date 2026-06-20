# Azure Deployment Guide

How to provision the cloud resources NexaFlow runs on and wire up the
[`deploy.yml`](../.github/workflows/deploy.yml) pipeline. You run these `az`
commands once; afterwards every push to `main` deploys automatically.

> Stack: **.NET 10** API on Azure App Service (Linux), **Azure SQL** database,
> **Angular** on Azure Static Web Apps, EF Core migrations + deploy via GitHub Actions.

## Prerequisites

```bash
brew install azure-cli      # macOS; see docs.microsoft.com for other OSes
az login
```

Pick globally-unique names and a region, then export them so the snippets below
are copy-paste:

```bash
export RG=nexaflow-rg
export LOCATION=eastus
export PLAN=nexaflow-plan
export API=nexaflow-api                 # must be globally unique
export SQL_SERVER=nexaflow-sql-$RANDOM  # must be globally unique
export SQL_DB=nexaflow-db
export SQL_ADMIN=nexaflow_admin
export SQL_PASSWORD='<choose-a-strong-password>'
```

## 1. Resource group + App Service plan

```bash
az group create --name $RG --location $LOCATION

# F1 is free; move to B1 if you need always-on / custom domains.
az appservice plan create --name $PLAN --resource-group $RG --sku F1 --is-linux
```

## 2. API Web App (.NET 10)

```bash
az webapp create \
  --name $API --resource-group $RG --plan $PLAN \
  --runtime "DOTNETCORE:10.0"
```

## 3. Azure SQL

```bash
az sql server create \
  --name $SQL_SERVER --resource-group $RG --location $LOCATION \
  --admin-user $SQL_ADMIN --admin-password "$SQL_PASSWORD"

az sql db create \
  --resource-group $RG --server $SQL_SERVER \
  --name $SQL_DB --service-objective Basic

# Allow other Azure services (the App Service) to reach the database.
az sql server firewall-rule create \
  --resource-group $RG --server $SQL_SERVER \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
```

## 4. App settings & connection string

The API reads the connection string from `ConnectionStrings:Default` and config
from environment variables (double underscore = nested key). The connection-string
name **must be `Default`** and the JWT section is **`Jwt`** — these are what the
code looks up (`config.GetConnectionString("Default")`, section `"Jwt"`).

```bash
az webapp config connection-string set \
  --name $API --resource-group $RG \
  --connection-string-type SQLAzure \
  --settings Default="Server=tcp:$SQL_SERVER.database.windows.net,1433;Initial Catalog=$SQL_DB;User ID=$SQL_ADMIN;Password=$SQL_PASSWORD;Encrypt=True;TrustServerCertificate=False;"

az webapp config appsettings set \
  --name $API --resource-group $RG \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    DemoData__Enabled="true" \
    Jwt__SecretKey="<a-random-secret-at-least-32-chars>" \
    App__AllowedOrigins__0="https://<your-static-web-app>.azurestaticapps.net"
```

> `App__AllowedOrigins__0` must be the SPA's URL, or CORS blocks the frontend.
> `Jwt__Issuer`/`Jwt__Audience` already have sane defaults; only `Jwt__SecretKey`
> is required (≥ 32 chars).
>
> `DemoData__Enabled=true` makes the API seed the **Demo Company** tenant on first
> start (login `demo@nexaflow.com` / `Demo@2025`). Set it to `false` once you no
> longer want the demo data re-created. Never hardcode secrets — keep
> `Jwt__SecretKey` and the SQL password in App Service settings only.

## 5. Static Web App (Angular)

Easiest from the Azure Portal → **Create resource → Static Web App**:

- **Source:** GitHub → this repo, branch `main`
- **App location:** `/nexaflow-web`
- **Output location:** `dist/nexaflow-web/browser`

Then set the frontend's `environment.apiBaseUrl` to
`https://<API>.azurewebsites.net/api` before building, or configure it via the
Static Web App. Grab the deployment token from **Manage deployment token**.

## 6. GitHub secrets

`deploy.yml` needs three repo secrets (Settings → Secrets and variables → Actions):

| Secret | Where to get it |
| --- | --- |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | `az webapp deployment list-publishing-credentials ...`, or Portal → the API → **Get publish profile** (paste the whole XML) |
| `AZURE_STATIC_WEB_APPS_TOKEN` | Static Web App → **Manage deployment token** |
| `PROD_DB_CONNECTION` | the same SQL connection string as step 4 (the migration step maps it to `ConnectionStrings__Default`) |

## 7. First deploy

Push to `main`. The pipeline builds + tests both apps, applies EF Core migrations
to Azure SQL, then deploys the API and the Angular bundle.

Verify:

- API health: `https://<API>.azurewebsites.net/health` → `{ "status": "healthy" }`
- App: your Static Web App URL → log in with the demo account.

> Swagger/OpenAPI is intentionally exposed in the **Development** environment only,
> so there is no `/swagger` on the Production deployment.

## Troubleshooting

- **Migrations fail** — confirm `PROD_DB_CONNECTION` is valid and the firewall rule exists.
- **App returns 500 on boot** — check `Jwt__SecretKey` is set and ≥ 32 chars.
- **CORS errors from the SPA** — ensure the API's allowed origins include the Static Web App URL.
- **F1 plan sleeps / throttles** — expected on the free tier; upgrade to B1 for always-on.
