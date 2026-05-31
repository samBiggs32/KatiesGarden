resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location
}

resource "azurerm_static_web_app" "main" {
  name                = var.app_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku_tier            = "Standard"
  sku_size            = "Standard"

  # System-assigned identity required for Key Vault @Microsoft.KeyVault(...) references
  identity {
    type = "SystemAssigned"
  }

  app_settings = {
    SMTP_HOST       = var.smtp_host
    SMTP_PORT       = var.smtp_port
    SMTP_USERNAME   = var.smtp_username
    SENDER_EMAIL    = var.smtp_sender_email
    RECIPIENT_EMAIL = var.recipient_email
    BREVO_LIST_ID   = var.brevo_list_id
    VAPID_PUBLIC_KEY = var.vapid_public_key
    VAPID_SUBJECT    = var.vapid_subject
    SITE_URL         = var.site_url
    AZURE_STORAGE_CONTAINER = "product-images"

    # OAuth client IDs are not secret (public identifiers)
    GITHUB_CLIENT_ID    = var.github_client_id
    GOOGLE_CLIENT_ID    = var.google_client_id
    MICROSOFT_CLIENT_ID = var.microsoft_client_id

    # Secrets — resolved at runtime from Key Vault; never stored as plaintext.
    # The SWA managed identity (above) must have Key Vault Secrets User on the vault.
    DATABASE_URL                    = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.database_url.versionless_id})"
    DATABASE_URL_MIGRATE            = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.database_url_migrate.versionless_id})"
    SMTP_PASSWORD                   = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.smtp_password.versionless_id})"
    STRIPE_SECRET_KEY               = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.stripe_secret_key.versionless_id})"
    STRIPE_WEBHOOK_SECRET           = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.stripe_webhook_secret.versionless_id})"
    BREVO_API_KEY                   = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.brevo_api_key.versionless_id})"
    VAPID_PRIVATE_KEY               = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.vapid_private_key.versionless_id})"
    AZURE_STORAGE_CONNECTION_STRING = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.azure_storage_connection_string.versionless_id})"
    GITHUB_CLIENT_SECRET            = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.github_client_secret.versionless_id})"
    GOOGLE_CLIENT_SECRET            = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.google_client_secret.versionless_id})"
    MICROSOFT_CLIENT_SECRET         = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.microsoft_client_secret.versionless_id})"
  }
}

# Storage account for product/advertising images
resource "azurerm_storage_account" "store" {
  name                     = "${replace(var.app_name, "-", "")}store"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  blob_properties {
    cors_rule {
      allowed_headers    = ["*"]
      allowed_methods    = ["GET", "HEAD"]
      allowed_origins    = ["https://www.katiesgarden.uk", "https://katiesgarden.uk"]
      exposed_headers    = ["*"]
      max_age_in_seconds = 3600
    }
  }
}

resource "azurerm_storage_container" "product_images" {
  name                  = "product-images"
  storage_account_id    = azurerm_storage_account.store.id
  container_access_type = "blob"
}
