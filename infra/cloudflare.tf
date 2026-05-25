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

# DMARC — "monitor only" (p=none) to start; tighten to p=quarantine once you
# have confirmed all legitimate mail sources appear in rua reports.
resource "cloudflare_record" "dmarc" {
  zone_id = var.cloudflare_zone_id
  name    = "_dmarc"
  type    = "TXT"
  content = "v=DMARC1; p=none; rua=mailto:team@katiesgarden.uk"
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
      characteristics         = ["ip.src"]
      period                  = 60
      requests_per_period     = 10
      mitigation_timeout      = 60
    }

    expression = "(http.request.uri.path eq \"/api/diagnostics\")"
  }
}
