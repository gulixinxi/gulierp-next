# WEB-PREVIEW-001: backend health + /csrf probe.
# Usage: probe-backend.ps1 [-BaseUrl 'http://127.0.0.1:5000']
param(
  [string]$BaseUrl = 'http://127.0.0.1:5000'
)
$ErrorActionPreference = 'Continue'

function Probe([string]$path, [string]$label) {
  $url = "$BaseUrl$path"
  try {
    $resp = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5
    $body = $resp.Content
    if ($body -and $body.Length -gt 160) { $body = $body.Substring(0, 160) + '...' }
    Write-Host "[$label] $url -> $($resp.StatusCode)  body: $body"
  } catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code) {
      Write-Host "[$label] $url -> HTTP $code"
    } else {
      Write-Host "[$label] $url -> UNREACHABLE ($($_.Exception.Message -split [Environment]::NewLine)[0])"
    }
  }
}

Probe '/health/live' 'LIVE'
Probe '/health/ready' 'READY'
Probe '/api/v1/auth/csrf' 'CSRF'
