---
name: omega-create-scene
description: "Use when creating or extending The Omega Strain gameplay scenes, scene folders, scene directors, scene registration, default scene objects, biome setup, scene overlays, scene progression, or scene-specific tests. Do not use for isolated object geometry/control changes unless a scene change is also required."
---

# Create Or Extend An Omega Scene

This skill keeps new scenes aligned with The Omega Strain's existing gameplay
scene patterns. Before implementing, read `AGENTS.md` and the scene-related
sections of `docs/AI_SOURCE_MAP.md`.

If the scene needs a new object type or new movement controller, also use
`.codex/skills/omega-add-game-object/SKILL.md`.

## First Files To Open

Start with the scene lifecycle and contracts:

- `TheOmegaStrain.Domain/Interfaces/Scenes/ISceneHandler.cs`
- `TheOmegaStrain.Domain/Interfaces/Scenes/ISceneDirector.cs`
- `TheOmegaStrain.Game/SceneManagement/SceneHandler.cs`
- `TheOmegaStrain.Game/World/GameWorld.cs`

Then inspect a scene close to the requested biome or gameplay:

- Simple campaign baseline: `TheOmegaStrain.Game/Scenes/Scene1/Scene1.cs`
- Rainforest/weather scene: `TheOmegaStrain.Game/Scenes/Scene3/Scene3.cs`
- Winter scene: `TheOmegaStrain.Game/Scenes/Scene4/Scene4.cs`
- Desert scene with custom placement: `TheOmegaStrain.Game/Scenes/Scene6/Scene6.cs`
- Dynamic endless scene: `TheOmegaStrain.Game/Scenes/SceneSimulation/SceneSimulation.cs`
- Small readable setup scene: `TheOmegaStrain.Game/Scenes/Tutorial/TutorialScene.cs`

Director examples:

- `TheOmegaStrain.Game/Scenes/Scene1/Scene1Director.cs`
- `TheOmegaStrain.Game/Scenes/Scene6/Scene6Director.cs`
- `TheOmegaStrain.Game/Scenes/SceneSimulation/SceneSimulationDirector.cs`
- `TheOmegaStrain.Game/Scenes/Tutorial/TutorialSceneDirector.cs`

## Default Gameplay Scene Payload

A normal gameplay scene should usually create these things:

- A private `Surface` instance.
- `SceneMusic`, `SceneType`, `SceneBiome`, `GameMode`, and difficulty/tuning
  properties.
- A scene `Director` when phase transitions, enemy activation, victory,
  defeat, or checkpoint rules are needed.
- A player ship created with `Ship.CreateShip(Surface)`.
- Ship weapons using existing weapon factories and `new Weapons(...)`.
- A `SeederGuidanceArrow`.
- Seeders using `SeederPlacementHelpers.AddSeederGroup(...)`.
- Optional drones/bombers/wildlife as established by similar scenes.
- An inactive mothership or boss phase when the director will activate it.
- A surface viewport from `Surface.GetSurfaceViewPort()`.
- A biome emitter or biome objects when the biome uses one.

Do not start by hand-rolling scene setup from scratch. Copy the closest scene
shape, then change content/tuning deliberately.

## Scene Class Pattern

New campaign scenes normally live in:

```text
TheOmegaStrain.Game/Scenes/SceneN/SceneN.cs
TheOmegaStrain.Game/Scenes/SceneN/SceneNDirector.cs
```

Baseline shape:

```csharp
public class SceneN : IScene
{
    private readonly Surface Surface = new();

    public string SceneMusic { get; } = "music_flight";
    public SceneTypes SceneType { get; } = SceneTypes.Game;
    public SceneBiomeTypes SceneBiome { get; } = SceneBiomeTypes.HillsWoods;
    public GameModes GameMode { get; } = GameModes.Playback;
    public ISceneDirector Director { get; } = new SceneNDirector();

    public float InfectionThresholdPercent { get; } = 13.0f;
    public int InfectionSpreadRate { get; } = 5;
    public int SeederOffscreenSpeedFactor { get; } = 12;
    public float LocalInfectionSpreadDelaySec { get; } = 4.0f;
    public float LocalInfectionSpreadRadius { get; } = 5000f;

    public void SetupScene(I3dWorld world)
    {
        // Follow the setup order from the closest existing scene.
    }
}
```

Tune values against nearby scenes and tests. Later scenes should be harder, but
must remain playable.

## SetupScene Order

Use the closest existing scene as source of truth. A safe default order is:

1. Resolve `ws = SurfaceSetup.WorldScale`.
2. Set `GameState.SurfaceState.SceneBiome = SceneBiome` before map generation
   when biome-specific map generation depends on it.
3. Create the ship with `Ship.CreateShip(Surface)`.
4. Generate or load the map with `Surface.Create2DMap(...)`.
5. Create ship weapons and assign `ship.WeaponSystems`.
6. Initialize ship `Rotation`, `WorldPosition`, `ObjectName`, `ImpactStatus`,
   `CrashBoxDebugMode`.
7. Add the ship to `world.WorldInhabitants`.
8. Add `SeederGuidanceArrow`.
9. Add enemies and wildlife, adding AI/progression objects to
   `GameState.SurfaceState.AiObjects`.
10. Add the surface viewport and assign
    `GameState.SurfaceState.SurfaceViewportObject`.
11. Add weather/biome emitters and surface-based decorations.

The exact position of the surface viewport in the method varies between older
scenes, but the final state must contain one `"Surface"` object in
`WorldInhabitants` and `GameState.SurfaceState.SurfaceViewportObject`.

## Default Object Snippets

Ship:

```csharp
var ship = Ship.CreateShip(Surface);
Surface.Create2DMap(30000, 15000, GameMode, "SceneNSurfaceRecording.retro");
var weapons = new List<I3dObject> { Lazer.CreateLazer(Surface), Bullet.CreateBullet(Surface) };
ship.Rotation = new Vector3 { };
ship.WorldPosition = new Vector3 { };
ship.ObjectName = "Ship";
ship.ImpactStatus = new ImpactStatus { ObjectHealth = ShipSetup.DefaultShipHealth };
ship.CrashBoxDebugMode = false;
ship.WeaponSystems = new Weapons(weapons, ship.Movement!, ship);
world.WorldInhabitants.Add(ship);
```

Guidance arrow:

```csharp
var guidanceArrow = SeederGuidanceArrow.CreateSeederGuidanceArrow(Surface);
guidanceArrow.ObjectOffsets = new Vector3 { x = 0, y = -200, z = 200 };
guidanceArrow.Rotation = new Vector3 { x = WorldViewSetup.SurfaceFacingObjectPitchDegrees, y = 0, z = 90 };
guidanceArrow.WorldPosition = new Vector3 { };
guidanceArrow.ObjectName = "SeederGuidanceArrow";
guidanceArrow.ImpactStatus = new ImpactStatus { };
guidanceArrow.CrashBoxDebugMode = false;
world.WorldInhabitants.Add(guidanceArrow);
```

Surface viewport:

```csharp
var surfaceObject = (OmegaObject3D)Surface.GetSurfaceViewPort();
surfaceObject.ObjectName = "Surface";
surfaceObject.ObjectOffsets = new Vector3 { x = 70 * ScreenSetup.ScreenScaleX, y = 500 * ScreenSetup.ScreenScaleY, z = 400 };
surfaceObject.Rotation = new Vector3 { x = WorldViewSetup.SurfacePitchDegrees, y = 0, z = 0 };
surfaceObject.WorldPosition = new Vector3 { };
surfaceObject.Movement = new GroundControls();
surfaceObject.ParentSurface = Surface;
surfaceObject.ImpactStatus = new ImpactStatus { };
surfaceObject.CrashBoxDebugMode = false;
surfaceObject.CrashBoxesFollowRotation = false;
world.WorldInhabitants.Add(surfaceObject);
GameState.SurfaceState.SurfaceViewportObject = surfaceObject;
```

Seeder group:

```csharp
SeederPlacementHelpers.AddSeederGroup(
    world,
    Surface,
    GameState.SurfaceState.GlobalMapPosition,
    totalSeederCount: 12,
    regularSeed: 3001,
    nearSeederCount: 4);
```

Use optional radius/powerup arguments only when the existing dynamic powerup or
distance pattern requires them.

## AI Objects And Scene Progression

Objects that count for gameplay progression must be represented in
`GameState.SurfaceState.AiObjects`. Typical examples:

- `"Seeder"`
- `"KamikazeDrone"`
- `"ZeppelinBomber"` when it is part of active scene gameplay
- `"MotherShipSmall"`, `"MotherShipMedium"`, `"MotherShipLarge"`
- Passive score targets only when existing similar scenes track them there.

If an enemy is visible but not in `AiObjects`, the director, HUD counters,
checkpoint restore, mothership activation, or victory may not see it.

## Director Pattern

Use a director when the scene has phases. The common campaign pattern is:

- Drones start inactive.
- Drones activate when decoy is unlocked.
- Mothership starts inactive.
- Mothership activates when live seeders and active drones are gone.
- Activating mothership saves a checkpoint, saves game state, submits highscore,
  and requests the save-confirmation voice.
- Victory happens when no live progression enemies remain.

Follow `Scene1Director` or `Scene6Director` for campaign scenes. Keep phase
state private to the director and reset it in `Initialize` and `Dispose`.

## Registering A New Scene

When adding a new campaign scene:

1. Add the namespace import to `TheOmegaStrain.Game/SceneManagement/SceneHandler.cs`.
2. Add `new SceneN()` to the `scenes` list in the intended order.
3. Update the `DevStartSceneIndex` comment.
4. Check tutorial, outro, and simulation indexes if the list shifts.
5. Update tests that assert scene order or scene indexes.

Be careful: scene index is saved in local player state. Changing order can
affect existing saves and manual test setup.

## Overlays

Every gameplay scene should implement both overlays:

- `SetupSceneOverlay()` for the briefing before the scene.
- `SetupGameOverlay()` for the HUD/game overlay state.

Briefing text must match the actual scene tuning: enemy counts, infection
threshold, spread delay, and special threats. Update
`SceneInfectionTuningTests` when those values change.

Use existing overlay state patterns:

```csharp
GameState.ScreenOverlayState.ResetToDefaults();
var o = GameState.ScreenOverlayState;
o.Type = ScreenOverlayType.Intro;
o.Anchor = ScreenOverlayAnchor.Top;
o.ShowOverlay = true;
o.AutoHide = false;
o.ShowDebugOverlay = false;
```

## Biome Rules

Pick the closest biome scene before adding content:

- Hills/woods: `Scene1`, `Scene2`
- Rainforest: `Scene3`, `Scene5`, `Scene8`
- Winter: `Scene4`, `Scene7`
- Desert: `Scene6`
- Mixed generated run: `SceneSimulation`

Biome-specific content should follow existing objects and emitters:

- Hills/woods: trees, leaf trees, leaf emitter, towers, houses, fish/swans.
- Rainforest: rainforest palette, rain/lightning, bamboo/palms.
- Winter: snow emitter, snow towers, igloos, polar bears, seals.
- Desert: sand emitter, cactuses, desert towers, tents, lakes/oasis plants.

Always use platform exclusion helpers for land-based objects. Normal objects
should not spawn on the landing platform.

## Tests

Update or add focused tests when scene behavior changes:

- Scene order/indexing: `TheOmegaStrain.Tests/SceneManagement/SceneHandlerTests.cs`
- Manual scene selection: `TheOmegaStrain.Tests/SceneManagement/SceneHandlerManualSceneSelectionTests.cs`
- Saved-state isolation: `TheOmegaStrain.Tests/SceneManagement/SceneHandlerSavedStateIsolationTests.cs`
- Infection tuning and briefing text: `TheOmegaStrain.Tests/SceneManagement/SceneInfectionTuningTests.cs`
- Tutorial-specific scene behavior: `TheOmegaStrain.Tests/SceneManagement/TutorialSceneTests.cs`
- Biome assets: `TheOmegaStrain.Tests/SceneManagement/Scene3RainforestWeatherTests.cs`,
  `TheOmegaStrain.Tests/SceneManagement/Scene4BearSpawningTests.cs`,
  `TheOmegaStrain.Tests/SceneManagement/Scene8RainforestAssetTests.cs`,
  `TheOmegaStrain.Tests/SceneManagement/WinterSceneWildlifeTests.cs`,
  `TheOmegaStrain.Tests/SceneManagement/SceneSimulationTests.cs`
- Placement: `TheOmegaStrain.Tests/SceneManagement/SeederPlacementHelpersTests.cs`

Useful commands:

```powershell
dotnet test .\TheOmegaStrain.Tests\TheOmegaStrain.Tests.csproj --no-restore --filter FullyQualifiedName~SceneManagement
dotnet test .\TheOmegaStrain.Tests\TheOmegaStrain.Tests.csproj --no-restore --filter FullyQualifiedName~SceneInfectionTuning
dotnet test .\TheOmegaStrain.Tests\TheOmegaStrain.Tests.csproj --no-restore --filter FullyQualifiedName~SeederPlacement
```

Do not claim tests passed unless they were actually run.

## Finish Checklist

- The closest existing scene pattern was inspected.
- New scene implements `IScene`.
- `SceneHandler` registration and scene index comments are updated.
- Ship, weapons, guidance arrow, surface viewport, and scene overlay exist.
- Progression enemies are in `SurfaceState.AiObjects`.
- Director handles activation/victory/checkpoint behavior when needed.
- Tuning values and briefing text agree.
- Platform exclusion rules are respected.
- Debug scene overrides, forced user state, and temporary test objects are
  removed.
- Relevant tests were added or updated.
