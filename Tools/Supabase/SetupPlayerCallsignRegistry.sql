-- Release-required online callsign registry for The Omega Strain.
-- Runtime still falls back to local/offline mode if the table or network is
-- unavailable, but public/beta releases should have this table configured.

CREATE TABLE IF NOT EXISTS public.player_callsigns (
    player_name TEXT PRIMARY KEY,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

INSERT INTO public.player_callsigns (player_name)
SELECT DISTINCT upper(trim(player_name))
FROM public.highscores
WHERE player_name IS NOT NULL
  AND trim(player_name) <> ''
ON CONFLICT (player_name) DO NOTHING;

ALTER TABLE public.player_callsigns ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Anyone can read player callsigns" ON public.player_callsigns;
DROP POLICY IF EXISTS "Anyone can insert player callsigns" ON public.player_callsigns;

CREATE POLICY "Anyone can read player callsigns"
ON public.player_callsigns
FOR SELECT
USING (true);

CREATE POLICY "Anyone can insert player callsigns"
ON public.player_callsigns
FOR INSERT
WITH CHECK (true);
