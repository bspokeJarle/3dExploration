# The Omega Strain

The Omega Strain is a retro 3D action game built around low-poly worlds,
physics-driven flight, hostile alien seeders, drones, mothership fights,
powerups, score chasing, Steam achievements, and biome-specific atmosphere.

The game is inspired by old-school polygon graphics and classics like
Zarch/Virus, but it is now backed by RetroMesh: a reusable engine extracted from
this codebase for rendering, geometry, projection, collision, physics helpers,
audio foundations, timing, and other shared game-loop functionality.

## Repository Layout

- `TheOmegaStrain.Wpf/`: WPF host application and game assets.
- `TheOmegaStrain.Game/`: scenes, world setup, game objects, and game content.
- `TheOmegaStrain.Gameplay/`: controls, weapons, AI, effects, and gameplay rules.
- `TheOmegaStrain.Runtime/`: game loop orchestration, persistence hooks, audio
  wiring, scene transitions, and runtime services.
- `TheOmegaStrain.Domain/`: Omega-specific domain models and state.
- `TheOmegaStrain.Common/`: shared Omega infrastructure and engine adapters.
- `TheOmegaStrain.Steam/`: optional Steamworks integration.
- `TheOmegaStrain.Tests/`: automated tests.
- `TheOmegaStrain.Benchmarks/`: benchmark and performance test tooling.
- `Tools/`: small local tools such as controller probing.
- `docs/`: architecture and engine usage documentation.
- `installer/`: installer setup and related notes.

## Local Setup

### Workshop quick start

Requirements:

- Windows x64
- [Git for Windows](https://git-scm.com/download/win)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PowerShell

Open PowerShell and run this complete setup from the folder where you keep
source repositories:

```powershell
git clone https://github.com/bspokeJarle/TheOmegaStrain.git
Set-Location .\TheOmegaStrain
powershell -NoProfile -ExecutionPolicy Bypass -File .\build\Prepare-RetroMeshDependencies.ps1 -RestoreOmega -BuildOmega
dotnet run --project .\TheOmegaStrain.Wpf\TheOmegaStrain.Wpf.csproj
```

The setup script clones the public RetroMesh repository as a sibling checkout,
restores both repositories, builds the engine, runs its tests, and builds The
Omega Strain. Steam and Supabase are not required for local workshop use.

### Repository layout

For local development, keep The Omega Strain and RetroMesh as sibling
repositories:

```text
Repositories/
  TheOmegaStrain/
  RetroMesh/
  RetroMesh.GameTemplate/
```

The Omega Strain references the local RetroMesh build through `RetroMeshRoot` in
`Directory.Build.props`. The default points to `..\RetroMesh\`, so a normal
sibling checkout builds without machine-specific paths.

From a fresh checkout, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build\Prepare-RetroMeshDependencies.ps1 -RestoreOmega -BuildOmega
```

That script clones RetroMesh next to this repository if it is missing, builds
the RetroMesh projects, optionally runs the engine tests, restores this solution,
and builds The Omega Strain.

If RetroMesh lives somewhere else, override the root when building:

```powershell
dotnet build .\TheOmegaStrain.sln -p:RetroMeshRoot=C:\Path\To\RetroMesh\
```

Use Release for the Steam/installer output:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build\Prepare-RetroMeshDependencies.ps1 -Configuration Release -RestoreOmega -BuildOmega
dotnet build .\TheOmegaStrain.sln -c Release --no-restore
```

To force the old WPF renderer while debugging machine-specific Direct3D issues:

```powershell
$env:OMEGASTRAIN_RENDERER = "wpf"
dotnet run --project .\TheOmegaStrain.Wpf\TheOmegaStrain.Wpf.csproj
```

## RetroMesh Engine

The general engine/game boundary is documented in
`docs/USING_RETROMESH_ENGINE.md`.

## Build

Requirements:

- Windows
- Git for Windows
- .NET 10 SDK
- x64 runtime
- PowerShell

Build the solution:

```powershell
dotnet build .\TheOmegaStrain.sln
```

Run the game locally:

```powershell
dotnet run --project .\TheOmegaStrain.Wpf\TheOmegaStrain.Wpf.csproj
```

Run tests:

```powershell
dotnet test .\TheOmegaStrain.sln
```

## Optional Services

Steam support is optional and isolated in `TheOmegaStrain.Steam/`. The game
should continue to run without Steam, without the Steam client, and without the
Steam DLL.

Supabase/highscore configuration is optional at runtime. When cloud setup or
network access is unavailable, the game should fall back to local persistence.
For external player testing and release, the Supabase project must have both
`highscores` and `player_callsigns` configured so global scores and callsign
reservations work online. Client config is read from
`%APPDATA%\OmegaStrain\secrets.json` first, then from `online-services.json` or
`secrets.json` beside `TheOmegaStrain.exe`; release packaging should copy the
AppData config into output/content as `online-services.json`. Use
`Tools/Supabase/CleanupDuplicateHighscores.sql` and
`Tools/Supabase/SetupPlayerCallsignRegistry.sql` in the Supabase SQL Editor,
then run `Tools/Supabase/VerifyReleaseSupabaseSetup.sql` before distributing
keys. Missing callsign registry access is still treated as offline mode by the
client.

## Related Repositories

- [`RetroMesh`](https://github.com/bspokeJarle/RetroMesh): the reusable engine.
- `RetroMesh.GameTemplate`: a minimal game template using RetroMesh and the
  copied LogoCube intro scene as a starting point.

## Project Direction

The short-term goal is to keep The Omega Strain stable and shippable while
RetroMesh becomes a clean framework for future games. Game-specific behavior
should stay in the Omega projects; reusable rendering, geometry, projection,
collision, timing, and engine services should live in RetroMesh.

## AI Agent Guidance

AI coding agents should read `AGENTS.md` before changing code. For concrete
source-code patterns and test pointers, use `docs/AI_SOURCE_MAP.md`.
Project-specific Codex skills live under `.codex/skills/`.
