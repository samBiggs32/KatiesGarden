resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location
}

resource "azurerm_static_web_app" "main" {
  name                = var.app_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku_tier            = "Free"
  sku_size            = "Free"

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
  }
}
