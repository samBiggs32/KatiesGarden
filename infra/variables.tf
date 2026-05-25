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
