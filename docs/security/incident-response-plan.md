# Incident Response Plan — Katie's Garden

**Version:** 1.0 | **Date:** 2026-05-31 | **Owner:** Sam Biggs

---

## Scope

Covers security incidents affecting the Katie's Garden platform including data breaches, service compromise, and unauthorised access.

## §Monitoring — Detection

| Signal | Source | Alert |
|--------|--------|-------|
| Elevated error rates | App Insights | Smart Detection alert |
| Repeated 401/403 | App Insights | Custom alert rule on `requests/failed` |
| DMARC failures | rua report to team@katiesgarden.uk | Weekly review |
| Vulnerable dependency | CI CVE gate | Build failure + Dependabot PR |
| Unusual order volume | App Insights dashboard | Manual review |

## Response Phases

### 1. Identify (≤ 1 hour)

Determine:
- Which system or data is affected?
- Is customer PII involved?
- Is the incident ongoing?

Check: App Insights Live Metrics, Azure portal activity log, Stripe dashboard, Neon query history.

### 2. Contain (≤ 2 hours)

| Scenario | Action |
|----------|--------|
| Compromised OAuth secret | Rotate via provider + Key Vault; redeploy |
| Compromised DB credential | Rotate in Neon + Key Vault; redeploy |
| Compromised Stripe key | Rotate in Stripe dashboard + Key Vault; redeploy |
| Admin account taken over | Remove admin role from SWA Role Management immediately |
| Active SQL/injection attack | Enable Cloudflare "Under Attack" mode; block offending IP ranges |
| Malicious file in blob storage | Delete the file; review audit logs for upload source |

### 3. Eradicate

- Rotate all credentials in the affected area (not just the compromised one).
- Review audit logs for evidence of lateral movement.
- Check for any exfiltrated data in order/customer tables.

### 4. Recover

- Verify all secrets are rotated and deployed.
- Confirm App Insights shows a return to normal error rates.
- Restore from Neon PITR if DB data was modified maliciously (see `backup-and-recovery.md`).

### 5. Learn

Document the incident within 5 days:
- Timeline, root cause, customer impact, remediation steps, improvements made.
- Store in a private location (not this repo, as it may contain sensitive detail).

## §Reporting

### UK GDPR Article 33 — ICO Notification

If the incident involves a personal data breach (name, email, address, phone, or order details exposed or exfiltrated):

- **Deadline:** Report to the ICO within **72 hours** of becoming aware.
- **Report via:** `ico.org.uk/report-a-breach`
- **Information needed:** nature of the breach, categories and approximate number of data subjects affected, likely consequences, measures taken.

If 72 hours is not possible, report as soon as feasible with an explanation of the delay.

### Customer Notification

If the breach is likely to result in high risk to customers (financial harm, identity theft):
- Notify affected customers directly "without undue delay" (UK GDPR Art. 34).
- Draft notification email before reporting to ICO so both can go out simultaneously.

## Contacts

| Role | Contact |
|------|---------|
| Site owner | Sam Biggs — eeysb11@gmail.com |
| Stripe fraud team | dashboard.stripe.com/support |
| Azure support | portal.azure.com → Support |
| ICO breach report | ico.org.uk/report-a-breach |
| Neon support | neon.tech/docs/introduction/support |
