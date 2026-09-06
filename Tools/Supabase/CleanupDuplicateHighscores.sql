-- Run once in the Supabase SQL Editor for The Omega Strain.
-- It normalizes player names, collapses duplicate highscore rows, and adds
-- a unique index so future submits keep one row per player.

BEGIN;

DELETE FROM public.highscores
WHERE player_name IS NULL
   OR trim(player_name) = '';

UPDATE public.highscores
SET player_name = upper(trim(player_name))
WHERE player_name <> upper(trim(player_name));

WITH ranked AS (
    SELECT
        id,
        upper(trim(player_name)) AS normalized_player_name,
        row_number() OVER (
            PARTITION BY upper(trim(player_name))
            ORDER BY score DESC, date_utc DESC NULLS LAST, id ASC
        ) AS row_number
    FROM public.highscores
),
merged AS (
    SELECT
        upper(trim(player_name)) AS normalized_player_name,
        max(score) AS score,
        max(wave_reached) AS wave_reached,
        max(total_kills) AS total_kills,
        max(total_shots_fired) AS total_shots_fired,
        max(total_deaths) AS total_deaths,
        max(accuracy) AS accuracy,
        coalesce(max(nullif(date_utc, '')), '') AS date_utc
    FROM public.highscores
    GROUP BY upper(trim(player_name))
),
keepers AS (
    SELECT
        ranked.id,
        merged.normalized_player_name,
        merged.score,
        merged.wave_reached,
        merged.total_kills,
        merged.total_shots_fired,
        merged.total_deaths,
        merged.accuracy,
        merged.date_utc
    FROM ranked
    JOIN merged
        ON merged.normalized_player_name = ranked.normalized_player_name
    WHERE ranked.row_number = 1
)
UPDATE public.highscores AS highscores
SET
    player_name = keepers.normalized_player_name,
    score = keepers.score,
    wave_reached = keepers.wave_reached,
    total_kills = keepers.total_kills,
    total_shots_fired = keepers.total_shots_fired,
    total_deaths = keepers.total_deaths,
    accuracy = keepers.accuracy,
    date_utc = keepers.date_utc
FROM keepers
WHERE highscores.id = keepers.id;

WITH ranked AS (
    SELECT
        id,
        row_number() OVER (
            PARTITION BY player_name
            ORDER BY score DESC, date_utc DESC NULLS LAST, id ASC
        ) AS row_number
    FROM public.highscores
)
DELETE FROM public.highscores AS highscores
USING ranked
WHERE highscores.id = ranked.id
  AND ranked.row_number > 1;

CREATE UNIQUE INDEX IF NOT EXISTS highscores_player_name_unique
ON public.highscores (player_name);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_policies
        WHERE schemaname = 'public'
          AND tablename = 'highscores'
          AND policyname = 'Anyone can update highscores'
    ) THEN
        CREATE POLICY "Anyone can update highscores"
        ON public.highscores
        FOR UPDATE
        USING (true)
        WITH CHECK (true);
    END IF;
END $$;

COMMIT;
