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

# ---------------------------------------------------------------------------
# Store / OAuth / Push
# ---------------------------------------------------------------------------

variable "github_client_id" {
  description = "GitHub OAuth App client ID for SWA admin login"
  type        = string
  sensitive   = true
  default     = ""
}

variable "github_client_secret" {
  description = "GitHub OAuth App client secret for SWA admin login"
  type        = string
  sensitive   = true
  default     = ""
}

variable "google_client_id" {
  description = "Google OAuth client ID for SWA admin login"
  type        = string
  sensitive   = true
  default     = ""
}

variable "google_client_secret" {
  description = "Google OAuth client secret for SWA admin login"
  type        = string
  sensitive   = true
  default     = ""
}

variable "microsoft_client_id" {
  description = "Microsoft AAD app client ID for SWA admin login"
  type        = string
  sensitive   = true
  default     = ""
}

variable "microsoft_client_secret" {
  description = "Microsoft AAD app client secret for SWA admin login"
  type        = string
  sensitive   = true
  default     = ""
}

variable "stripe_secret_key" {
  description = "Stripe secret key (sk_live_... or sk_test_...)"
  type        = string
  sensitive   = true
  default     = ""
}

variable "stripe_webhook_secret" {
  description = "Stripe webhook signing secret (whsec_...)"
  type        = string
  sensitive   = true
  default     = ""
}

variable "vapid_public_key" {
  description = "VAPID public key for web push notifications (generate with: npx web-push generate-vapid-keys)"
  type        = string
  default     = ""
}

variable "vapid_private_key" {
  description = "VAPID private key for web push notifications"
  type        = string
  sensitive   = true
  default     = ""
}

variable "vapid_subject" {
  description = "VAPID subject — a mailto: or URL identifying the push sender"
  type        = string
  default     = "mailto:team@katiesgarden.uk"
}

variable "site_url" {
  description = "Public base URL of the site (no trailing slash)"
  type        = string
  default     = "https://www.katiesgarden.uk"
}

# ---------------------------------------------------------------------------
# Cloudflare
# ---------------------------------------------------------------------------

variable "cloudflare_api_token" {
  description = "Cloudflare API token with Zone:DNS:Edit, Zone:Settings:Edit, and Zone:Zone WAF:Edit permissions for the katiesgarden.uk zone."
  type        = string
  sensitive   = true
}

variable "cloudflare_zone_id" {
  description = "Cloudflare Zone ID for katiesgarden.uk — found in the Cloudflare dashboard under the domain's Overview tab."
  type        = string
}
