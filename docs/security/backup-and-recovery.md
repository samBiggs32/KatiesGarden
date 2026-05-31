# Backup and Recovery — Katie's Garden

**Version:** 1.0 | **Date:** 2026-05-31

---

## Database (Neon PostgreSQL)

**Mechanism:** Neon provides Point-in-Time Recovery (PITR) on all plans. The free plan retains 7 days of history; paid plans extend this.

**RPO:** ~5 minutes (Neon's WAL-based continuous archiving).  
**RTO:** ~10 minutes to restore a branch and reconnect the API.

### Recovery procedure

1. Log into the Neon console.
2. Navigate to the `katiesgarden` project → Branches.
3. Click **Restore** on the main branch and select a point in time before the incident.
4. Neon creates a new branch at that point. Verify data looks correct.
5. If correct, update `DATABASE_URL` in Key Vault to the restored branch's connection string.
6. Redeploy (or trigger a configuration refresh) to reconnect the API.

### What is backed up

- All tables: orders, order_lines, order_status_history, products, collections, push_subscriptions, audit_logs, stripe_processed_events.
- Schema (Neon archives WAL, not just data snapshots).

### What is NOT backed up by Neon

- Azure Blob Storage product images (see below).

---

## Blob Storage (Product Images)

**Mechanism:** Azure Storage Account `LRS` replication (3 copies within one data centre). For disaster recovery beyond hardware failure, enable GRS (Geo-Redundant Storage) in the Terraform config.

**Current risk:** Single-region only. A regional Azure outage would make images unavailable until failover.

**Recommendation:** Upgrade to `Standard_GRS` in `infra/main.tf` for the storage account if business continuity is a concern. Images can also be re-uploaded from local copies.

### Recovery procedure

If product images are lost:
1. Re-upload via the admin panel (Admin → Products → Edit → Upload image).
2. If the source files are available locally, they can be batch-uploaded using the Azure CLI:
   ```bash
   az storage blob upload-batch \
     --account-name katiesgardenstore \
     --destination product-images \
     --source ./local-images/
   ```

---

## Application Code and Configuration

**Mechanism:** Git (GitHub). All code is committed; all infrastructure is in Terraform.

**Recovery:** Re-run the production CI/CD pipeline from the last known-good commit. The deployment is fully automated — no manual steps.

---

## Recovery Time Objectives

| Component | RPO | RTO | Notes |
|-----------|-----|-----|-------|
| Database | ~5 min | ~10 min | Neon PITR |
| Images | N/A (no change tracking) | Hours (manual re-upload) | Local copies advised |
| Application | Commit time | ~15 min | CI/CD pipeline |
| Secrets (Key Vault) | Not applicable (never changed in disaster) | ~5 min | RBAC re-grant if needed |
