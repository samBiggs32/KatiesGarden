# Security Policy — Katie's Garden

**Version:** 1.0 | **Date:** 2026-05-31 | **Owner:** Sam Biggs

This document covers the policy families required by NIST SP 800-53 Rev 4 P1 controls for a Low–Moderate impact system. It is intentionally concise — proportionate to a small-team e-commerce site, not a government agency.

---

## §AC — Access Control

**Policy:** Only authenticated users with the `admin` role may access administrative functions. Access is enforced at the edge (SWA route rules) and in the API (`SwaAuth.RequireAdmin()`). No shared credentials; each administrator uses their own OAuth identity.

**Procedure:**
- Admin access is granted by adding the user to the `admin` role in the Azure SWA portal under Role Management. Roles are reviewed quarterly or when a team member leaves.
- The developer bypass (`DEV_BYPASS_SECRET`) is only usable in non-Azure environments. It must never be set in production app settings.
- Guest order lookup requires order number, email, and order total (three-factor) to prevent enumeration.

---

## §AU — Audit and Accountability

**Policy:** All state-changing operations on orders, products, and collections are recorded in the `audit_logs` table. No PII (email addresses, names) appears in log output — they are replaced with a one-way SHA-256 hash before logging.

**Procedure:**
- Audit logs are append-only at the database level (`kg_app` role has INSERT-only on `audit_logs`).
- Application Insights telemetry is scrubbed of email patterns and sensitive query keys before transmission (`PiiScrubbingInitializer`).
- Audit log retention: 7 years (same as financial records, required by HMRC).

---

## §CM — Configuration Management

**Policy:** All infrastructure is defined in Terraform (`infra/`). No configuration changes are made ad-hoc in the Azure or Cloudflare portals. Terraform state is stored in Azure Blob Storage (encrypted, versioned).

**Procedure:**
- Security-relevant configuration (headers, CSP, route auth) lives in `staticwebapp.config.json` and is deployed via CI.
- All changes go through pull request review before merging to `main`.
- Dependabot weekly PRs for NuGet and GitHub Actions dependencies must be reviewed within 7 days of opening.

---

## §IA — Identification and Authentication

**Policy:** Admin authentication uses OAuth 2.0 via Azure SWA built-in providers (GitHub, Google, Microsoft AAD). No passwords are stored. MFA is the responsibility of the OAuth provider and is expected to be enabled on all admin accounts.

**Procedure:**
- OAuth client secrets rotate annually (or immediately if compromised). Secrets are stored in Azure Key Vault and referenced via `@Microsoft.KeyVault(...)` — they are never stored in plain text.
- VAPID keys (push notifications) rotate annually. Generate new keys with `npx web-push generate-vapid-keys`, update Key Vault, and redeploy.
- Stripe API keys rotate if a key is accidentally exposed. Rotate via the Stripe dashboard and update Key Vault.

**Rotation schedule:**

| Secret | Rotation | Owner |
|--------|----------|-------|
| OAuth client secrets | Annual | Sam |
| VAPID private key | Annual | Sam |
| Stripe secret key | On exposure | Sam |
| Database passwords (kg_migrate, kg_app) | Annual or on exposure | Sam |
| SMTP password | Annual | Sam |

---

## §IR — Incident Response

See `docs/security/incident-response-plan.md` for the full plan.

**Policy:** Security incidents are reported to the UK ICO within 72 hours if personal data is involved, per UK GDPR Article 33.

---

## §PS — Personnel Security

**Policy:** Admin access is granted on a need-to-know basis. Access is revoked within 24 hours of a team member leaving or changing role. OAuth role assignments are the access control list.

---

## §RA — Risk Assessment

**System categorisation:** Low–Moderate impact.
- **Confidentiality:** Moderate — customer PII (names, email, address, phone) and payment data (Stripe-tokenised; no raw card data stored).
- **Integrity:** Moderate — order integrity affects fulfilment; product catalogue integrity affects revenue.
- **Availability:** Low — the site being unavailable for hours is inconvenient but not life-safety.

**Identified risks and mitigations:**

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Credential leak (OAuth secrets, DB) | Low | High | Key Vault; rotation schedule |
| PII in logs | Mitigated | High | LogRedaction (hash); PiiScrubbingInitializer |
| Order enumeration | Low | Medium | Three-factor lookup; Cloudflare rate limit |
| Supply-chain CVE | Medium | Medium | Dependabot; CI CVE gate |
| Email spoofing | Low | Medium | SPF + DKIM + DMARC |
| Malicious file upload | Low | High | Magic-byte validation (ImageSignature) |

Risk assessment is reviewed annually or after a security incident.

---

## §SI — System and Information Integrity

**Policy:** Dependencies are kept up to date. CVEs in direct or transitive dependencies block deployment. Input is validated at all API boundaries.

**Retention schedule:**

| Data type | Retention | Basis |
|-----------|-----------|-------|
| Orders (financial fields, lines) | 7 years | HMRC requirement |
| Order PII (name, email, address) | Until erasure request or 2 years after last activity | UK GDPR Art. 17 |
| Audit logs | 7 years | HMRC; accountability |
| App Insights telemetry | 90 days (default) | Operational need |
| Newsletter subscribers | Until unsubscribe or 2 years inactive | UK GDPR |

**GDPR right to erasure:** Admin endpoint `POST /api/manage/orders/{id}/anonymise` replaces PII with `[erased]`, retains financial fields required by HMRC, stores a SHA-256 of the original email in the audit log for evidence of erasure, and unlinks the customer OAuth identity.
