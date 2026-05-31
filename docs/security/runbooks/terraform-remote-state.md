# Runbook: Terraform Remote State

Migrate Terraform state from the local `terraform.tfstate` file to an Azure Storage Account. Local state stores secret outputs (connection strings, keys) on disk — remote state stores them encrypted in Azure Blob Storage with versioning and RBAC.

## Steps

### 1. Create the state storage account (one-time, manual)

These commands are run *before* Terraform manages the storage account (it cannot manage its own state backend).

```bash
# Create resource group and storage account
az group create \
  --name katiesgarden-tfstate-rg \
  --location westeurope

az storage account create \
  --name kgtfstate \
  --resource-group katiesgarden-tfstate-rg \
  --location westeurope \
  --sku Standard_LRS \
  --allow-blob-public-access false \
  --min-tls-version TLS1_2

# Enable versioning (protects against accidental state corruption)
az storage account blob-service-properties update \
  --account-name kgtfstate \
  --resource-group katiesgarden-tfstate-rg \
  --enable-versioning true

# Create the container
az storage container create \
  --name tfstate \
  --account-name kgtfstate
```

### 2. Restrict access to the state account

Grant yourself (or a service principal used in CI) `Storage Blob Data Contributor` on the container:

```bash
az role assignment create \
  --assignee <your-object-id-or-sp> \
  --role "Storage Blob Data Contributor" \
  --scope "/subscriptions/<sub-id>/resourceGroups/katiesgarden-tfstate-rg/storageAccounts/kgtfstate/blobServices/default/containers/tfstate"
```

Disable storage account key access so only RBAC works:

```bash
az storage account update \
  --name kgtfstate \
  --resource-group katiesgarden-tfstate-rg \
  --allow-shared-key-access false
```

### 3. Migrate existing state

The backend block in `infra/providers.tf` is now active. Run:

```bash
cd infra
terraform init -migrate-state
```

Terraform will prompt: `Do you want to copy existing state to the new backend?` — answer `yes`.

### 4. Validate

```bash
terraform state list    # should show all existing resources
terraform plan          # should show no changes
```

### 5. Delete local state files

```bash
rm infra/terraform.tfstate infra/terraform.tfstate.backup 2>/dev/null || true
```

Confirm the files are removed and not tracked by git (`.gitignore` should already exclude `*.tfstate`).

## Rollback

To revert to local state:

1. Comment out the `backend "azurerm"` block in `providers.tf`.
2. Run `terraform init -migrate-state` — Terraform will offer to copy remote state back to local.
