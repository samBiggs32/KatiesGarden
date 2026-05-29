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

  app_settings = {
    SMTP_HOST       = var.smtp_host
    SMTP_PORT       = var.smtp_port
    SMTP_USERNAME   = var.smtp_username
    SMTP_PASSWORD   = var.smtp_password
    SENDER_EMAIL    = var.smtp_sender_email
    RECIPIENT_EMAIL = var.recipient_email
    DATABASE_URL    = var.database_url
    BREVO_API_KEY   = var.brevo_api_key
    BREVO_LIST_ID   = var.brevo_list_id

    # Store
    STRIPE_SECRET_KEY              = var.stripe_secret_key
    STRIPE_WEBHOOK_SECRET          = var.stripe_webhook_secret
    VAPID_PUBLIC_KEY               = var.vapid_public_key
    VAPID_PRIVATE_KEY              = var.vapid_private_key
    VAPID_SUBJECT                  = var.vapid_subject
    SITE_URL                       = var.site_url
    AZURE_STORAGE_CONNECTION_STRING = azurerm_storage_account.store.primary_connection_string
    AZURE_STORAGE_CONTAINER        = "product-images"

    # OAuth (SWA reads these by name from app settings for built-in auth)
    GITHUB_CLIENT_ID        = var.github_client_id
    GITHUB_CLIENT_SECRET    = var.github_client_secret
    GOOGLE_CLIENT_ID        = var.google_client_id
    GOOGLE_CLIENT_SECRET    = var.google_client_secret
    MICROSOFT_CLIENT_ID     = var.microsoft_client_id
    MICROSOFT_CLIENT_SECRET = var.microsoft_client_secret
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
