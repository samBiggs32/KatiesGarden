# ---------------------------------------------------------------------------
# Cloudflare — DNS, security settings, and rate limiting
#
# Requires: cloudflare_zone_id and cloudflare_api_token variables
# The zone must already exist in Cloudflare (created via dashboard or
# imported with: terraform import cloudflare_zone.main <zone-id>)
# ---------------------------------------------------------------------------

# Apex domain → Azure Static Web App (CNAME-flattened by Cloudflare)
resource "cloudflare_record" "apex" {
  zone_id = var.cloudflare_zone_id
  name    = "katiesgarden.uk"
  type    = "CNAME"
  content = azurerm_static_web_app.main.default_host_name
  proxied = true
}

# www subdomain → Azure Static Web App
resource "cloudflare_record" "www" {
  zone_id = var.cloudflare_zone_id
  name    = "www"
  type    = "CNAME"
  content = azurerm_static_web_app.main.default_host_name
  proxied = true
}

# SPF — authorises Brevo's mail servers to send on behalf of katiesgarden.uk.
# ~all (softfail) allows monitoring before tightening to -all.
resource "cloudflare_record" "spf" {
  zone_id = var.cloudflare_zone_id
  name    = "katiesgarden.uk"
  type    = "TXT"
  content = "v=spf1 include:spf.brevo.com ~all"
  proxied = false
}

# DKIM — set var.dkim_record_value from Brevo → Senders & Domains → Authenticate.
# The record name (mail._domainkey) and selector prefix (mail) are Brevo defaults;
# adjust if your provider uses a different selector.
resource "cloudflare_record" "dkim" {
  count   = var.dkim_record_value != "" ? 1 : 0
  zone_id = var.cloudflare_zone_id
  name    = "mail._domainkey"
  type    = "TXT"
  content = var.dkim_record_value
  proxied = false
}

# DMARC — p=quarantine: unauthenticated mail goes to spam rather than inbox.
# Advance to p=reject once SPF+DKIM pass reliably (check rua reports after 2 weeks).
resource "cloudflare_record" "dmarc" {
  zone_id = var.cloudflare_zone_id
  name    = "_dmarc"
  type    = "TXT"
  content = "v=DMARC1; p=quarantine; pct=100; rua=mailto:team@katiesgarden.uk; ruf=mailto:team@katiesgarden.uk; adkim=s; aspf=s"
  proxied = false
}

# ---------------------------------------------------------------------------
# Zone-level security settings
# ---------------------------------------------------------------------------
resource "cloudflare_zone_settings_override" "main" {
  zone_id = var.cloudflare_zone_id

  settings {
    # Full SSL: Cloudflare → Azure SWA connection uses HTTPS
    ssl = "full"

    # Redirect all HTTP → HTTPS automatically
    always_use_https = "on"

    # Enforce TLS 1.2 minimum (drops TLS 1.0 and 1.1)
    min_tls_version = "1.2"

    # Cloudflare's standard DDoS/bot protection level
    security_level = "medium"

    # Browser Integrity Check — blocks requests with suspicious UA strings
    browser_check = "on"

    # HTTP/3 (QUIC) for improved performance
    http3 = "on"

    # Brotli compression at the edge
    brotli = "on"
  }
}

# ---------------------------------------------------------------------------
# Rate limiting — 5 requests per minute per IP on the two write endpoints
# Implemented as a Cloudflare Ruleset (replaces the deprecated rate_limit resource)
# ---------------------------------------------------------------------------
resource "cloudflare_ruleset" "rate_limit_api" {
  zone_id     = var.cloudflare_zone_id
  name        = "API rate limiting"
  description = "Limit /api/contact and /api/subscribe to 5 req/min per IP"
  kind        = "zone"
  phase       = "http_ratelimit"

  rules {
    action      = "block"
    description = "Block IPs that exceed 5 requests per minute on API write endpoints"
    enabled     = true

    action_parameters {
      response {
        status_code  = 429
        content_type = "text/plain"
        content      = "Too many requests — please wait a moment and try again."
      }
    }

    ratelimit {
      characteristics         = ["ip.src"]
      period                  = 60
      requests_per_period     = 5
      mitigation_timeout      = 300
    }

    # Matches POST requests to /api/contact and /api/subscribe
    expression = "(http.request.method eq \"POST\" and http.request.uri.path matches \"^/api/(contact|subscribe)$\")"
  }

  # Diagnostics is read-only but each call hits Brevo's API. Cap at
  # 10 req/min per IP so uptime monitors (typically one call per
  # minute) work fine but a hostile actor can't burn our Brevo quota.
  rules {
    action      = "block"
    description = "Cap /api/diagnostics at 10 req/min per IP to protect Brevo API quota"
    enabled     = true

    action_parameters {
      response {
        status_code  = 429
        content_type = "text/plain"
        content      = "Too many requests."
      }
    }

    ratelimit {
      characteristics     = ["ip.src"]
      period              = 60
      requests_per_period = 10
      mitigation_timeout  = 60
    }

    expression = "(http.request.uri.path eq \"/api/diagnostics\")"
  }

  # Checkout — 20 req/min: generous enough for normal use, blocks automated abuse
  rules {
    action      = "block"
    description = "Rate limit /api/checkout/* at 20 req/min per IP"
    enabled     = true

    action_parameters {
      response {
        status_code  = 429
        content_type = "text/plain"
        content      = "Too many requests — please wait a moment and try again."
      }
    }

    ratelimit {
      characteristics     = ["ip.src"]
      period              = 60
      requests_per_period = 20
      mitigation_timeout  = 60
    }

    expression = "(http.request.uri.path matches \"^/api/checkout/\")"
  }

  # Stripe webhook — 10 req/min; short mitigation so Stripe retries aren't permanently blocked
  rules {
    action      = "block"
    description = "Rate limit /api/webhooks/stripe at 10 req/min"
    enabled     = true

    action_parameters {
      response {
        status_code  = 429
        content_type = "text/plain"
        content      = "Too many requests."
      }
    }

    ratelimit {
      characteristics     = ["ip.src"]
      period              = 60
      requests_per_period = 10
      mitigation_timeout  = 60
    }

    expression = "(http.request.uri.path eq \"/api/webhooks/stripe\")"
  }

  # Admin API — 30 req/min: enough for normal use, blocks scripted enumeration
  rules {
    action      = "block"
    description = "Rate limit /api/admin/* at 30 req/min per IP"
    enabled     = true

    action_parameters {
      response {
        status_code  = 429
        content_type = "text/plain"
        content      = "Too many requests."
      }
    }

    ratelimit {
      characteristics     = ["ip.src"]
      period              = 60
      requests_per_period = 30
      mitigation_timeout  = 120
    }

    expression = "(http.request.uri.path matches \"^/api/admin/\")"
  }

  # Push notification endpoints — 10 req/min
  rules {
    action      = "block"
    description = "Rate limit /api/push/* at 10 req/min per IP"
    enabled     = true

    action_parameters {
      response {
        status_code  = 429
        content_type = "text/plain"
        content      = "Too many requests."
      }
    }

    ratelimit {
      characteristics     = ["ip.src"]
      period              = 60
      requests_per_period = 10
      mitigation_timeout  = 60
    }

    expression = "(http.request.uri.path matches \"^/api/push/\")"
  }

  # Guest order lookup — 10 req/min closes the order-number enumeration vector
  # (order-number space is 65,536/day; brute-force without rate limit is feasible).
  # Defence-in-depth with the email+total second factor in CustomerFunction.cs (A5).
  rules {
    action      = "block"
    description = "Rate limit /api/customer/* at 10 req/min per IP"
    enabled     = true

    action_parameters {
      response {
        status_code  = 429
        content_type = "text/plain"
        content      = "Too many requests."
      }
    }

    ratelimit {
      characteristics     = ["ip.src"]
      period              = 60
      requests_per_period = 10
      mitigation_timeout  = 300
    }

    expression = "(http.request.uri.path matches \"^/api/customer/\")"
  }

  # Product search — 30 req/min prevents automated scraping of the catalogue
  rules {
    action      = "block"
    description = "Rate limit /api/shop/search at 30 req/min per IP"
    enabled     = true

    action_parameters {
      response {
        status_code  = 429
        content_type = "text/plain"
        content      = "Too many requests."
      }
    }

    ratelimit {
      characteristics     = ["ip.src"]
      period              = 60
      requests_per_period = 30
      mitigation_timeout  = 60
    }

    expression = "(http.request.uri.path eq \"/api/shop/search\")"
  }
}
