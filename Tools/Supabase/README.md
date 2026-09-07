# The Omega Strain Supabase Setup

Supabase is optional at runtime, but public builds should have both online
tables configured before Steam keys or beta access are distributed.

Required release tables:

- `highscores` stores the global leaderboard row per player.
- `player_callsigns` reserves player names globally before a new profile starts.

The client reads Supabase config from `%APPDATA%\OmegaStrain\secrets.json`
first, then from `online-services.json` or `secrets.json` beside
`TheOmegaStrain.exe`. Release packaging should copy the AppData file into
output/content as `online-services.json`. This file must contain only the
Supabase URL and anon/public API key, never a service role key.

For Steam content, generate the distributable config with:

```powershell
.\Tools\Supabase\PrepareOnlineServicesConfig.ps1 `
  -DestinationPath "C:\Users\JarleAdolfsen\Repositories\SteamWorksSdk_1.60\sdk\tools\ContentBuilder\content\online-services.json"
```

Run these in the Supabase SQL Editor:

1. `CleanupDuplicateHighscores.sql`
2. `SetupPlayerCallsignRegistry.sql`
3. `VerifyReleaseSupabaseSetup.sql`

The game client must still work if Supabase is offline or unavailable. That is
why the callsign registry fails back to local-only checks in runtime code, even
though the table is required for release readiness.
