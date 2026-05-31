# Runbook: Email Authentication Records (SPF, DKIM, DMARC)

Add SPF, DKIM, and DMARC DNS records to protect katiesgarden.uk from email spoofing and ensure outbound mail from Brevo is deliverable.

The DNS records are managed via Terraform (`infra/cloudflare.tf`). The DMARC policy starts at `p=quarantine` (unauthenticated mail to spam); advance to `p=reject` after 2 weeks of clean rua reports.

## Prerequisites

- Brevo account with the sending domain `katiesgarden.uk` added
- Terraform applied (cloudflare provider) with Cloudflare zone access
- DMARC aggregate report monitoring active (rua is set to `team@katiesgarden.uk`)

## Steps

### 1. Generate DKIM key in Brevo

1. Log into Brevo → Senders & IPs → Domains.
2. Click **Authenticate** next to `katiesgarden.uk`.
3. Brevo will show a TXT record value like:  
   `v=DKIM1; k=rsa; p=MIIBIjAN...`
4. Copy the **full value** (everything after the `=` sign in the record content).

### 2. Set the DKIM value in tfvars

```hcl
dkim_record_value = "v=DKIM1; k=rsa; p=MIIBIjAN..."
```

### 3. Apply Terraform

```bash
cd infra
terraform plan   # review: spf record create, dkim record create, dmarc record update (p=none → p=quarantine)
terraform apply
```

Expect 3 changes: SPF TXT create, DKIM TXT create (because `count = 1` now), DMARC TXT update.

### 4. Validate DNS propagation

Wait up to 5 minutes for Cloudflare to propagate, then verify:

```bash
dig TXT katiesgarden.uk          | grep "v=spf1"
dig TXT mail._domainkey.katiesgarden.uk | grep "v=DKIM1"
dig TXT _dmarc.katiesgarden.uk   | grep "p=quarantine"
```

All three should return records.

### 5. Send a test email

Send a test email from Brevo to a Gmail account and inspect the received headers:

- `Authentication-Results` should contain `spf=pass` and `dkim=pass`.
- No DMARC failure warnings in the Google Postmaster Tools.

### 6. Monitor rua reports (2 weeks)

DMARC aggregate reports arrive at `team@katiesgarden.uk` as XML attachments. After 2 weeks of reports showing only legitimate sources (Brevo), advance to `p=reject`:

In `infra/cloudflare.tf`, change:
```hcl
content = "v=DMARC1; p=quarantine; ..."
```
to:
```hcl
content = "v=DMARC1; p=reject; ..."
```

Then `terraform apply`.

## Rollback

If legitimate mail is being rejected unexpectedly, revert DMARC to `p=none`:

```hcl
content = "v=DMARC1; p=none; rua=mailto:team@katiesgarden.uk"
```

Then `terraform apply`. SPF and DKIM records do not need to be removed.
