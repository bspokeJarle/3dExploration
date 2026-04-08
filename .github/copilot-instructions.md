# Copilot Instructions

## Project Guidelines
- During playback runs, include logging of the needed playback data for validation. Logging must be gated by both the local `enableLogging` flag and the global `Logger.EnableFileLogging`; do not auto-enable global logging from local code.
- When logging `Vector3` coordinates in this project, use a clearer format such as labeled components or semicolon-separated values instead of a comma as the coordinate delimiter.
- For enemy movement, surface synchronization should be a shared reusable pattern for all enemies, not implemented ad hoc per enemy control.
- For collision-sensitive enemy navigation, measure and navigate based on the ship center versus drone center, preferably using crash-box centers rather than synthetic origin points.
- For ship crash box sizing in this project, size multipliers do not need to be per-axis; prefer a single scalar multiplier unless axis-specific tuning is necessary.
- For changes related to `KamikazeDroneControl.cs`, localize modifications to this file and avoid altering shared helpers used across the application unless absolutely necessary. User does not want speculative behavior-changing fixes applied to kamikaze collision logic without stronger validation first.
- Check for and use existing helpers or extension methods before introducing new helper methods or extensions. Use existing helpers for measurement before introducing new ones.
- For movement in this project, some objects must store progress locally in their control class across frames; for kamikaze behavior, follow the same persistent-state pattern used by the seeder rather than assuming object world state alone persists.
- For collision logging, include only actual collisions by default; log skipped collisions only when explicitly enabled.
- Implement audio spatialization consistently for all moving sound-emitting objects in the project, not just kamikaze drones.
- Collision/crash detection logic must stay centralized in the `CrashDetection` class. Do not scatter collision triggers into individual object controls (like `KamikazeDroneControl`, `DecoyBeaconControl`, etc.). Keep detection in one place.
- When the scene GameMode is Playback, do not run surface generation; load the surface from a recording file. During loading, calculate `TotalBioTiles` since `SurfaceGeneration.ReturnPseudoRandomMap` (which normally counts them) is not called.
- If an object has an unnatural crash, check the size of the object (geometry/scale), not the offset or anything else.

## Control Class Pattern
- Control classes implement `IObjectMovement` and are assigned to objects via the `Movement` property.
- Base rotations are defined as constants and applied each frame in `MoveObject()` via `theObject.Rotation.x/y/z`.
- Visual spinning is achieved by incrementing a rotation field per frame (e.g., `Zrotation += BaseZRotationIncrementPerFrame`), NOT by using `RotateZMesh`/`RotateXMesh` on parts (that's only for sub-part animation like TowerControls tower head).
- Ground/surface sync: Use `SurfacePositionSyncHelpers.GetSurfaceSyncedObjectOffsets(theObject, _syncY, SyncFactorY)` with a lazily-initialized `_syncY` captured from the object's initial `ObjectOffsets.y`. SyncFactorY is typically 2.5f.
- Position sync back to `AiObjects`: Include a `SyncToOriginal()` method that copies `WorldPosition` and `ObjectOffsets` from the deep copy back to the matching object in `GameState.SurfaceState.AiObjects` (by ObjectId), with a `ReferenceEquals` early-out.
- `Dispose()` should reset sync state (`_syncInitialized`, `_syncY`) and null out coordinate references, not throw `NotImplementedException`.
- Objects spawned at runtime (like PowerUp from `CleanupExplodedObjects`) must have `Movement` assigned and be added to both `WorldInhabitants` and `GameState.SurfaceState.AiObjects`.

## Coordinate System and Rotation Conventions
- The project uses a custom 3D engine with `Vector3` having x, y, z fields.
- X axis: lateral (left/right on screen)
- Y axis: depth/forward (into the screen, -Y is forward for weapons/projectiles)
- Z axis: vertical (up/down on screen, +Z is up; note that for `ObjectOffsets.y` and `WorldPosition.y`, + is down (lower on screen), - is up (higher on screen / higher altitude))
- Base rotation for objects facing the camera: X=70 (camera tilt), Y=0, Z=90
- Rotation around Z axis controls yaw/heading in the screen plane (turning left/right)
- Rotation around X axis controls pitch (tilting forward/back relative to camera)
- The arrow geometry points in +X direction by default (right on screen)
- On-screen objects (ship, guidance arrow) have `WorldPosition=(0,0,0)` and use `ObjectOffsets` for screen placement.
- World objects (seeders, drones) have actual `WorldPosition` values in world space.
- Surface-based objects use `SurfaceBasedId` and don't need a `WorldPosition`.
- `ObjectOffsets` control the on-screen visual position of objects.
- Investigate and correct the heading formula `atan2(dz, dx)` for direction = target - source in world coordinates, as it gives the correct heading for both on-screen and world objects. Use `TargetZrotation = headingDeg`, `TargetXrotation = 70f`, and `TargetYrotation = 0f`. Avoid using crash-center positions to prevent zoom/offset bias in the direction vector.

## Weapon Key Bindings
- Use the following key bindings for weapons:
  - 1 = Bullet (better for Seeders)
  - 2 = Decoy
  - 3 = Lazer
  - 4 = Rocket (future)

## Code Comments
- Keep code comments in English only; remove non-English comments when touching code.

## Animation Guidelines
- For the decoy, animate the wheel part by rotating it around its own center, using the tower part-rotation pattern as a reference.