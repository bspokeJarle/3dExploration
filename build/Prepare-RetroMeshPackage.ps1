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

$scriptPath = Join-Path $PSScriptRoot "Prepare-RetroMeshDependencies.ps1"
& $scriptPath @PSBoundParameters
