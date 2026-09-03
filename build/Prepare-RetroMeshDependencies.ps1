[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$EngineRepoUrl = "https://github.com/bspokeJarle/RetroMesh.git",

    [string]$EnginePath = "",

    [switch]$RestoreOmega,

    [switch]$BuildOmega,

    [switch]$SkipEngineTests
)

$ErrorActionPreference = "Stop"

function Invoke-CommandChecked {
    param(
        [string]$Command,
        [string[]]$Arguments
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Command $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$omegaSolutionPath = Join-Path $repoRoot "TheOmegaStrain.sln"

if ([string]::IsNullOrWhiteSpace($EnginePath)) {
    $EnginePath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "..\RetroMesh"))
}
elseif (-not [System.IO.Path]::IsPathRooted($EnginePath)) {
    $EnginePath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $EnginePath))
}
else {
    $EnginePath = [System.IO.Path]::GetFullPath($EnginePath)
}

if (-not (Test-Path -LiteralPath $EnginePath)) {
    Write-Host "RetroMesh checkout not found. Cloning from $EngineRepoUrl..."
    Invoke-CommandChecked -Command "git" -Arguments @("clone", $EngineRepoUrl, $EnginePath)
}

$engineSolutionPath = Join-Path $EnginePath "RetroMesh.Engine.slnx"
if (-not (Test-Path -LiteralPath $engineSolutionPath)) {
    $engineSolutionPath = Join-Path $EnginePath "RetroMesh.Engine.sln"
}

if (-not (Test-Path -LiteralPath $engineSolutionPath)) {
    throw "RetroMesh solution was not found in '$EnginePath'."
}

Write-Host "Building RetroMesh dependencies from '$EnginePath' ($Configuration)..."
Invoke-CommandChecked -Command "dotnet" -Arguments @("restore", $engineSolutionPath)
Invoke-CommandChecked -Command "dotnet" -Arguments @("build", $engineSolutionPath, "-c", $Configuration, "--no-restore")

if (-not $SkipEngineTests) {
    $engineTestsPath = Join-Path $EnginePath "RetroMesh.Engine.Tests\RetroMesh.Engine.Tests.csproj"
    if (Test-Path -LiteralPath $engineTestsPath) {
        Write-Host "Running RetroMesh.Engine tests..."
        Invoke-CommandChecked -Command "dotnet" -Arguments @("test", $engineTestsPath, "-c", $Configuration, "--no-build")
    }
}

if ($RestoreOmega) {
    Write-Host "Restoring The Omega Strain solution..."
    Invoke-CommandChecked -Command "dotnet" -Arguments @("restore", $omegaSolutionPath)
}

if ($BuildOmega) {
    Write-Host "Building The Omega Strain solution..."
    Invoke-CommandChecked -Command "dotnet" -Arguments @(
        "build",
        $omegaSolutionPath,
        "-c",
        $Configuration,
        "--no-restore",
        "-p:RetroMeshRoot=$EnginePath\"
    )
}

Write-Host "RetroMesh dependencies are ready."
