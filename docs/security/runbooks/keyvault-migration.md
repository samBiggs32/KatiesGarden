# Runbook: Key Vault Migration

Migrate all secrets out of plain-text Static Web App settings into Azure Key Vault.
After this runbook the SWA will resolve secrets at runtime via `@Microsoft.KeyVault(...)` references; no secret value is stored in Terraform state or the Azure portal as readable text.

## Prerequisites

- `infra/keyvault.tf` committed and reviewed
- Terraform `~> 4.0` azurerm provider
- `az login` with Owner or Contributor + User Access Administrator on the subscription
- Current `terraform.tfvars` with all secret values populated

## Steps

### 1. Create the tfstate storage account (if not already done)

Run the terraform-remote-state runbook first. The Key Vault plan references the storage account for state locking.

### 2. Plan

```bash
cd infra
terraform init          # pulls azurerm 4.x provider
terraform plan -out=kv.plan
```

Review the plan. Expect:
- `azurerm_key_vault.main` — create
- `azurerm_key_vault_secret.*` — create (one per secret)
- `azurerm_static_web_app.main` — update (identity block + app_settings changes)
- `azurerm_role_assignment.swa_kv_secrets_user` — create

No resources should be destroyed. If any secret or the SWA is scheduled for destruction, stop and investigate.

### 3. Apply

```bash
terraform apply kv.plan
```

### 4. Validate identity is set

```bash
az staticwebapp identity show --name katiesgarden --resource-group katiesgarden-rg
```

Expect `"type": "SystemAssigned"` and a non-empty `principalId`.

### 5. Validate Key Vault references resolve

In the Azure portal: Static Web Apps → katiesgarden → Configuration → Application settings.
Each secret entry should show a green tick icon (resolved) next to the `@Microsoft.KeyVault(...)` reference. A red icon means the identity role assignment hasn't propagated yet — wait 2 minutes and refresh.

### 6. Smoke-test the live API

```bash
curl https://www.katiesgarden.uk/api/health
curl https://www.katiesgarden.uk/api/diagnostics | jq .status
```

Both should return 200/`"ready"`. If `diagnostics` reports a service as unhealthy, check the Application Insights live stream for startup errors.

### 7. Remove plaintext secrets from tfvars

Once the smoke test passes, remove the raw secret values from `terraform.tfvars` (the Key Vault secrets are the source of truth now). Keep the `key_vault_name` and non-sensitive vars.

## Rollback

If the Key Vault references fail to resolve (e.g. the SWA identity wasn't provisioned before apply):

1. In `main.tf`, revert the affected `app_settings` entries back to the raw `var.*` references temporarily.
2. `terraform apply` to restore plain-text settings.
3. Investigate the identity/RBAC issue, fix, and re-apply.

The Key Vault and its secrets are safe to leave in place during a rollback; they are only read, not required, for the SWA to function with plain-text settings.
