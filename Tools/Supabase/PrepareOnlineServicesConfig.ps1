[CmdletBinding()]
param(
    [string]$SourcePath = "",
    [Parameter(Mandatory = $true)]
    [string]$DestinationPath
)

$ErrorActionPreference = "Stop"

function ConvertFrom-Base64Url {
    param([string]$Value)

    $padded = $Value.Replace('-', '+').Replace('_', '/')
    switch ($padded.Length % 4) {
        2 { $padded += "==" }
        3 { $padded += "=" }
    }

    [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($padded))
}

function Assert-PublicSupabaseAnonKey {
    param([string]$Key)

    if ($Key -match "service_role") {
        throw "SupabaseAnonKey appears to be a service_role key. Use the public anon key only."
    }

    $parts = $Key -split "\."
    if ($parts.Length -lt 2) {
        return
    }

    try {
        $payload = ConvertFrom-Base64Url -Value $parts[1] | ConvertFrom-Json
        if ($payload.role -eq "service_role") {
            throw "SupabaseAnonKey is a service_role key. Use the public anon key only."
        }
    }
    catch {
        if ($_.Exception.Message -like "*service_role*") {
            throw
        }
    }
}

if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    $SourcePath = Join-Path $env:APPDATA "OmegaStrain\secrets.json"
}

if (-not (Test-Path -LiteralPath $SourcePath)) {
    throw "Supabase config was not found at '$SourcePath'."
}

$sourceJson = Get-Content -LiteralPath $SourcePath -Raw | ConvertFrom-Json
$supabaseUrl = [string]$sourceJson.SupabaseUrl
$supabaseAnonKey = [string]$sourceJson.SupabaseAnonKey

if ([string]::IsNullOrWhiteSpace($supabaseUrl)) {
    throw "Supabase config is missing SupabaseUrl."
}

if ([string]::IsNullOrWhiteSpace($supabaseAnonKey)) {
    throw "Supabase config is missing SupabaseAnonKey."
}

Assert-PublicSupabaseAnonKey -Key $supabaseAnonKey

$destinationDirectory = Split-Path -Parent $DestinationPath
if (-not [string]::IsNullOrWhiteSpace($destinationDirectory)) {
    New-Item -ItemType Directory -Force -Path $destinationDirectory | Out-Null
}

[PSCustomObject]@{
    SupabaseUrl = $supabaseUrl
    SupabaseAnonKey = $supabaseAnonKey
} | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $DestinationPath -Encoding UTF8

Write-Host "Online services config written to: $DestinationPath"
