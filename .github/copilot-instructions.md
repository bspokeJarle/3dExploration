# Copilot Instructions

## Project Guidelines
- Steam integration must not be bypassed or broken for production builds; local renderer testing should preserve normal Steam startup behavior.
- Refer to the RetroMesh Direct3D 11 work as a potential permanent renderer, not a test renderer, in commit messages and descriptions.

## Tile Resolution Guidelines
- When increasing surface tile resolution, preserve world-space sizes for features defined in tile counts (lakes, mountains, landing platforms) by scaling their tile-count parameters proportionally.