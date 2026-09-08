---
name: omega-add-object-particles
description: "Use when adding, moving, or fixing exhaust/thruster particle effects on a The Omega Strain object: placing particle start/direction guide parts, wiring guides in LiveGameLoop, emitting from a movement controller, or debugging particles that appear inside the hull, in the wrong direction, or not at all. Do not use for explosion particles driven by ExplosionParticleHelpers, hit sparks, or unrelated rendering work."
---

# Add Particles To An Omega Object

This skill covers the guide-driven exhaust particle pattern used by `Ship`,
`AttackShip`, `Rocket`, and `KamikazeDrone`. Before implementing, read
`AGENTS.md` and the relevant sections of `docs/AI_SOURCE_MAP.md`.

Related skill: `omega-add-game-object` for the surrounding object factory,
scene placement, crash box, and movement controller patterns.

## First Files To Open

- `TheOmegaStrain.Gameplay/Controls/Particles.cs` (emission and simulation math)
- `TheOmegaStrain.Runtime/Loops/LiveGameLoop.cs` (`SetMovementGuides`)
- `TheOmegaStrain.Game/World/Objects/Rocket.cs` (simplest single-engine example)
- `TheOmegaStrain.Gameplay/Controls/WorkshopCheatcode/RocketControls.cs`
- `TheOmegaStrain.Game/World/Objects/AttackShip.cs` (dual-engine example)
- `TheOmegaStrain.Gameplay/Controls/WorkshopCheatcode/AttackShipControls.cs`
- `TheOmegaStrain.Game/World/Objects/KamikazeDrone.cs` (AI object example)

## How The System Actually Works

Three pieces must line up. If any one is wrong the plume looks broken.

1. **Two hidden guide triangles** on the object, in local model space.
2. **A case in `LiveGameLoop.SetMovementGuides`** that binds each guide part.
3. **A `ReleaseParticles` call** from the object's movement controller.

`Particles.ReleaseParticles` reduces each guide mesh to its centroid and derives
velocity from the difference:

```text
velocity = (startCentroid - guideCentroid) / life
```

`MoveParticles` then applies `position -= velocity`, so the net travel direction
is `guideCentroid - startCentroid`.

**Consequence:** the direction guide must sit *further along the exhaust
direction* than the start guide. Reversing the two makes the plume shoot
forward through the object.

Each velocity axis is clamped to +/-10, so exaggerating guide distance to get a
faster plume stops helping past a point. Use `ParticlesAI` tuning instead.

## Step 1: Determine The Model Axis And Exit Point

Read the factory geometry rather than assuming. Find the nose/tip coordinate and
the rear-most engine/nozzle coordinate.

For all current objects, local **+X is forward** and **-X is backward**, so
exhaust guides have coordinates more negative than the nozzle.

The **exit point** is the rear-most solid geometry, for example:

| Object | Exit point (local X) | Source |
| --- | --- | --- |
| `Rocket` | `-10.0` | `nozzle = V(-10f, 0, 0)` |
| `AttackShip` | `-54.2` | `EngineNozzleCapX` |
| `KamikazeDrone` | `-47.0` | `engineBackX` |

## Step 2: Place The Guides Outside The Hull

Both guides go **beyond the exit point**, meaning outside the rear of the model,
not inside the engine bay. This is the single most common mistake: guides placed
only a few units behind the engine sit inside the hull, and the plume is born
buried in the geometry and looks wrong or invisible.

Measured values currently in the codebase:

| Object | Model length | Start guide | Behind exit | Direction guide | Guide gap |
| --- | --- | --- | --- | --- | --- |
| `Rocket` | ~21 | `-13.0` | 3.0 | `-15.5` | 2.5 |
| `AttackShip` | ~73 | `-100.0` | 45.8 | `-114.0` | 14.0 |
| `KamikazeDrone` | ~87 | `-110.0` | 63.0 | `-126.0` | 16.0 |

Starting heuristic, then tune visually:

- **Start guide:** place it clear of the exit point, scaled to the model. The
  gap must exceed the visual particle radius (`SizeMultiplier` inflates this) or
  large particles still clip into the hull.
- **Direction guide:** roughly **15-20% of the model length** further back than
  the start guide. This gap only sets direction, not speed, so it does not need
  to be large.

Note the ratios above are not uniform: `Rocket` is a small, thin object and
needs far less absolute clearance than the bulky `AttackShip` and
`KamikazeDrone`. Scale to the model, then confirm in-game.

Keep both guides on the same Y/Z centre line as the nozzle. Offsetting them
tilts the plume away from the engine axis.

Build them as single hidden triangles and register them with `AddPart(..., false)`
so they never render:

```csharp
AddPart(drone, "KamikazeParticlesStartGuide", startGuide, false);
AddPart(drone, "KamikazeParticlesGuide", guide, false);
```

For multi-engine objects, add one start/direction pair per engine, mirrored on
the lateral axis, as `AttackShip` does.

## Step 3: Create The Particle Container With Styling

Set `Particles` in the object factory. A bare `new ParticlesAI()` uses default
gravity, which drags the plume under the hull on fast-moving objects:

```csharp
drone.Particles = new ParticlesAI
{
	GravityStrength = 34f,
	LifeMultiplier = 0.6f,
	SizeMultiplier = 1.25f,
	ThrottleDurationFactor = 0.2f,
	ColorStartOverride = "fff8c8",
	ColorMidOverride = "ff8a20",
	ColorEndOverride = "5a1800"
};
```

## Step 4: Bind The Guides In LiveGameLoop

Add a case per guide part in `LiveGameLoop.SetMovementGuides`. Objects with one
engine use only the primary pair:

```csharp
case "KamikazeParticlesStartGuide":
	inhabitant.Movement.SetParticleGuideCoordinates(rotatedMesh.First() as TriangleMeshWithColor, null);
	break;
case "KamikazeParticlesGuide":
	inhabitant.Movement.SetParticleGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
	break;
```

Objects with two engines use `SetParticleGuideCoordinates` for the first engine
and `SetRearEngineGuideCoordinates` for the second, as `AttackShip` does.

Part names in the factory and in `SetMovementGuides` must match exactly. A
silent no-op here is a common cause of "no particles at all".

## Step 5: Emit From The Movement Controller

**Critical frame-order rule:** `LiveGameLoop` runs `MoveObject` *before*
`SetMovementGuides`, so the bound `StartCoordinates` / `GuideCoordinates` always
describe the *previous* frame's orientation. Emitting from them makes the plume
lag visibly during turns.

Rotate the guides for the current frame instead:

```csharp
private ITriangleMeshWithColorAndTexture? GetCurrentFrameRotatedGuide(I3dObject theObject, string partName)
{
	var part = theObject.ObjectParts?.Find(p => p.PartName == partName);
	if (part?.Triangles == null || part.Triangles.Count == 0) return null;

	var rotation = theObject.Rotation ?? new Vector3();
	var mesh = new List<ITriangleMeshWithColorAndTexture>
	{
		OmegaObjectHelpers.CopyTriangle(part.Triangles[0])
	};

	mesh = _guideRotation.RotateZMesh(mesh, rotation.z);
	mesh = _guideRotation.RotateYMesh(mesh, rotation.y);
	mesh = _guideRotation.RotateXMesh(mesh, rotation.x);

	return mesh[0];
}
```

Apply the rotations in Z, Y, X order to match the engine's convention.

Then emit, throttled, and step the simulation:

```csharp
public void ReleaseParticles(I3dObject theObject)
{
	if (theObject.Particles == null || _isExploding) return;
	if (++_framesSinceParticleRelease < FramesBetweenParticleReleases) return;

	var start = GetCurrentFrameRotatedGuide(theObject, ParticleStartGuidePartName);
	var guide = GetCurrentFrameRotatedGuide(theObject, ParticleDirectionGuidePartName);
	if (start == null || guide == null) return;

	_framesSinceParticleRelease = 0;

	var worldPosition = new Vector3
	{
		x = theObject.WorldPosition?.x ?? 0f,
		y = theObject.WorldPosition?.y ?? 0f,
		z = theObject.WorldPosition?.z ?? 0f
	};

	theObject.Particles.ReleaseParticles(guide, start, worldPosition, this, ParticleThrust, null);
}
```

Note the argument order: **trajectory/direction guide first, start guide second**.

Call it near the end of `MoveObject`, after rotation is settled, and advance the
existing particles:

```csharp
ReleaseParticles(theObject);
if (theObject.Particles?.Particles.Count > 0)
	theObject.Particles.MoveParticles();
```

Emitting every 2nd frame with a thrust of about 3 gives a steady plume without
flooding the particle list. Skip emission while the object is exploding so
exhaust does not fight the explosion effect.

## Controller State Rule

`LiveGameLoop` deep-copies every inhabitant each frame, so anything written to
the frame copy is discarded. The controller instance is shared across frames.

Keep emission counters, accumulated rotation, and similar state as **fields on
the controller**, never on the object copy.

## Troubleshooting

| Symptom | Likely cause |
| --- | --- |
| No particles at all | Part name mismatch between factory and `SetMovementGuides`; `Particles` is null; guide part has no triangles |
| Plume born inside the hull | Start guide too close to the exit point, or `SizeMultiplier` inflated particles past the clearance |
| Plume shoots forward | Start and direction guides swapped, or direction guide is nearer the hull than the start guide |
| Plume lags behind during turns | Emitting from bound `StartCoordinates`/`GuideCoordinates` instead of current-frame rotated guides |
| Plume sags under the object | `GravityStrength` left at default; lower it as `AttackShip` does |
| Plume speed will not increase | Velocity is clamped to +/-10 per axis; tune `ParticlesAI` instead of widening the guide gap |

## Tests

Particle behavior tests live in `TheOmegaStrain.Tests/Controls`. Follow the
existing `ExplosionParticleEffectsTests` pattern.

```powershell
dotnet test .\TheOmegaStrain.Tests\TheOmegaStrain.Tests.csproj --no-restore --filter FullyQualifiedName~Particle
```

Do not claim a test passed unless it was actually run. Guide placement is a
visual property, so confirm the final look in-game as well.

## Finish Checklist

- Model axis and exit point were read from the factory, not assumed.
- Both guides sit outside the rear of the hull, scaled to the model size.
- Direction guide is further along the exhaust direction than the start guide.
- Guides share the nozzle's Y/Z centre line.
- Guide parts are registered hidden.
- `ParticlesAI` gravity and colours are tuned, not left at defaults.
- `SetMovementGuides` cases exist and part names match exactly.
- Controller emits from current-frame rotated guides.
- Emission counters live on the controller, not the deep-copied object.
- `MoveParticles` is called each frame.
- Temporary particle logging and debug guide rendering are removed.
