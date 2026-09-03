# Copilot Instructions

## General Guidelines
- Commit frequently during workspace changes to provide safe rollback points and prevent losing a working state.
- Do not alter base startup or other unrelated functionality for a focused change without verifying that the change is required; preserve existing behavior by default.
- Never alter base startup or intro sequence when selecting scenes; use only the established scene-selection mechanism and verify the intro remains first.
- Use the existing specific-scene activation mechanism (GamePlayState.SceneIndex/startup override) rather than changing base startup flow when selecting a scene for testing.

## Project Guidelines
- Steam integration must not be bypassed or broken for production builds; local renderer testing should preserve normal Steam startup behavior.
- When activating a scene for testing, preserve the intro flow and avoid changing unrelated startup behavior; only set the active scene.
- Refer to the RetroMesh Direct3D 11 work as a potential permanent renderer, not a test renderer, in commit messages and descriptions.

## Tile Resolution Guidelines
- When increasing surface tile resolution, preserve world-space sizes for features defined in tile counts (lakes, mountains, landing platforms) by scaling their tile-count parameters proportionally.