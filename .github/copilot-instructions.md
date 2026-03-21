# Copilot Instructions

## Project Guidelines
- During playback runs, include logging of the needed playback data for validation. Logging must be gated by both the local `enableLogging` flag and the global `Logger.EnableFileLogging`; do not auto-enable global logging from local code.
- When logging `Vector3` coordinates in this project, use a clearer format such as labeled components or semicolon-separated values instead of a comma as the coordinate delimiter.
- For enemy movement, surface synchronization should be a shared reusable pattern for all enemies, not implemented ad hoc per enemy control.
- For collision-sensitive enemy navigation, measure and navigate based on the ship center versus drone center, preferably using crash-box centers rather than synthetic origin points.
- For ship crash box sizing in this project, size multipliers do not need to be per-axis; prefer a single scalar multiplier unless axis-specific tuning is necessary.
- For changes related to `KamikazeDroneControl.cs`, localize modifications to this file and avoid altering shared helpers used across the application unless absolutely necessary. User does not want speculative behavior-changing fixes applied to kamikaze collision logic without stronger validation first.
- Check for and use existing helpers or extension methods before introducing new helper methods or extensions.
- For movement in this project, some objects must store progress locally in their control class across frames; for kamikaze behavior, follow the same persistent-state pattern used by the seeder rather than assuming object world state alone persists.
- For collision logging, include only actual collisions by default; log skipped collisions only when explicitly enabled.