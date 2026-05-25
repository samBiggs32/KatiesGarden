variable "subscription_id" {
  description = "Azure subscription ID"
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
  default     = "katiesgarden-rg"
}

variable "location" {
  description = "Azure region — must be one that supports Static Web Apps (westeurope, eastus2, etc.)"
  type        = string
  default     = "westeurope"
}

variable "app_name" {
  description = "Name of the Static Web App resource (must be globally unique)"
  type        = string
  default     = "katiesgarden"
}

variable "smtp_host" {
  description = "SMTP server hostname"
  type        = string
  default     = "smtp-relay.brevo.com"
}

variable "smtp_port" {
  description = "SMTP port — 587 for STARTTLS (recommended), 465 for SSL"
  type        = string
  default     = "587"
}

variable "smtp_username" {
  description = "SMTP login username (often your sending email address)"
  type        = string
  sensitive   = true
}

variable "smtp_password" {
  description = "SMTP password or API key"
  type        = string
  sensitive   = true
}

variable "smtp_sender_email" {
  description = "From address shown on outbound emails"
  type        = string
  default     = "noreply@katiesgarden.uk"
}

variable "recipient_email" {
  description = "Email address that receives contact form submissions"
  type        = string
  default     = "team@katiesgarden.uk"
}

variable "database_url" {
  description = "Neon PostgreSQL connection string (postgresql://user:pass@host/db?sslmode=require)"
  type        = string
  sensitive   = true
  default     = ""
}

variable "brevo_api_key" {
  description = "Brevo REST API key — different from the SMTP key. Found under Brevo → SMTP & API → API Keys."
  type        = string
  sensitive   = true
  default     = ""
}

variable "brevo_list_id" {
  description = "Brevo contact list ID to add newsletter subscribers to. Create a list in Brevo → Contacts → Lists, then copy its numeric ID."
  type        = string
  default     = ""
}
