output "static_web_app_url" {
  description = "Default hostname of the deployed site"
  value       = "https://${azurerm_static_web_app.main.default_host_name}"
}

output "deployment_token" {
  description = "Set this as the AZURE_STATIC_WEB_APPS_API_TOKEN secret in GitHub Actions"
  value       = azurerm_static_web_app.main.api_key
  sensitive   = true
}
