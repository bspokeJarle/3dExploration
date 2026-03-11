# Copilot Instructions

## Project Guidelines
- During playback runs, include logging of the needed playback data for validation. Logging must be gated by both the local `enableLogging` flag and the global `Logger.EnableFileLogging`; do not auto-enable global logging from local code.