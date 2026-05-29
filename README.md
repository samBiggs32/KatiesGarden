<a name="readme-top"></a>

<div align="center">
  <a href="https://github.com/samBiggs32/KatiesGarden">
    <img src="\Images\Katies-Garden.gif" alt="Logo" width="80" height="80">
  </a>

  <h1 align="center">Katie's Garden</h1>

  <p align="center">
    A modern web application for a family-run garden business in Milverton, Taunton, UK
    <br />
    <a href="https://github.com/samBiggs32/KatiesGarden"><strong>View Repository »</strong></a>
    <br />
    <br />
    <a href="https://github.com/samBiggs32/KatiesGarden/issues">Report Bug</a>
    ·
    <a href="https://github.com/samBiggs32/KatiesGarden/issues">Request Feature</a>
  </p>
</div>

## 📋 Table of Contents

- [About The Project](#about-the-project)
  - [Built With](#built-with)
- [Features](#features)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Quick start (Aspire)](#quick-start-aspire)
  - [Running just the frontend](#running-just-the-frontend)
  - [Running the tests](#running-the-tests)
  - [Verifying the local stack](#verifying-the-local-stack)
- [Configuration reference](#configuration-reference)
  - [Database (Neon PostgreSQL)](#database-neon-postgresql)
  - [Email sending (Brevo)](#email-sending-brevo)
  - [Online shop (Stripe)](#online-shop-stripe)
  - [Product image storage (Azure Blob)](#product-image-storage-azure-blob)
  - [Push notifications (VAPID)](#push-notifications-vapid)
  - [Admin login (OAuth)](#admin-login-oauth)
    - [Testing admin login locally](#testing-admin-login-locally)
- [Operations](#-operations)
  - [Live readiness check](#live-readiness-check)
  - [Logs](#logs)
- [Infrastructure](#-infrastructure)
  - [Provisioning with Terraform](#provisioning-with-terraform)
  - [GitHub Actions Secrets](#github-actions-secrets)
  - [Cloudflare](#cloudflare)
- [Development Roadmap](#development-roadmap)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)

---

## 🌱 About The Project

Katie's Garden is a family-run garden centre and landscaping service based in Milverton, Taunton, UK. This web application showcases projects, services, and products while providing an online shopping experience for customers.

### Built With

| Layer | Technology |
|---|---|
| Frontend | [Blazor WebAssembly (.NET 10)](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) + [MudBlazor](https://mudblazor.com) |
| API | [Azure Functions v4 isolated worker (.NET 9)](https://learn.microsoft.com/en-us/azure/azure-functions/) |
| Database | [Neon serverless PostgreSQL](https://neon.tech) via EF Core |
| Payments | [Stripe Checkout](https://stripe.com/docs/payments/checkout) |
| Email | [MailKit](https://github.com/jstedfast/MailKit) + [Brevo SMTP](https://www.brevo.com) |
| Image storage | [Azure Blob Storage](https://azure.microsoft.com/en-us/products/storage/blobs) |
| Hosting | [Azure Static Web Apps (Standard)](https://azure.microsoft.com/en-us/services/app-service/static/) |
| CDN / WAF | [Cloudflare](https://www.cloudflare.com) (free tier) |
| Local orchestration | [.NET Aspire 9](https://learn.microsoft.com/en-us/dotnet/aspire/) |
| Infrastructure | [Terraform ≥ 1.7](https://www.terraform.io) |
| Unit tests | [xUnit.v3](https://xunit.net) + [FluentAssertions](https://fluentassertions.com) |
| E2E tests | [Playwright NUnit](https://playwright.dev/dotnet/) |

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## ✨ Features

- **Home page** — seasonal highlights, business introduction, responsive carousel
- **Gallery** — woodwork, bugs & hugs, and maintenance project showcases
- **Contact page** — enquiry form with server-side validation and email delivery
- **Newsletter sign-up** — subscriber list synced to Brevo contact list
- **Online shop** — browse collections and products, add to basket, proceed to Stripe Checkout
- **Admin panel** (`/admin`) — order management, product and collection CRUD, delivery settings, push notification toggle
- **Push notifications** — Katie receives a browser push when a new order is placed
- **Progressive Web App** — service worker, offline shell, installable on mobile

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## 🚀 Getting Started

### Prerequisites

| Tool | Why |
|---|---|
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | Builds the Blazor client and runs tests (.NET 9 runtime for the Functions API is included) |
| [Docker Desktop](https://docs.docker.com/get-docker/) | Aspire spins up a local Postgres container; also required for integration tests |
| [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local) | Aspire launches the Functions host via the `func` CLI |
| [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) (optional) | Local Azure Blob Storage emulator for product image uploads. Install with `npm install -g azurite` |
| Git | Source control |
| VS Code or Visual Studio 2022 | Recommended editors |

### Quick start (Aspire)

Clone the repo, copy the example settings, then run the full stack:

```sh
git clone https://github.com/samBiggs32/KatiesGarden.git
cd KatiesGarden

# Aspire needs only the worker runtime locally — the AppHost injects everything else
cp Api/local.settings.json.example Api/local.settings.json

dotnet run --project AppHost/KatiesGarden.AppHost.csproj
```

Aspire starts Postgres, the Functions API (pinned to **port 7071**), and the Blazor dev server in one command. A dashboard URL is printed in the console (e.g. `https://localhost:17158`) — open it to see all services.

> **Keep `local.settings.json` minimal when running via Aspire.** Per Microsoft's
> [Azure Functions + Aspire guidance](https://learn.microsoft.com/azure/azure-functions/dotnet-aspire-integration),
> the only value it should contain is `FUNCTIONS_WORKER_RUNTIME` — the AppHost injects
> `DATABASE_URL`, `SMTP_*`, `STRIPE_*`, and `SITE_URL` as environment variables. In
> particular, **do not set `AzureWebJobsStorage`**: `AddAzureFunctionsProject` provisions
> that connection for you, and hard-coding `UseDevelopmentStorage=true` points the host at a
> local Azurite that usually isn't running — which makes the Functions host **fail to start
> with exit code `0x80008081` before any of your code logs**. Other integrations (Brevo,
> Blob, VAPID) are left unset locally and report `not_configured` — not a failure.
>
> For a standalone `func start` (without Aspire), add the settings you need from the
> [Configuration reference](#-configuration-reference) to `local.settings.json` yourself.

> **Open the `web` resource's HTTP endpoint (`http://localhost:5000`), not the HTTPS one.**
> The Blazor app is a standalone WebAssembly SPA: it runs in the browser and cannot read
> Aspire's service-discovery variables, so in Development it is wired directly to the
> Functions host at `http://localhost:7071` (via `wwwroot/appsettings.Development.json`).
> A page served over **https** calling an **http** API is blocked by the browser as mixed
> content — so use the HTTP web endpoint locally. In production this is a non-issue: SWA
> serves the app and `/api/*` from the same HTTPS origin, and `ApiBaseUrl` is left empty.

> For the **full single-origin experience** (no CORS, no mixed-content caveat, and working
> `/.auth/*` admin login), run behind the SWA emulator instead — see
> [Testing admin login locally](#testing-admin-login-locally). That is the closest local
> mirror of production; plain Aspire is best for backend/dashboard and DB work.

### Running just the frontend

If you only want to work on the Blazor UI against a deployed or local API, run the WASM
dev server directly:

```sh
dotnet run --project Web/KatiesGarden.Web/Client/KatiesGarden.Web.Client.csproj
```

Point it at any API by setting `ApiBaseUrl` in
`Web/KatiesGarden.Web/Client/wwwroot/appsettings.Development.json` (e.g. `http://localhost:7071`
for a local `func start`, or a deployed URL). Leaving it empty makes the app call `/api/*`
on its own origin — which is what production (SWA) uses.

### Running the tests

**Unit tests** (fast, no Docker required):

```sh
dotnet test Tests/KatiesGarden.Tests/ --filter "Category!=Integration"
```

**Integration tests** (Aspire + Testcontainers; requires Docker):

```sh
dotnet test Tests/KatiesGarden.Tests/ --filter "Category=Integration"
```

**E2E tests** (Playwright; targets a running site):

```sh
# Install Playwright browsers the first time
pwsh Tests/KatiesGarden.E2E/bin/Debug/net10.0/playwright.ps1 install chromium

# Run against production
PLAYWRIGHT_BASE_URL=https://www.katiesgarden.uk \
  dotnet test Tests/KatiesGarden.E2E/ -- NUnit.NumberOfTestWorkers=1

# Or against a local Aspire stack
PLAYWRIGHT_BASE_URL=http://localhost:4280 \
  dotnet test Tests/KatiesGarden.E2E/ -- NUnit.NumberOfTestWorkers=1
```

> E2E tests must run single-threaded per worker (`NUnit.NumberOfTestWorkers=1`) because Playwright shares browser state within a worker.

### Verifying the local stack

After `dotnet run --project AppHost/...`:

1. **Console prints a dashboard URL** like `https://localhost:17158` — open it
2. **Dashboard "Resources" tab shows 4 resources** — `postgres`, `katiesgardendb`, `api`, `web` — each goes **Running** then **Healthy** (Postgres takes ~10 s the first time while the image pulls)
3. **Health state reflects live integrations** — each resource has a health probe:
   - `postgres` — Aspire's built-in Postgres readiness check
   - `api` — probes **`/api/diagnostics`**, which is only "ready" when the database is reachable and every *configured* integration (Stripe, Blob Storage, Brevo) responds to a **read-only** call. Locally these are placeholders, so they report `not_configured` (not a failure) and `api` still goes Healthy. With real credentials the probe genuinely verifies each integration is live.
   - `web` — probes `/` (the dev server is serving)
4. **Inspect the integration checks directly**:
   ```sh
   curl http://localhost:7071/api/diagnostics | jq
   # {"status":"ready","checks":{"api":"ok","database":"ok","brevo_api":"not_configured",
   #  "stripe":"not_configured","blob_storage":"not_configured","smtp":"skipped"}, ...}

   # Add ?checkSmtp=true to also verify SMTP credentials are live. This connects +
   # authenticates + disconnects — it NEVER sends a message, so no send quota is used.
   curl "http://localhost:7071/api/diagnostics?checkSmtp=true" | jq
   ```
   Every check is read-only and free: `database` runs `SELECT 1`, `brevo_api` reads the account, `stripe` retrieves the balance (no charge), `blob_storage` reads account properties (no writes), and `smtp` only logs in. Nothing here bills against any account or spends email quota.
5. **`web` opens** — click the **HTTP** endpoint (`http://localhost:5000`) on the `web` row; the Blazor app should load with shop/cart/admin links working against your local API on `:7071`

**Troubleshooting**

- **`api` fails with `dotnet ---port does not exist` / `Could not execute because the specified command or file was not found`** (Postgres starts fine, only `api` dies) — Aspire's `AddAzureFunctionsProject` launches the Functions host via `func host start`. When it **can't find `func`**, the command degrades to a malformed `dotnet --port {n}` (the port is an Aspire-assigned dynamic one, e.g. `61483`, not 7071). The cause is almost always **PATH inheritance**, not a missing install:

  > **`func` working in your terminal does NOT mean Visual Studio sees it.** `devenv.exe` caches the system PATH from when it started. If `func` was installed/added to PATH *after* VS was already open, VS — and the Aspire AppHost it spawns — is blind to it.

  **Fastest fix (no reboot):** run the AppHost from a terminal where `func --version` works — it inherits that terminal's PATH:
  ```sh
  dotnet run --project AppHost/KatiesGarden.AppHost.csproj
  ```
  **Permanent fix for VS F5:** install the tools, then **fully exit** Visual Studio (not just "Restart") and reopen — a reboot is surest:
  ```powershell
  func --version            # 4.x.xxxx if installed
  where.exe func            # resolved path, or "Could not find"
  winget install Microsoft.Azure.FunctionsCoreTools   # if missing
  ```
  The Functions launch profile lives in `Api/Properties/launchSettings.json` and pins the standalone port to 7071. See [microsoft/aspire #7010](https://github.com/microsoft/aspire/issues/7010).
- **`api` launches (you see `func`) but the project exits immediately with code `0x80008081` / `-2147450751`, and the `/api/diagnostics` health check throws `TaskCanceledException`** — the Functions host crashed during startup, so nothing is listening and the probe times out. The usual cause is `AzureWebJobsStorage` being set to `UseDevelopmentStorage=true` in `local.settings.json` while Azurite isn't running. **Remove `AzureWebJobsStorage` from `local.settings.json`** — Aspire's `AddAzureFunctionsProject` provisions it for you. Keep the file minimal (only `FUNCTIONS_WORKER_RUNTIME`); the AppHost injects the rest. If you see no `Katie's Garden API starting up` log at all, the crash is here — before our code runs.
- **`api` starts but never goes Healthy (stays yellow), so `web` never launches** — `/api/diagnostics` is returning 503. A *configured-but-unreachable* integration reports `fail` (→ 503), whereas an *unset* one reports `not_configured` (fine). The common culprit is `AZURE_STORAGE_CONNECTION_STRING=UseDevelopmentStorage=true` with no Azurite running — leave it empty locally so Blob Storage reports `not_configured`. Hit `http://localhost:7071/api/diagnostics` to see which check is failing.
- **Shop/cart show errors or "unexpected character `<`"** — the browser is calling the wrong origin. Make sure you opened the **HTTP** `web` endpoint (not HTTPS), so the WASM app can reach `http://localhost:7071` without mixed-content blocking.
- **Dashboard never appears** — check Docker Desktop is running (`docker info`).

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## ⚙️ Configuration reference

All configuration is read from environment variables. Locally these live in `Api/local.settings.json` (gitignored). In production they are set as **SWA application settings** by Terraform. Copy `Api/local.settings.json.example` to `Api/local.settings.json` and fill in the values below.

### Database (Neon PostgreSQL)

Newsletter subscribers, shop products, collections, and orders are stored in a free-tier [Neon](https://neon.tech) serverless PostgreSQL database.

1. Sign up at [neon.tech](https://neon.tech) — no credit card required
2. Create a project, then copy the **connection string** from the dashboard

```
DATABASE_URL=postgresql://user:pass@host.neon.tech/katiesgarden?sslmode=require
```

All tables are created automatically on first cold start via `EnsureCreated()` and an idempotent `CREATE TABLE IF NOT EXISTS` migration that runs on every startup — safe on both new and existing databases. The tables created are: `collections`, `products`, `orders`, `order_lines`, `delivery_settings`, `push_subscriptions`, `advertising_content`, and `subscribers`.

> If `DATABASE_URL` is not set, the API degrades gracefully — contact form emails still send, but nothing is persisted.

### Email sending (Brevo)

The contact form sends email via SMTP. The newsletter sign-up syncs subscribers to a Brevo contact list via the REST API.

**Recommended: Brevo (free tier, 300 emails/day)**

1. Sign up at [brevo.com](https://www.brevo.com) — no credit card required
2. Go to **Senders & IPs → Domains**, add `katiesgarden.uk`, and complete DNS verification
3. Go to **SMTP & API → SMTP** and generate an **SMTP key** (for contact form emails)
4. Go to **SMTP & API → API Keys** and generate a separate **REST API key** (for newsletter syncing)
5. Go to **Contacts → Lists**, create a list (e.g. "Website Newsletter"), and note its numeric ID

```
SMTP_HOST=smtp-relay.brevo.com
SMTP_PORT=587
SMTP_USERNAME=your-brevo-login-email@example.com  # your Brevo account email
SMTP_PASSWORD=xsmtpsib-...                        # the generated SMTP key, not your login password
SENDER_EMAIL=noreply@katiesgarden.uk
RECIPIENT_EMAIL=team@katiesgarden.uk
BREVO_API_KEY=your-brevo-rest-api-key
BREVO_LIST_ID=1                                   # numeric list ID from Brevo → Contacts → Lists
```

**Alternative: any SMTP provider**
If `team@katiesgarden.uk` is hosted via Google Workspace, Microsoft 365, or a domain host (e.g. Krystal), use their SMTP credentials instead. Check your host's "outbound SMTP" settings page.

### Online shop (Stripe)

The shop uses **Stripe Checkout** — the API creates a Checkout Session and returns a redirect URL; no card data ever touches the server.

1. Sign up at [stripe.com](https://stripe.com) and go to the [Dashboard](https://dashboard.stripe.com)
2. Use **test mode** (toggle in the top-left) while developing — test card `4242 4242 4242 4242`, any future date, any CVC
3. Copy your **Secret key** from **Developers → API keys**
4. Set up a webhook:
   - Go to **Developers → Webhooks → Add endpoint**
   - URL: `https://www.katiesgarden.uk/api/webhooks/stripe` (or your ngrok URL locally)
   - Events to listen for: `checkout.session.completed`
   - Copy the **Signing secret** (`whsec_...`) after saving
5. For local development, use the [Stripe CLI](https://stripe.com/docs/stripe-cli) to forward events:
   ```sh
   stripe listen --forward-to http://localhost:7071/api/webhooks/stripe
   # The CLI prints a signing secret — use that as STRIPE_WEBHOOK_SECRET locally
   ```

```
STRIPE_SECRET_KEY=sk_test_...      # or sk_live_... in production
STRIPE_WEBHOOK_SECRET=whsec_...
SITE_URL=http://localhost:4280     # used for Stripe Checkout success/cancel redirect URLs
```

> `SITE_URL` must match the origin you browse from so Stripe can redirect back: use
> `http://localhost:4280` under the SWA emulator, `http://localhost:5000` under plain Aspire,
> and `https://www.katiesgarden.uk` in production.

> When you go live, swap the test keys for live keys and update the webhook endpoint URL.

### Product image storage (Azure Blob)

Product images are uploaded via the admin panel and stored in Azure Blob Storage (public read, authenticated write).

**Local development** — Azurite emulates Blob Storage with no Azure account needed:

```sh
# Start Azurite (if not already running)
azurite --location /tmp/azurite --debug /tmp/azurite.log &
```

```
AZURE_STORAGE_CONNECTION_STRING=UseDevelopmentStorage=true
AZURE_STORAGE_CONTAINER=product-images
```

**Production** — create a Storage Account in Azure (or let Terraform create it), then copy the connection string:

1. In the [Azure Portal](https://portal.azure.com), find your Storage Account
2. Go to **Security + networking → Access keys** and copy **Connection string** for key1
3. The container (`product-images` by default) is created automatically on first upload

```
AZURE_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;AccountName=...
AZURE_STORAGE_CONTAINER=product-images
```

> If `AZURE_STORAGE_CONNECTION_STRING` is not set, the image upload endpoint returns 503. Products can still be created without images.

### Push notifications (VAPID)

Katie receives a browser push notification when a new order is placed. VAPID (Voluntary Application Server Identification) keys authenticate the push server.

Generate a key pair once (Node.js required):

```sh
npx web-push generate-vapid-keys
```

This prints a public/private key pair. Copy them to your settings:

```
VAPID_PUBLIC_KEY=BN...          # the public key (safe to expose — used by the browser)
VAPID_PRIVATE_KEY=...           # the private key (keep secret)
VAPID_SUBJECT=mailto:team@katiesgarden.uk
```

The same public key is served by `GET /api/push/vapid-public-key` and used by the browser to subscribe. The same private key signs the push payload sent to the browser's push service.

> If `VAPID_PUBLIC_KEY` is not set, the bell icon is hidden in the admin panel and no pushes are sent.

### Admin login (OAuth)

The `/admin` route is protected by Azure Static Web Apps' built-in authentication. Katie signs in with GitHub, Google, or Microsoft — whichever she prefers — and SWA checks her email against the `allowedRoles` list in `staticwebapp.config.json`.

**One-time setup per OAuth provider:**

**GitHub:**
1. Go to [github.com/settings/applications/new](https://github.com/settings/applications/new)
2. Homepage URL: `https://www.katiesgarden.uk`
3. Callback URL: `https://www.katiesgarden.uk/.auth/login/github/callback`
4. Copy the **Client ID** and generate a **Client Secret**

**Google:**
1. Go to [console.cloud.google.com → Credentials → Create OAuth client ID](https://console.cloud.google.com)
2. Application type: Web application
3. Authorised redirect URI: `https://www.katiesgarden.uk/.auth/login/google/callback`
4. Copy the **Client ID** and **Client Secret**

**Microsoft:**
1. Go to [portal.azure.com → App registrations → New registration](https://portal.azure.com)
2. Redirect URI (Web): `https://www.katiesgarden.uk/.auth/login/aad/callback`
3. Under **Certificates & secrets → New client secret**, generate a secret
4. Copy the **Application (client) ID** and the secret value

Pass these to Terraform via `terraform.tfvars` (see [Provisioning with Terraform](#provisioning-with-terraform)). Terraform sets them as SWA identity provider settings automatically.

To grant Katie the `admin` role in production, invite her in the **Azure Portal → your Static Web App → Role management → Invite** — choose her provider and email, assign the role `admin`, and send her the invite link. Once she accepts and signs in, `/admin` unlocks and the **Admin** link appears in the nav. (The `allowedRoles: ["admin"]` entry in `staticwebapp.config.json` only declares *which* role is required — it doesn't assign it.)

> There's deliberately **no public "Sign in" link** on the site — admins reach the panel by navigating to `/admin` (which redirects to `/admin/login`). The nav's Admin link only appears once you're already signed in as an admin.

#### Testing admin login locally

The `/.auth/*` endpoints, `/.auth/me`, and roles are provided by the Azure Static Web Apps runtime — they **don't exist** under a plain Aspire / `dotnet run`, so admin login can't be exercised that way. The **SWA CLI** emulates them locally and lets you fake a login with any roles, including `admin`.

**Install once:**

```sh
npm install -g @azure/static-web-apps-cli
# (Azure Functions Core Tools v4 must also be installed — see Prerequisites)
```

**Run it** (from the repo root):

```powershell
# Windows — one command starts the API, the Blazor dev server, and the emulator:
pwsh scripts/start-local-auth.ps1
```

Or manually, in two terminals (any OS):

```sh
# Terminal 1 — the Functions API on :7071
cd Api && func start

# Terminal 2 — the SWA emulator on :4280 (also launches the Blazor dev server)
swa start
```

Both read `swa-cli.config.json`, which wires the Blazor dev server (`:5000`), the API (`:7071`), and your real `staticwebapp.config.json` together behind `http://localhost:4280`.

**Sign in as admin:**

1. Open **http://localhost:4280** and go to **`/admin/login`**.
2. Click any provider button — the SWA CLI shows a local fake-login form instead of the real OAuth screen.
3. In the **Roles** field, add `admin` (alongside the default `anonymous,authenticated`), pick any username, and submit.
4. You're redirected to `/admin` as an administrator: the route guard passes, the API sees the `x-ms-client-principal` header with the admin role, and the **Admin** link appears in the nav.

> The admin pages call `/api/admin/*`, which needs the database. The emulator only handles auth — for working data, point `DATABASE_URL` in `Api/local.settings.json` at a local Postgres (e.g. `docker run -e POSTGRES_PASSWORD=dev -p 5432:5432 postgres`) or a Neon dev branch. Without it, sign-in still works but data calls will error.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## 🩺 Operations

### Live readiness check

Public endpoint at `https://katiesgarden.uk/api/diagnostics`. Returns JSON with the status of each external dependency:

```sh
curl https://katiesgarden.uk/api/diagnostics
```

```json
{
  "status": "ready",
  "checks": {
    "api": "ok",
    "database": "ok",
    "brevo_api": "ok",
    "stripe": "ok",
    "blob_storage": "ok",
    "smtp": "skipped"
  },
  "timestamp": "2026-05-25T18:00:00Z"
}
```

This same endpoint is the Aspire `api` health probe, so `dotnet run --project AppHost/...` surfaces the identical integration status on the dashboard.

- HTTP **200** when everything is green; HTTP **503** when any check is `"fail"`. Each check is `"ok"`, `"fail"`, or `"not_configured"` (an absent/placeholder integration — not a failure)
- **Every check is read-only and quota-safe**: `database` runs `SELECT 1`; `brevo_api` reads the account; `stripe` retrieves the balance (no charge or Checkout Session created); `blob_storage` reads account properties (no container/blob writes). None of these spend any quota or money
- `smtp` is skipped by default — a full STARTTLS + AUTH round-trip is 1–3 s, too slow for per-minute polling. Append **`?checkSmtp=true`** to verify SMTP credentials are live: it connects + authenticates + disconnects and **never sends a message**, so it costs no send quota. The daily `verify-secrets` workflow also covers SMTP
- Rate limited at the Cloudflare edge to 10 requests per minute per IP

Point an uptime monitor (UptimeRobot, BetterStack, etc.) at this URL and alert on non-200 responses.

### Logs

Logs are emitted via `ILogger` throughout the API. In production they are collected by **Azure Application Insights** when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set. To enable:

1. In the [Azure Portal](https://portal.azure.com), find your Static Web App's linked Functions environment
2. Go to **Settings → Configuration** and add `APPLICATIONINSIGHTS_CONNECTION_STRING`
3. View logs at **Application Insights → Logs** with KQL, or **Live Metrics** for real-time tailing

Log levels in use:

| Level | When |
|---|---|
| `Information` | Successful operations, validation failures, idempotent hits |
| `Warning` | Malformed request bodies, Brevo non-2xx responses, missing DB context |
| `Error` | SMTP send failures, DB persistence failures, startup DB init failures |
| `Debug` | Skipped Brevo sync (quiet in production) |

Example KQL queries:

```kql
// Recent contact form failures
traces | where customDimensions["CategoryName"] startswith "ContactFormFunction"
       | where severityLevel >= 3
       | order by timestamp desc

// New orders in the last 24 h
traces | where message contains "Order created"
       | where timestamp > ago(24h)
       | order by timestamp desc
```

If Application Insights isn't connected, logs go to the Functions runtime stdout — visible via `az functionapp logs tail` or the SWA portal's **Logs** tab.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## 🏗 Infrastructure

Azure resources are managed with Terraform in the `infra/` directory. The config provisions:
- **Azure Static Web App (Standard tier)** — hosts the Blazor frontend and Functions API
- **Azure Storage Account** — stores product images uploaded via the admin panel
- **Cloudflare DNS + WAF** — CDN, rate limiting, HTTPS enforcement

### Provisioning with Terraform

**Prerequisites:** [Terraform ≥ 1.7](https://developer.hashicorp.com/terraform/install) and the [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli).

Create `infra/terraform.tfvars` (gitignored — **never commit this file**):

```hcl
subscription_id   = "your-azure-subscription-id"

# Email
smtp_host         = "smtp-relay.brevo.com"
smtp_port         = "587"
smtp_username     = "your-brevo-login-email@example.com"
smtp_password     = "your-brevo-smtp-key"
smtp_sender_email = "noreply@katiesgarden.uk"
recipient_email   = "team@katiesgarden.uk"

# Newsletter
brevo_api_key     = "your-brevo-rest-api-key"
brevo_list_id     = "1"

# Database
database_url      = "postgresql://user:password@host.neon.tech/katiesgarden?sslmode=require"

# Shop (Stripe)
stripe_secret_key      = "sk_live_..."
stripe_webhook_secret  = "whsec_..."
site_url               = "https://www.katiesgarden.uk"

# Push notifications
vapid_public_key   = "BN..."
vapid_private_key  = "..."
vapid_subject      = "mailto:team@katiesgarden.uk"

# Admin OAuth — add whichever providers you want to support
github_client_id      = ""
github_client_secret  = ""
google_client_id      = ""
google_client_secret  = ""
microsoft_client_id   = ""
microsoft_client_secret = ""

# Cloudflare
cloudflare_api_token = "your-cloudflare-api-token"
cloudflare_zone_id   = "your-zone-id"
```

Then run:

```sh
az login
az account show   # confirm the correct subscription is active

cd infra
terraform init
terraform validate
terraform plan
terraform apply
```

If the Static Web App **already exists** in Azure, import it before applying:

```sh
terraform import azurerm_resource_group.main \
  /subscriptions/<sub-id>/resourceGroups/katiesgarden-rg

terraform import azurerm_static_web_app.main \
  /subscriptions/<sub-id>/resourceGroups/katiesgarden-rg/providers/Microsoft.Web/staticSites/katiesgarden
```

### GitHub Actions Secrets

The deploy pipeline runs a `verify-secrets` job before every deploy. Set the following under **GitHub → Settings → Secrets and variables → Actions**:

| Secret | Used for |
|---|---|
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | SWA deploy token |
| `CLOUDFLARE_API_TOKEN` | Cloudflare DNS/WAF management |
| `DATABASE_URL` | Neon Postgres connection string |
| `BREVO_API_KEY` | Newsletter list management |
| `SMTP_HOST` `SMTP_PORT` `SMTP_USERNAME` `SMTP_PASSWORD` | Contact form email sending |
| `STRIPE_SECRET_KEY` | Stripe API (verify-secrets checks it's non-empty) |
| `STRIPE_WEBHOOK_SECRET` | Stripe webhook signature verification |
| `VAPID_PUBLIC_KEY` `VAPID_PRIVATE_KEY` `VAPID_SUBJECT` | Web push notifications |
| `AZURE_STORAGE_CONNECTION_STRING` | Product image storage |

Retrieve the SWA deploy token from Terraform:

```sh
cd infra && terraform output -raw deployment_token
```

> These secrets are also set as SWA application settings by Terraform — that's how the deployed Functions read them at runtime. Keep them in sync. A scheduled run of the verify workflow (daily at 06:00 UTC) catches drift (e.g. a rotated SMTP password you only updated in one place).

### Database (Neon PostgreSQL)

1. Sign up at [neon.tech](https://neon.tech) — no credit card required
2. Create a project, then copy the **connection string** from the dashboard
3. Paste into `database_url` in `terraform.tfvars` and `DATABASE_URL` in `local.settings.json`

All tables are created or updated automatically on cold start — no manual migrations needed.

### Cloudflare

Cloudflare provides CDN, DDoS protection, rate limiting, and HTTPS enforcement in front of the Azure SWA.

#### Setup

1. Sign up at [cloudflare.com](https://www.cloudflare.com) — free tier covers everything here
2. Add `katiesgarden.uk` and point your domain registrar's nameservers to Cloudflare's
3. Copy the **Zone ID** from the domain's **Overview** tab in the dashboard → `cloudflare_zone_id`
4. Go to **My Profile → API Tokens → Create Token** with these zone permissions:
   - **Zone → DNS → Edit**
   - **Zone → Zone Settings → Edit**
   - **Zone → Zone WAF → Edit**
5. Copy the token → `cloudflare_api_token`

After `terraform apply`, Terraform creates DNS records, applies security settings, and configures rate limits on all API routes.

#### Custom domain on Azure SWA

This is a **one-time manual step** (not managed by Terraform for the free tier):

1. In the [Azure Portal](https://portal.azure.com), open your Static Web App → **Custom domains → Add**
2. Enter `katiesgarden.uk` — Azure gives you a TXT record for ownership verification
3. Add the TXT record in Cloudflare's DNS dashboard, then click **Validate** in Azure
4. Repeat for `www.katiesgarden.uk` using a CNAME validation record

Set Cloudflare SSL mode to **Full** (Terraform does this) — not "Flexible".

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## 📈 Development Roadmap

- [x] Responsive home page
- [x] Gallery with project showcase (woodwork, bugs & hugs, maintenance)
- [x] Contact page with server-side validated form
- [x] Newsletter sign-up synced to Brevo
- [x] CI/CD pipeline with PR previews and E2E tests
- [x] DNS, Cloudflare CDN, and Azure Static Web Apps deployment
- [x] Online shop — product catalogue, collections, shopping basket
- [x] Stripe Checkout integration (Click & Collect + local delivery)
- [x] Admin panel — orders, products, collections, delivery settings
- [x] Push notifications to Katie on new orders
- [x] Progressive Web App (service worker, offline shell, installable)
- [ ] Seasonal specials and promotions
- [ ] Expand gallery with new projects

See the [open issues](https://github.com/samBiggs32/KatiesGarden/issues) for proposed features and known issues.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## 🤝 Contributing

1. Fork the project
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a pull request

### Development standards

- Follow the existing code style and patterns
- Write unit tests for new features; integration tests for new API endpoints
- Ensure `dotnet test Tests/KatiesGarden.Tests/ --filter "Category!=Integration"` passes before opening a PR
- Update this README when adding new configuration variables or setup steps

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## 📝 License

Distributed under the MIT License. See `LICENSE.txt` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## 📬 Contact

Sam Biggs - [LinkedIn](https://www.linkedin.com/in/sambiggs32/)

Project Link: [https://github.com/samBiggs32/KatiesGarden](https://github.com/samBiggs32/KatiesGarden)

<p align="right">(<a href="#readme-top">back to top</a>)</p>
