---
name: omega-add-game-object
description: "Use when adding or changing The Omega Strain world/scene objects, object factories, movement/control classes, AI behavior, placement, crash boxes, shadows, or object-specific tests. Do not use for unrelated UI, Steam, persistence, installer, or pure documentation work."
---

# Add Or Change An Omega Game Object

This skill keeps new objects aligned with The Omega Strain's existing patterns.
Before implementing, read `AGENTS.md` and the relevant sections of
`docs/AI_SOURCE_MAP.md`.

## First Files To Open

For almost every new object or control class, inspect these patterns first:

- `TheOmegaStrain.Domain/Interfaces/Objects/I3dObject.cs`
- `TheOmegaStrain.Domain/Interfaces/Movement/IObjectMovement.cs`
- `TheOmegaStrain.Game/World/GameWorld.cs`
- `TheOmegaStrain.Game/Scenes/Tutorial/TutorialScene.cs`
- `TheOmegaStrain.Game/World/Objects/KamikazeDrone.cs`
- `TheOmegaStrain.Gameplay/Controls/KamikazeDroneControls/KamikazeDroneControls.cs`
- `TheOmegaStrain.Gameplay/Controls/KamikazeDroneControls/KamikazeDroneAi.cs`
- `TheOmegaStrain.Game/Helpers/ObjectPlacementHelpers.cs`
- `TheOmegaStrain.Common/OmegaEngineAdapters/SurfacePositionSyncHelpers.cs`

If the new object is seeder-like or surface/AI-state heavy, also read:

- `TheOmegaStrain.Game/World/Objects/Seeder.cs`
- `TheOmegaStrain.Gameplay/Controls/SeederControls/SeederControls.cs`
- `TheOmegaStrain.Gameplay/Controls/SeederControls/SeederAi.cs`
- `TheOmegaStrain.Gameplay/Controls/TutorialSeederControls.cs`

If the object explodes, collides, casts a shadow, or uses surface anchoring,
also read:

- `TheOmegaStrain.Runtime/CrashDetection/CrashDetection.Main.cs`
- `TheOmegaStrain.Runtime/CrashDetection/CrashDetection.Collision.cs`
- `TheOmegaStrain.Game/Helpers/OmegaObject3DHelpers.cs`
- `TheOmegaStrain.Common/OmegaEngineAdapters/SurfaceGroundProjectionHelpers.cs`

## Object Factory Pattern

Prefer a factory method on the object type, similar to
`KamikazeDrone.CreateKamikazeDrone(...)`.

The factory should usually own:

- Object parts and visible low-poly geometry.
- Hidden guide parts only when an existing movement/rendering pattern needs
  them.
- Object name and type-specific defaults.
- Movement controller assignment.
- Particle/shadow setup when the object should have those.
- Crash box generation.
- Surface parent assignment for surface-based objects.
- Scale using existing helpers, not scattered manual multiplication.

Use existing geometry helpers and right-hand-rule helpers. If a single face is
intentionally visible from both sides, handle hidden-face behavior locally for
that face instead of changing global rendering rules.

## Scene Placement Pattern

Scene insertion should be explicit and boring:

```csharp
var enemy = NewEnemy.Create(parentSurface);
enemy.WorldPosition = new WorldPosition { X = x, Y = y, Z = z };
enemy.ObjectOffsets = new ObjectOffsets { X = ox, Y = oy, Z = oz };
enemy.Movement = new NewEnemyControls();
enemy.ImpactStatus = new ImpactStatus();
enemy.IsActive = true;

world.WorldInhabitants.Add(enemy);
GameState.SurfaceState.AiObjects.Add(enemy);
```

Only add to `GameState.SurfaceState.AiObjects` when the existing scene/object
pattern does so. Some screen-space or one-off visual objects should only live in
`WorldInhabitants`.

Use existing placement helpers for:

- Platform exclusion.
- Minimum distance between objects.
- Rings or spread around the platform.
- Keeping important objects near the platform.
- Biome-specific placement constraints.

Do not place normal world objects on the platform unless the user explicitly
asks for a temporary test object.

## Movement Controller Pattern

Movement controllers implement:

```csharp
void MoveObject(I3dObject object3d, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry);
```

Keep object movement delta-time based:

```text
movementThisFrame = speedPerSecond * deltaSeconds
```

For pursuit-style behavior:

```text
direction       = target - current
distance        = |direction|
unitDirection   = direction / distance
speedPerSecond  = screenWidth / secondsPerScreen
moveDistance    = speedPerSecond * deltaSeconds
current        += unitDirection * moveDistance
```

Use existing vector helpers for distance, normalization, dot products, and
projection. Avoid per-frame constants that make behavior slower at 60 FPS than
90 FPS.

## AI State Pattern

For simple enemies, keep local controller state in the control class.

For many independent AI objects, follow the `SeederAi` pattern:

- Keep per-object state keyed by stable object identity.
- Desynchronize timing so many objects do not move or fire in lockstep.
- Separate target selection from movement math where practical.
- Sync authoritative AI state back to the object instance used by rendering and
  collision.
- Keep tutorial/training special behavior in tutorial-specific controls.

## Surface, Shadow, And Coordinate Rules

Remember the game coordinate system:

```text
smaller Y = up
larger Y  = down
```

Surface-based objects should be anchored through existing surface sync helpers.
Do not fix sinking objects by changing projection globally.

Flying objects should have shadows when similar objects have shadows. Use the
existing ground projection pattern rather than inventing a new shadow position
calculation.

When applying Omega's camera-angle surface-height correction to a flying object:

- Calculate the object's normal movement, descent, hover, or altitude-cycle Y first.
- Add the shared correction only to the main object's `ObjectOffsets.y`.
- The correction supplements behavior; it never replaces or controls base movement.
- Do not separately correct weapons, projectiles, particles, particle guides, or shadows.
  They already derive from the corrected main object, while shadows remain projected
  directly onto the surface.
- Apply the correction at most once per frame, after every base-Y contribution that
  should be preserved.

## Crash Boxes

Use existing crash-box helpers. Do not create unrelated manual collision math
unless there is no local pattern and the change is covered by a focused test.

If the visualized box looks wrong:

1. Inspect generated crash box corners.
2. Inspect transform/projection.
3. Inspect debug visualization.
4. Only then change collision behavior.

Keep crashbox debug off before finishing unless the user explicitly asks to
leave it enabled.

## Tests

Add or update focused tests near the behavior:

- New object factory or crash boxes: `TheOmegaStrain.Tests/WorldObjects` or
  `TheOmegaStrain.Tests/Physics`.
- Movement controller: `TheOmegaStrain.Tests/Controls`.
- Scene placement: `TheOmegaStrain.Tests/SceneManagement`.
- Surface anchoring/shadows: `TheOmegaStrain.Tests/OmegaEngineAdapters` or
  `TheOmegaStrain.Tests/Physics`.

Useful examples:

```powershell
dotnet test .\TheOmegaStrain.Tests\TheOmegaStrain.Tests.csproj --no-restore --filter FullyQualifiedName~KamikazeDrone
dotnet test .\TheOmegaStrain.Tests\TheOmegaStrain.Tests.csproj --no-restore --filter FullyQualifiedName~Seeder
dotnet test .\TheOmegaStrain.Tests\TheOmegaStrain.Tests.csproj --no-restore --filter FullyQualifiedName~Surface
dotnet test .\TheOmegaStrain.Tests\TheOmegaStrain.Tests.csproj --no-restore --filter FullyQualifiedName~Collision
```

Do not claim a test passed unless it was actually run.

## Finish Checklist

- Existing pattern was inspected and reused.
- Object factory owns geometry and defaults.
- Scene placement uses helpers and avoids the platform.
- Movement is delta-time based.
- Surface/shadow behavior uses existing helpers.
- Crash boxes are generated through existing helpers.
- Tests cover the behavior that changed.
- Temporary debug/test objects, forced scene state, and diagnostics are removed.
