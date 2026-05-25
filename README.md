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
- [Infrastructure](#infrastructure)
  - [Provisioning with Terraform](#provisioning-with-terraform)
  - [GitHub Actions Secret](#github-actions-secret)
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

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- A code editor (recommended: [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/))

### Installation

1. Clone the repository
   ```sh
   git clone https://github.com/samBiggs32/KatiesGarden.git
   cd KatiesGarden
   ```

2. Run the Blazor frontend
   ```sh
   dotnet run --project Web/KatiesGarden.Web/Client/KatiesGarden.Web.Client.csproj
   ```

3. Open your browser and go to `https://localhost:5001`

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## 🏗 Infrastructure

Azure resources are managed with Terraform. The config lives in the `infra/` directory and provisions a single **Azure Static Web App (Free tier)**, which hosts both the Blazor frontend and the Azure Functions API at no cost.

### Provisioning with Terraform

**Prerequisites:** [Terraform ≥ 1.7](https://developer.hashicorp.com/terraform/install) and the [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli).

Create `infra/terraform.tfvars` (gitignored — never commit this file):

```hcl
subscription_id   = "your-azure-subscription-id"

# SMTP — see "Email sending" below for provider options
smtp_host         = "smtp.brevo.com"
smtp_port         = "587"
smtp_username     = "your-smtp-login"
smtp_password     = "your-smtp-password-or-api-key"
smtp_sender_email = "noreply@katiesgarden.uk"
recipient_email   = "team@katiesgarden.uk"
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

### GitHub Actions Secret

After `terraform apply`, retrieve the deployment token and add it to GitHub:

```sh
terraform output -raw deployment_token
```

Set this value as the `AZURE_STATIC_WEB_APPS_API_TOKEN` secret in **GitHub → Settings → Secrets and variables → Actions**.

### Email sending

The contact form posts to an Azure Function which sends email via SMTP. You need a provider and credentials — nothing is sent without them.

**Recommended: Brevo (free tier, 300 emails/day)**

1. Sign up at [brevo.com](https://www.brevo.com) — no credit card required
2. Go to **Senders & IPs → Domains** and add `katiesgarden.uk`. Follow the DNS verification steps (adds a few TXT/CNAME records to your DNS). This lets you send from any `@katiesgarden.uk` address.
3. Go to **SMTP & API → SMTP** and generate an SMTP key.
4. Your `terraform.tfvars` SMTP entries should be:

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