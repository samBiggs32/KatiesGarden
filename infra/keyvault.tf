# ---------------------------------------------------------------------------
# Azure Key Vault — stores secrets referenced by the Static Web App via
# @Microsoft.KeyVault(...) syntax in app_settings (main.tf).
#
# The SWA is given a system-assigned managed identity and the least-privilege
# "Key Vault Secrets User" role (read-only, no management plane access).
#
# Sam applies this before rotating secrets out of plain-text app_settings:
#   see docs/security/runbooks/keyvault-migration.md
# ---------------------------------------------------------------------------

resource "azurerm_key_vault" "main" {
  name                = var.key_vault_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"

  # Protect against accidental deletion — must be explicitly purged
  soft_delete_retention_days = 90
  purge_protection_enabled   = true

  # RBAC model (not vault access policies) — cleaner with Managed Identity
  enable_rbac_authorization = true
}

data "azurerm_client_config" "current" {}

# ---------------------------------------------------------------------------
# Secrets — values come from terraform.tfvars (sensitive = true on all vars)
# ---------------------------------------------------------------------------

resource "azurerm_key_vault_secret" "database_url" {
  name         = "DATABASE-URL"
  value        = var.database_url
  key_vault_id = azurerm_key_vault.main.id
}

resource "azurerm_key_vault_secret" "database_url_migrate" {
  name         = "DATABASE-URL-MIGRATE"
  value        = var.database_url_migrate
  key_vault_id = azurerm_key_vault.main.id
}

resource "azurerm_key_vault_secret" "stripe_secret_key" {
  name         = "STRIPE-SECRET-KEY"
  value        = var.stripe_secret_key
  key_vault_id = azurerm_key_vault.main.id
}

resource "azurerm_key_vault_secret" "stripe_webhook_secret" {
  name         = "STRIPE-WEBHOOK-SECRET"
  value        = var.stripe_webhook_secret
  key_vault_id = azurerm_key_vault.main.id
}

resource "azurerm_key_vault_secret" "smtp_password" {
  name         = "SMTP-PASSWORD"
  value        = var.smtp_password
  key_vault_id = azurerm_key_vault.main.id
}

resource "azurerm_key_vault_secret" "brevo_api_key" {
  name         = "BREVO-API-KEY"
  value        = var.brevo_api_key
  key_vault_id = azurerm_key_vault.main.id
}

resource "azurerm_key_vault_secret" "vapid_private_key" {
  name         = "VAPID-PRIVATE-KEY"
  value        = var.vapid_private_key
  key_vault_id = azurerm_key_vault.main.id
}

resource "azurerm_key_vault_secret" "github_client_secret" {
  name         = "GITHUB-CLIENT-SECRET"
  value        = var.github_client_secret
  key_vault_id = azurerm_key_vault.main.id
}

resource "azurerm_key_vault_secret" "google_client_secret" {
  name         = "GOOGLE-CLIENT-SECRET"
  value        = var.google_client_secret
  key_vault_id = azurerm_key_vault.main.id
}

resource "azurerm_key_vault_secret" "microsoft_client_secret" {
  name         = "MICROSOFT-CLIENT-SECRET"
  value        = var.microsoft_client_secret
  key_vault_id = azurerm_key_vault.main.id
}

resource "azurerm_key_vault_secret" "azure_storage_connection_string" {
  name         = "AZURE-STORAGE-CONNECTION-STRING"
  value        = azurerm_storage_account.store.primary_connection_string
  key_vault_id = azurerm_key_vault.main.id
}

# ---------------------------------------------------------------------------
# System-assigned managed identity for the Static Web App
# ---------------------------------------------------------------------------

resource "azurerm_role_assignment" "swa_kv_secrets_user" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_static_web_app.main.identity[0].principal_id
}
