# Runbook: Database Least-Privilege Roles

Create `kg_migrate` (DDL) and `kg_app` (DML-only, append-only audit_logs) roles in Neon PostgreSQL and update the connection strings used by the API.

## Background

Currently the API uses a single superuser credential for all database operations. `infra/sql/roles.sql` separates this into:

- **`kg_migrate`** — schema owner, used only by EF Core `MigrateAsync()` at startup. Connected via `DATABASE_URL_MIGRATE`.
- **`kg_app`** — runtime DML (SELECT/INSERT/UPDATE/DELETE on all tables), but **INSERT-only on `audit_logs`**. The DB-level revoke of UPDATE/DELETE on that table enforces audit immutability — no application code change can bypass it. Connected via `DATABASE_URL`.

## Prerequisites

- Access to the Neon console or a `psql` session with superuser privileges on the `katiesgarden` database
- Neon project ID and branch ID

## Steps

### 1. Apply roles.sql

```bash
psql "$NEON_SUPERUSER_URL" -f infra/sql/roles.sql
```

The script is idempotent (`IF NOT EXISTS`). It will create the roles if absent; running it again is safe.

### 2. Set secure passwords

```sql
ALTER ROLE kg_migrate PASSWORD 'GENERATE_STRONG_PASSWORD';
ALTER ROLE kg_app     PASSWORD 'GENERATE_STRONG_PASSWORD';
```

Use a password manager to generate 32+ character random strings.

### 3. Construct connection strings

```
kg_migrate: postgresql://kg_migrate:<password>@<neon-host>/<db>?sslmode=require
kg_app:     postgresql://kg_app:<password>@<neon-host>/<db>?sslmode=require
```

### 4. Update tfvars

```hcl
database_url         = "postgresql://kg_app:...@.../katiesgarden?sslmode=require"
database_url_migrate = "postgresql://kg_migrate:...@.../katiesgarden?sslmode=require"
```

### 5. Apply Terraform (updates Key Vault secrets)

```bash
cd infra
terraform apply -target=azurerm_key_vault_secret.database_url \
                -target=azurerm_key_vault_secret.database_url_migrate
```

### 6. Validate

Restart the API (redeploy or trigger a configuration refresh) and verify:

```bash
curl https://www.katiesgarden.uk/api/health          # 200
curl https://www.katiesgarden.uk/api/diagnostics | jq .
```

Then confirm audit immutability:

```sql
-- Run as kg_app — should fail with permission denied
UPDATE audit_logs SET actor = 'tampered' WHERE id = (SELECT id FROM audit_logs LIMIT 1);
DELETE FROM audit_logs WHERE id = (SELECT id FROM audit_logs LIMIT 1);
```

Both statements must return `ERROR: permission denied for table audit_logs`.

### 7. Retire the superuser credential

Remove the superuser connection string from all tfvars files and Key Vault secrets. The superuser is only needed for future `roles.sql` changes.

## Rollback

Revert `DATABASE_URL` in Key Vault to the superuser credential. The `kg_app` and `kg_migrate` roles can remain; they do no harm.
