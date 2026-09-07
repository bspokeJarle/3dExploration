[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.0",
    [string]$SecretsPath = "",
    [string]$InnoCompiler = "",
    [switch]$SkipPublish,
    [switch]$SkipInno
)

$ErrorActionPreference = "Stop"

function Resolve-InnoCompiler {
    param([string]$ExplicitPath)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        if (-not (Test-Path -LiteralPath $ExplicitPath)) {
            throw "Inno compiler not found at '$ExplicitPath'."
        }

        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }

    $command = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidatePaths = @(
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    )

    foreach ($candidate in $candidatePaths) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "Inno Setup 6 was not found. Install Inno Setup, or pass -InnoCompiler with the full path to ISCC.exe."
}

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

function Read-OnlineServicesConfig {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "secrets.json was not found at '$Path'."
    }

    $json = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $supabaseUrl = [string]$json.SupabaseUrl
    $supabaseAnonKey = [string]$json.SupabaseAnonKey

    if ([string]::IsNullOrWhiteSpace($supabaseUrl)) {
        throw "secrets.json is missing SupabaseUrl."
    }

    if ([string]::IsNullOrWhiteSpace($supabaseAnonKey)) {
        throw "secrets.json is missing SupabaseAnonKey."
    }

    Assert-PublicSupabaseAnonKey -Key $supabaseAnonKey

    [PSCustomObject]@{
        SupabaseUrl = $supabaseUrl
        SupabaseAnonKey = $supabaseAnonKey
    }
}

function Write-OnlineServicesConfig {
    param(
        [Parameter(Mandatory = $true)]$Config,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    $Config | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $Path -Encoding UTF8
}

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "TheOmegaStrain.Wpf\TheOmegaStrain.Wpf.csproj"
$installerScript = Join-Path $PSScriptRoot "TheOmegaStrain.iss"
$publishDir = Join-Path $repoRoot "artifacts\installer\publish\$Runtime"
$outputDir = Join-Path $repoRoot "artifacts\installer\output"
$stagingDir = Join-Path $repoRoot "artifacts\installer\staging"

if ([string]::IsNullOrWhiteSpace($SecretsPath)) {
    $SecretsPath = Join-Path $env:APPDATA "OmegaStrain\secrets.json"
}

$SecretsPath = (Resolve-Path -LiteralPath $SecretsPath).Path
$onlineServicesConfig = Read-OnlineServicesConfig -Path $SecretsPath

New-Item -ItemType Directory -Force -Path $publishDir, $outputDir, $stagingDir | Out-Null

if (-not $SkipPublish) {
    dotnet publish $projectPath `
        -c $Configuration `
        -r $Runtime `
        --self-contained true `
        -o $publishDir `
        /p:Version=$Version `
        /p:PublishSingleFile=false `
        /p:PublishReadyToRun=false
}

$mainExe = Join-Path $publishDir "TheOmegaStrain.exe"
if (-not (Test-Path -LiteralPath $mainExe)) {
    throw "Published executable not found at '$mainExe'. Run without -SkipPublish first."
}

$stagedSecrets = Join-Path $stagingDir "secrets.json"
Write-OnlineServicesConfig -Config $onlineServicesConfig -Path $stagedSecrets

$publishedOnlineServicesConfig = Join-Path $publishDir "online-services.json"
Write-OnlineServicesConfig -Config $onlineServicesConfig -Path $publishedOnlineServicesConfig

if ($SkipInno) {
    Write-Host "Publish and secrets staging completed."
    Write-Host "PublishDir: $publishDir"
    Write-Host "StagedSecrets: $stagedSecrets"
    Write-Host "PublishedOnlineServicesConfig: $publishedOnlineServicesConfig"
    Write-Host "Skipped Inno compilation."
    return
}

$iscc = Resolve-InnoCompiler -ExplicitPath $InnoCompiler

$isccArgs = @(
    "/DAppVersion=$Version",
    "/DPublishDir=$publishDir",
    "/DOutputDir=$outputDir",
    "/DSecretsSource=$stagedSecrets",
    $installerScript
)

& $iscc @isccArgs
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compiler failed with exit code $LASTEXITCODE."
}

Write-Host "Installer created in: $outputDir"

$outputBaseName = "TheOmegaStrainSetup-$Version"
$installerFiles = Get-ChildItem -LiteralPath $outputDir -File |
    Where-Object { $_.BaseName -eq $outputBaseName -or $_.BaseName -like "$outputBaseName-*" } |
    Sort-Object Name

if ($installerFiles) {
    Write-Host "Installer files:"
    foreach ($file in $installerFiles) {
        Write-Host "  $($file.FullName)"
    }
}
