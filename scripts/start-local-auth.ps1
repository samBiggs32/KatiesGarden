<#
.SYNOPSIS
    Runs Katie's Garden locally behind the Azure Static Web Apps emulator so the
    admin sign-in + role flow can be tested without deploying to Azure.

.DESCRIPTION
    Azure Static Web Apps provides the /.auth/* endpoints (login, /.auth/me,
    roles) at runtime. Those don't exist under a plain Aspire / dotnet run, which
    is why admin login can't be exercised locally. The SWA CLI emulates them.

    This script:
      1. Starts the Azure Functions API (func) on http://localhost:7071 in a new
         window — it reads Api/local.settings.json for DATABASE_URL etc.
      2. Starts the SWA emulator on http://localhost:4280, which itself launches
         the Blazor dev server (per swa-cli.config.json) and proxies /api to func.

    Then browse to http://localhost:4280/admin/login, click any provider, and in
    the emulator's fake-login form add "admin" to the Roles field. You'll land on
    /admin as an administrator.

.NOTES
    Prerequisites (install once):
      npm install -g @azure/static-web-apps-cli
      npm install -g azure-functions-core-tools@4   (or winget equivalent)
#>

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

if (-not (Get-Command swa -ErrorAction SilentlyContinue)) {
    Write-Error "SWA CLI not found. Install it with:  npm install -g @azure/static-web-apps-cli"
}
if (-not (Get-Command func -ErrorAction SilentlyContinue)) {
    Write-Error "Azure Functions Core Tools not found. Install it with:  npm install -g azure-functions-core-tools@4"
}

# Prefer PowerShell 7 (pwsh) for the API window, fall back to Windows PowerShell.
$shell = if (Get-Command pwsh -ErrorAction SilentlyContinue) { "pwsh" } else { "powershell" }

Write-Host "Starting Azure Functions API on http://localhost:7071 ..." -ForegroundColor Cyan
Start-Process $shell -ArgumentList "-NoExit", "-Command", "Set-Location '$root/Api'; func start"

Write-Host "Starting SWA emulator on http://localhost:4280 (also launches the Blazor dev server) ..." -ForegroundColor Cyan
Write-Host "When ready, open http://localhost:4280/admin/login and add 'admin' to the Roles field." -ForegroundColor Green
Set-Location $root
swa start
