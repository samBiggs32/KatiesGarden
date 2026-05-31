terraform {
  required_version = ">= 1.7"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    cloudflare = {
      source  = "cloudflare/cloudflare"
      version = "~> 4.0"
    }
  }

  # Remote state — encrypted Storage Account, versioning enabled.
  # Run the runbook at docs/security/runbooks/terraform-remote-state.md before
  # the first `terraform init -migrate-state` to create the storage account.
  backend "azurerm" {
    resource_group_name  = "katiesgarden-tfstate-rg"
    storage_account_name = "kgtfstate"
    container_name       = "tfstate"
    key                  = "katiesgarden.tfstate"
  }
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}

provider "cloudflare" {
  api_token = var.cloudflare_api_token
}
