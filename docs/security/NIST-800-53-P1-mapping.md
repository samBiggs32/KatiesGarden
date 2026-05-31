# NIST SP 800-53 Rev 4 — P1 Control Mapping (SSP-lite)

**System:** Katie's Garden e-commerce platform  
**Classification:** Low–Moderate impact (florist online shop, UK GDPR in scope)  
**Date:** 2026-05-31  
**Source:** NIST SP 800-53 Rev 4 Appendix D, Table D-2 — P1 "implement first" controls

Controls are assigned one of four dispositions:

| Status | Meaning |
|--------|---------|
| **Met** | Implemented in code, config, or IaC in this repository |
| **Inherited** | Provided by a managed platform (Azure, Cloudflare, Neon); documented as provider responsibility |
| **Planned** | Config written; Sam must run the apply step — see linked runbook |
| **N-A** | Not applicable to this system type with rationale |

---

## Access Control (AC)

| Control | Title | Status | Evidence |
|---------|-------|--------|---------|
| AC-1 | Access Control Policy | Met | `docs/security/security-policy.md` §AC |
| AC-2 | Account Management | Met | SWA built-in OAuth; only users in `admin` role access admin routes (`staticwebapp.config.json`); no shared accounts |
| AC-3 | Access Enforcement | Met | `Api/Auth/SwaAuth.cs` — `RequireAdmin()` enforced on every admin endpoint; hardened dev bypass (A4) |
| AC-4 | Information Flow Enforcement | Met | CSP (`staticwebapp.config.json`), Cloudflare WAF, edge rate limits (`infra/cloudflare.tf`) |
| AC-5 | Separation of Duties | Planned | `infra/sql/roles.sql` — kg_migrate (DDL) vs kg_app (DML). Runbook: `runbooks/db-least-privilege.md` |
| AC-6 | Least Privilege | Planned | DB roles (B2); Key Vault Secrets User role for SWA identity (B1). Runbooks: db-least-privilege.md, keyvault-migration.md |
| AC-17 | Remote Access | Met | All access via HTTPS/TLS 1.2+ enforced at Cloudflare; admin via OAuth only; no SSH/RDP to serverless infra |
| AC-18 | Wireless Access | N-A | No wireless infrastructure managed by this system |
| AC-19 | Access Control for Mobile Devices | N-A | No MDM; mobile devices access only the public website |

## Audit and Accountability (AU)

| Control | Title | Status | Evidence |
|---------|-------|--------|---------|
| AU-1 | Audit Policy | Met | `docs/security/security-policy.md` §AU |
| AU-2 | Audit Events | Met | `Api/Auditing/AuditService.cs` — logs status changes, notes edits, anonymisation, login; App Insights for HTTP requests |
| AU-3 | Content of Audit Records | Met | AuditLog entity: timestamp, event type, entity, actor, details JSON — sufficient to reconstruct who did what and when |
| AU-8 | Time Stamps | Met | All `AuditLog.CreatedAt` and `Order.UpdatedAt` are UTC (`DateTime.UtcNow`); App Insights timestamps are UTC |
| AU-9 | Protection of Audit Information | Planned | `infra/sql/roles.sql` — `REVOKE UPDATE, DELETE ON audit_logs FROM kg_app` makes audit records append-only at the DB level. PII scrubbed before reaching App Insights by `PiiScrubbingInitializer` (A2). Runbook: db-least-privilege.md |
| AU-12 | Audit Record Generation | Met | `IAuditService.LogAsync` called on all state-changing operations; App Insights captures all HTTP requests |

## Configuration Management (CM)

| Control | Title | Status | Evidence |
|---------|-------|--------|---------|
| CM-1 | Configuration Management Policy | Met | `docs/security/security-policy.md` §CM |
| CM-2 | Baseline Configuration | Met | All infra defined in `infra/*.tf`; `staticwebapp.config.json` for SWA config; EF Core migrations for DB schema |
| CM-6 | Configuration Settings | Met | Security headers, CSP, TLS 1.2 min, rate limits all in IaC or SWA config (not set ad-hoc in portal) |
| CM-7 | Least Functionality | Met | Functions host exposes only declared `[Function]` triggers; no open ports; no admin endpoints without auth |

## Identification and Authentication (IA)

| Control | Title | Status | Evidence |
|---------|-------|--------|---------|
| IA-1 | IA Policy | Met | `docs/security/security-policy.md` §IA |
| IA-2 | Identification and Authentication (Org Users) | Met | SWA built-in OAuth (GitHub/Google/Microsoft AAD) — no password database; MFA delegated to OAuth provider; dev bypass hardened (A4) |
| IA-5 | Authenticator Management | Planned | OAuth secrets in Key Vault (B1); rotation schedule in `docs/security/security-policy.md` §IA |
| IA-8 | Identification and Authentication (Non-Org Users) | Met | Guest order lookup requires order number + email + order total (three-factor, constant-time, A5); no anonymous write access |

## Incident Response (IR)

| Control | Title | Status | Evidence |
|---------|-------|--------|---------|
| IR-1 | IR Policy | Met | `docs/security/security-policy.md` §IR |
| IR-4 | Incident Handling | Met | `docs/security/incident-response-plan.md` |
| IR-5 | Incident Monitoring | Met | App Insights alerts on error spikes; IR plan §Monitoring |
| IR-6 | Incident Reporting | Met | IR plan §Reporting (ICO 72-hour notification requirement) |
| IR-8 | Incident Response Plan | Met | `docs/security/incident-response-plan.md` |

## Maintenance (MA)

| Control | Title | Status | Evidence |
|---------|-------|--------|---------|
| MA-4 | Non-local Maintenance | Inherited | Azure Functions and SWA have no SSH/RDP maintenance surface; updates are code deploys |

## Media Protection (MP)

| Control | Title | Status | Evidence |
|---------|-------|--------|---------|
| MP-2 | Media Access | Inherited | Azure physical media controls |
| MP-6 | Media Sanitization | Inherited | Azure hardware disposal; no removable media managed by this system |
| MP-7 | Media Use | N-A | No removable media |

## Physical and Environmental Protection (PE)

| Control | Title | Status | Evidence |
|---------|-------|--------|---------|
| PE-2 | Physical Access Authorizations | Inherited | Azure data centre physical security |
| PE-3 | Physical Access Control | Inherited | Azure data centre physical security |
| PE-6 | Monitoring Physical Access | Inherited | Azure data centre physical security |
| PE-8 | Visitor Access Records | Inherited | Azure data centre physical security |
| PE-12 | Emergency Lighting | Inherited | Azure data centre |
| PE-13 | Fire Protection | Inherited | Azure data centre |
| PE-14 | Temperature and Humidity Controls | Inherited | Azure data centre |
| PE-15 | Water Damage Protection | Inherited | Azure data centre |
| PE-16 | Delivery and Removal | Inherited | Azure data centre |

## Planning (PL)

| Control | Title | Status | Evidence |
|---------|-------|--------|---------|
| PL-2 | System Security Plan | Met | This document is the SSP-lite |

## Personnel Security (PS)

| Control | Title | Status | Evidence |
|---------|-------|--------|---------|
| PS-1 | PS Policy | Met | `docs/security/security-policy.md` §PS |

## Risk Assessment (RA)

| Control | Title | Status | Evidence |
|---------|-------|--------|---------|
| RA-1 | RA Policy | Met | `docs/security/security-policy.md` §RA |
| RA-2 | Security Categorization | Met | Low–Moderate impact; PII (names, email, address) and payment data (Stripe-tokenised) in scope. Documented in `docs/security/security-policy.md` §RA |
| RA-3 | Risk Assessment | Met | `docs/security/security-policy.md` §RA |
| RA-5 | Vulnerability Scanning | Met | `dotnet list package --vulnerable --include-transitive` in CI gate (A6); Dependabot weekly (`.github/dependabot.yml`) |

## System and Services Acquisition (SA)

| Control | Title | Status | Evidence |
|---------|-------|--------|---------|
| SA-2 | Allocation of Resources | N-A | Federal budgeting control — not applicable to a private UK business |
| SA-11 | Developer Security Testing and Evaluation | Met | Unit + integration test suite in CI (177 tests); CVE gate in both `ci.yml` and `production-deploy.yml` (A6) |

## System and Communications Protection (SC)

| Control | Title | Status | Evidence |
|---------|-------|--------|---------|
| SC-5 | Denial of Service Protection | Met | Cloudflare DDoS mitigation; rate limits on all API routes (contact, subscribe, checkout, webhook, admin, push, customer, search) |
| SC-7 | Boundary Protection | Met | Cloudflare WAF; CSP; SWA route auth; admin routes require `admin` role |
| SC-8 | Transmission Confidentiality and Integrity | Met | TLS 1.2+ enforced at Cloudflare; HSTS `max-age=31536000; includeSubDomains`; HTTPS redirect |
| SC-12 | Cryptographic Key Establishment and Management | Planned | Secrets in Key Vault (B1); VAPID keys generated offline; Stripe keys managed by Stripe. Runbook: keyvault-migration.md |
| SC-13 | Cryptographic Protection | Met | TLS 1.2+ for all transport; Stripe handles PCI; Neon encrypts data at rest; SHA-256 for PII tokens in audit log |
| SC-15 | Collaborative Computing | N-A | No screen-sharing, remote desktop, or collaborative computing systems |
| SC-18 | Mobile Code | Met | CSP `script-src` removed `unsafe-eval`; only `wasm-unsafe-eval` for Blazor WASM; all external scripts explicitly allowlisted (A7) |
| SC-19 | Voice over IP | N-A | No VoIP |
| SC-20 | Secure Name/Address Resolution (Authoritative) | Inherited | Cloudflare DNS with DNSSEC |
| SC-21 | Secure Name/Address Resolution (Recursive) | Inherited | Cloudflare DNS resolver |
| SC-22 | Architecture and Provisioning for Name/Address Resolution | Inherited | Cloudflare DNS infrastructure |
| SC-23 | Session Authenticity | Met | SWA sessions managed by Azure (signed JWT tokens); HTTPS-only; `X-Frame-Options: SAMEORIGIN` |
| SC-28 | Protection of Information at Rest | Planned | Secrets in Key Vault (encrypted at rest with HSM-backed keys, B1); Neon encrypts DB at rest; Azure Blob Storage encrypted at rest |
| SC-39 | Process Isolation | Inherited | Azure Functions isolated worker model; each invocation is a separate process |

## System and Information Integrity (SI)

| Control | Title | Status | Evidence |
|---------|-------|--------|---------|
| SI-1 | SI Policy | Met | `docs/security/security-policy.md` §SI |
| SI-2 | Flaw Remediation | Met | Dependabot weekly PR alerts; CVE gate blocks deployment of vulnerable packages (A6); EF Core migrations for schema fixes |
| SI-3 | Malware Protection | Met | Image uploads validated by magic bytes (`ImageSignature.cs`, A3) before reaching blob storage; Cloudflare malware scanning at edge |
| SI-10 | Information Input Validation | Met | FluentValidation on all API inputs; `ImageSignature` content-type validation (A3); `OrderVerification` constant-time total check (A5) |
| SI-11 | Error Handling | Met | All API errors return generic messages (no stack traces); `logger.LogError` for internal detail; `Responses` helper for consistent shape |
| SI-12 | Information Management and Retention | Met | PII hashed before logging (`LogRedaction`, A1); GDPR erasure endpoint `POST /api/manage/orders/{id}/anonymise` (A8); financial records retained per HMRC 7-year requirement; `docs/security/security-policy.md` §SI retention schedule |
