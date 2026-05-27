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
  - [Installation](#installation)
- [Operations](#-operations)
  - [Live readiness check](#live-readiness-check)
  - [Logs](#logs)
- [Infrastructure](#infrastructure)
  - [Provisioning with Terraform](#provisioning-with-terraform)
  - [GitHub Actions Secret](#github-actions-secret)
  - [Cloudflare](#cloudflare)
- [Development Roadmap](#development-roadmap)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)

## 🌱 About The Project

Katie's Garden is a family-run garden center and landscaping service based in Milverton, Taunton, UK. This web application showcases our products, services, and finished garden projects while providing an online shopping experience for customers.

Our site features a responsive design that works beautifully on both desktop and mobile devices, allowing customers to browse our plant selection, view our garden design gallery, and place orders online.

### Built With

* [Blazor WebAssembly (.NET 10)](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) - Frontend
* [Azure Functions v4 (.NET 9)](https://learn.microsoft.com/en-us/azure/azure-functions/) - Backend API
* [MudBlazor](https://mudblazor.com) - UI component library
* [MailKit](https://github.com/jstedfast/MailKit) - SMTP email sending
* [Azure Static Web Apps](https://azure.microsoft.com/en-us/services/app-service/static/) - Hosting (Free tier)
* [Cloudflare](https://www.cloudflare.com) - CDN, DDoS protection, and rate limiting (free tier)
* [Terraform](https://www.terraform.io) - Infrastructure as code

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## ✨ Features

- **Home Page** - Featuring seasonal highlights and business introduction
- **Gallery** - Showcase of completed garden projects and available plants
- **Contact Page** - Business information and inquiry form
- **Shop** (Coming Soon) - Online ordering for:
  - Indoor Plants
  - Perennials
  - Vegetables
  - Herbs
  - Woodwork Products (Bug Houses, Boot Stands, etc.)

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## 🚀 Getting Started

Follow these instructions to get a copy of the project up and running on your local machine for development and testing purposes.

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (also pulls in the .NET 9 runtime needed by the Functions API)
- [Docker](https://docs.docker.com/get-docker/) — required for the Aspire local stack (Postgres) and for running integration tests
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local) — Aspire launches the Functions host via `func`
- Git
- A code editor (recommended: [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/))

### Installation

1. Clone the repository
   ```sh
   git clone https://github.com/samBiggs32/KatiesGarden.git
   cd KatiesGarden
   ```

2. Run the full local stack via Aspire — spins up Postgres, the Functions API, and the Blazor client with a dashboard at the URL printed in the console
   ```sh
   dotnet run --project AppHost/KatiesGarden.AppHost.csproj
   ```

   Or run just the frontend against a remote / pre-existing API
   ```sh
   dotnet run --project Web/KatiesGarden.Web/Client/KatiesGarden.Web.Client.csproj
   ```

### Running the tests

Unit tests (fast, no Docker required):
```sh
dotnet test Tests/KatiesGarden.Tests/ --filter "Category!=Integration"
```

Integration tests (uses Testcontainers; needs Docker running):
```sh
dotnet test Tests/KatiesGarden.Tests/ --filter "Category=Integration"
```

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## 🩺 Operations

### Live readiness check

Public endpoint at `https://katiesgarden.uk/api/diagnostics`. Returns JSON with the status of each external dependency the API touches at runtime:

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
    "smtp": "skipped"
  },
  "timestamp": "2026-05-25T18:00:00Z"
}
```

- HTTP **200** when everything green; HTTP **503** when any check is `"fail"`
- Individual values: `"ok"` (reachable), `"fail"` (unreachable), `"not_configured"` (env var not set — endpoint degrades gracefully), `"skipped"` (intentionally not run on each call)
- `smtp` is deliberately skipped on every call — a full STARTTLS + AUTH LOGIN round-trip is 1–3s, too slow for per-minute uptime polling. The daily `verify-secrets` GitHub Action covers SMTP end-to-end
- Rate limited at the Cloudflare edge to 10 requests per minute per IP, so uptime monitors are fine but the Brevo API quota is protected from abuse

Point an uptime monitor (UptimeRobot, BetterStack, etc.) at this URL and alert on non-200 responses for a low-effort production health signal.

### Logs

Logs are emitted via `ILogger` throughout the API. In production they're collected by **Azure Application Insights** when the SWA-linked Functions runtime has `APPLICATIONINSIGHTS_CONNECTION_STRING` set. To enable:

1. In the [Azure Portal](https://portal.azure.com), find your Static Web App's linked Functions app (or the SWA-managed Functions environment)
2. Go to **Settings → Configuration** and add `APPLICATIONINSIGHTS_CONNECTION_STRING` pointing at an Application Insights resource (free 5 GB/month tier is plenty)
3. View logs at **Application Insights → Logs** with KQL, or **Live Metrics** for real-time tailing

Log levels in use:

| Level | When |
|---|---|
| `Information` | Successful operations, validation failures, "already-subscribed" idempotent hits |
| `Warning` | Malformed request bodies, Brevo non-2xx responses, missing DB context, diagnostics check failures |
| `Error` | SMTP send failures, DB persistence failures, host startup DB init failures |
| `Debug` | Skipped Brevo sync (expected in dev; quiet in production) |

Logs are structured: every entry uses templated parameters (`{Email}`, `{StatusCode}` etc.) so they're queryable in Application Insights via KQL. Example queries:

```kql
// Recent contact form failures
traces | where customDimensions["CategoryName"] startswith "ContactFormFunction"
       | where severityLevel >= 3
       | order by timestamp desc

// Validation failure patterns
traces | where message contains "validation failed"
       | summarize count() by tostring(customDimensions["Errors"])
       | order by count_ desc
```

If Application Insights isn't connected, logs still go to the Functions runtime stdout — visible via `az functionapp logs tail` or the SWA portal's **Logs** tab.

## 🏗 Infrastructure

Azure resources are managed with Terraform. The config lives in the `infra/` directory and provisions a single **Azure Static Web App (Free tier)**, which hosts both the Blazor frontend and the Azure Functions API at no cost. Subscriber data is stored in a **Neon serverless PostgreSQL** database (free tier).

### Provisioning with Terraform

**Prerequisites:** [Terraform ≥ 1.7](https://developer.hashicorp.com/terraform/install) and the [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli).

Create `infra/terraform.tfvars` (gitignored — never commit this file):

```hcl
subscription_id   = "your-azure-subscription-id"

# SMTP (Brevo) — see "Email sending" below
smtp_host         = "smtp-relay.brevo.com"
smtp_port         = "587"
smtp_username     = "your-brevo-login-email@example.com"
smtp_password     = "your-brevo-smtp-key"
smtp_sender_email = "noreply@katiesgarden.uk"
recipient_email   = "team@katiesgarden.uk"

# Neon PostgreSQL — see "Database" below
database_url      = "postgresql://user:password@host.neon.tech/katiesgarden?sslmode=require"

# Brevo newsletter list — see "Email sending" below
brevo_api_key     = "your-brevo-rest-api-key"
brevo_list_id     = "1"

# Cloudflare — see "Cloudflare" below
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

The deploy pipeline is gated by a `verify-secrets` job that checks every external dependency is reachable before letting a deploy proceed. Set the following under **GitHub → Settings → Secrets and variables → Actions**:

| Secret | Used for | How verified |
|---|---|---|
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | SWA deploy | Presence + length check (full check at deploy) |
| `CLOUDFLARE_API_TOKEN` | Cloudflare DNS/WAF management | `GET /user/tokens/verify` returns `status: active` |
| `BREVO_API_KEY` | Newsletter contact-list management | `GET /v3/account` returns 200 |
| `SMTP_HOST` `SMTP_PORT` `SMTP_USERNAME` `SMTP_PASSWORD` | Contact form email sending | Live SMTP STARTTLS handshake + AUTH LOGIN |
| `DATABASE_URL` | Subscriber persistence (Neon Postgres) | `psql ... -c "SELECT 1"` (optional — warns rather than fails if unset) |

Retrieve the SWA token from Terraform:

```sh
cd infra
terraform output -raw deployment_token
```

These secrets are duplicated between Azure (set by Terraform as SWA app settings — that's how the deployed Functions read them at runtime) and GitHub (read by the verify-secrets workflow during CI). Keep them in sync. A scheduled run of the verify workflow (daily at 06:00 UTC) catches drift — for example, a rotated SMTP password that you only updated in Azure.

### Database (Neon PostgreSQL)

Newsletter subscribers are stored in a free-tier [Neon](https://neon.tech) serverless PostgreSQL database. Neon scales to zero when idle — ideal for a low-traffic site.

1. Sign up at [neon.tech](https://neon.tech) — no credit card required
2. Create a project, then copy the **connection string** from the dashboard (looks like `postgresql://user:pass@host.neon.tech/dbname?sslmode=require`)
3. Paste it into `database_url` in `terraform.tfvars`

The `subscribers` table is created automatically on first deploy via `EnsureCreated()`. No manual migrations needed to get started.

> If `DATABASE_URL` is not set, the subscribe endpoint degrades gracefully — it still adds contacts to Brevo, it just won't store them locally.

### Email sending

The contact form posts to an Azure Function which sends email via SMTP. You need a provider and credentials — nothing is sent without them.

**Recommended: Brevo (free tier, 300 emails/day)**

1. Sign up at [brevo.com](https://www.brevo.com) — no credit card required
2. Go to **Senders & IPs → Domains** and add `katiesgarden.uk`. Follow the DNS verification steps (a few TXT/CNAME records). This lets you send from any `@katiesgarden.uk` address.
3. Go to **SMTP & API → SMTP** and generate an **SMTP key** (for sending contact form emails).
4. Go to **SMTP & API → API Keys** and generate a separate **API key** (for adding newsletter subscribers to a contact list). Copy this to `brevo_api_key`.
5. Go to **Contacts → Lists**, create a list (e.g. "Website Newsletter"), and copy its numeric ID to `brevo_list_id`.
6. Your `terraform.tfvars` SMTP entries should be:

```hcl
smtp_host         = "smtp-relay.brevo.com"
smtp_port         = "587"
smtp_username     = "your-brevo-login-email@example.com"  # your Brevo account email
smtp_password     = "xsmtpsib-..."                        # the generated SMTP key, not your login password
smtp_sender_email = "noreply@katiesgarden.uk"
recipient_email   = "team@katiesgarden.uk"
```

**Alternative: your existing email host**
If `team@katiesgarden.uk` is hosted via Google Workspace, Microsoft 365, or a domain host (e.g. Krystal), you can use their outbound SMTP credentials instead. Check your host's SMTP settings page.

**Alternative: SendGrid (free tier, 100 emails/day)**
Use `smtp.sendgrid.net`, port `587`, username `apikey`, and a SendGrid API key with Mail Send permission as the password.

**Local development**
Copy `Api/local.settings.json.example` to `Api/local.settings.json` (gitignored) and fill in your credentials to test the contact form locally with the Azure Functions emulator.

### Cloudflare

Cloudflare sits in front of the Azure Static Web App and provides:

- **CDN** — static assets cached at Cloudflare's edge, reducing latency globally
- **DDoS protection** — automatic, always-on at the free tier
- **Rate limiting** — blocks IPs that submit more than 5 requests per minute to `/api/contact` or `/api/subscribe`, preventing spam and abuse
- **SSL termination** — Cloudflare manages the public HTTPS certificate; traffic to Azure SWA goes over HTTPS (full mode)
- **HTTP → HTTPS redirect** — enforced at the Cloudflare edge

#### Setup

1. Sign up at [cloudflare.com](https://www.cloudflare.com) — the free tier covers everything here
2. Add your domain (`katiesgarden.uk`) and follow the prompts to update your domain registrar's nameservers to Cloudflare's
3. Once active, find your **Zone ID** in the dashboard under the domain's **Overview** tab (right-hand panel) — copy it to `cloudflare_zone_id` in `terraform.tfvars`
4. Go to **My Profile → API Tokens → Create Token** and create a custom token with these permissions on the `katiesgarden.uk` zone:
   - **Zone → DNS → Edit**
   - **Zone → Zone Settings → Edit**
   - **Zone → Zone WAF → Edit**
5. Copy the token to `cloudflare_api_token` in `terraform.tfvars`

After running `terraform apply`, Terraform will:
- Create CNAME records pointing `katiesgarden.uk` and `www.katiesgarden.uk` to your Azure SWA hostname
- Apply zone security settings (TLS 1.2+, HTTP→HTTPS redirect, full SSL, medium security level)
- Configure rate limiting on the API endpoints

#### Custom domain on Azure SWA

Connecting your custom domain in Azure SWA is a **one-time manual step** (not managed by Terraform for the free tier):

1. In the [Azure Portal](https://portal.azure.com), open your Static Web App
2. Go to **Custom domains → Add**
3. Enter `katiesgarden.uk` — Azure will give you a TXT record to verify ownership
4. Add that TXT record in Cloudflare's DNS dashboard, then click **Validate** in Azure
5. Repeat for `www.katiesgarden.uk` using a CNAME validation record

Azure SWA generates its own TLS certificate for the custom domain. Because the DNS CNAMEs point to Azure via Cloudflare's proxy, set SSL mode to **Full** (already done by Terraform) — not "Flexible".

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## 📈 Development Roadmap

- [x] Implement responsive home page
- [x] Create gallery with project showcase
- [x] Develop contact page with form
- [x] Set up CI/CD pipeline 
- [x] Configure DNS and deploy to Azure Static Web Apps
- [ ] Implement shop/ordering functionality
  - [ ] Product catalog with categories
  - [ ] Shopping cart functionality
  - [ ] Checkout process
  - [ ] Order management
- [ ] Add user accounts and authentication
- [ ] Expand gallery with new projects
- [ ] Implement seasonal specials and promotions

See the [open issues](https://github.com/samBiggs32/KatiesGarden/issues) for a list of proposed features and known issues.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## 🤝 Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Development Standards

- Follow the existing code style and patterns
- Write unit tests for new features
- Update documentation as needed
- Ensure all tests pass before submitting a pull request

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## 📝 License

Distributed under the MIT License. See `LICENSE.txt` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## 📬 Contact

Sam Biggs - [LinkedIn](https://www.linkedin.com/in/sambiggs32/)

Project Link: [https://github.com/samBiggs32/KatiesGarden](https://github.com/samBiggs32/KatiesGarden)

<p align="right">(<a href="#readme-top">back to top</a>)</p>