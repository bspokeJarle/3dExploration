-- Release checklist for The Omega Strain Supabase setup.
-- Run in Supabase SQL Editor before distributing beta/public Steam keys.
-- Every row should return OK.

WITH checks AS (
    SELECT
        'highscores table exists' AS check_name,
        to_regclass('public.highscores') IS NOT NULL AS ok
    UNION ALL
    SELECT
        'highscores.player_name exists',
        EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'highscores'
              AND column_name = 'player_name'
        )
    UNION ALL
    SELECT
        'highscores.score exists',
        EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'highscores'
              AND column_name = 'score'
        )
    UNION ALL
    SELECT
        'highscores unique player_name index exists',
        to_regclass('public.highscores_player_name_unique') IS NOT NULL
    UNION ALL
    SELECT
        'highscores RLS enabled',
        EXISTS (
            SELECT 1
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'public'
              AND c.relname = 'highscores'
              AND c.relrowsecurity
        )
    UNION ALL
    SELECT
        'highscores read policy exists',
        EXISTS (
            SELECT 1
            FROM pg_policies
            WHERE schemaname = 'public'
              AND tablename = 'highscores'
              AND policyname = 'Anyone can read'
        )
    UNION ALL
    SELECT
        'highscores insert policy exists',
        EXISTS (
            SELECT 1
            FROM pg_policies
            WHERE schemaname = 'public'
              AND tablename = 'highscores'
              AND policyname = 'Anyone can insert'
        )
    UNION ALL
    SELECT
        'highscores update policy exists',
        EXISTS (
            SELECT 1
            FROM pg_policies
            WHERE schemaname = 'public'
              AND tablename = 'highscores'
              AND policyname = 'Anyone can update highscores'
        )
    UNION ALL
    SELECT
        'player_callsigns table exists',
        to_regclass('public.player_callsigns') IS NOT NULL
    UNION ALL
    SELECT
        'player_callsigns.player_name exists',
        EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'player_callsigns'
              AND column_name = 'player_name'
        )
    UNION ALL
    SELECT
        'player_callsigns primary key exists',
        to_regclass('public.player_callsigns_pkey') IS NOT NULL
    UNION ALL
    SELECT
        'player_callsigns RLS enabled',
        EXISTS (
            SELECT 1
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'public'
              AND c.relname = 'player_callsigns'
              AND c.relrowsecurity
        )
    UNION ALL
    SELECT
        'player_callsigns read policy exists',
        EXISTS (
            SELECT 1
            FROM pg_policies
            WHERE schemaname = 'public'
              AND tablename = 'player_callsigns'
              AND policyname = 'Anyone can read player callsigns'
        )
    UNION ALL
    SELECT
        'player_callsigns insert policy exists',
        EXISTS (
            SELECT 1
            FROM pg_policies
            WHERE schemaname = 'public'
              AND tablename = 'player_callsigns'
              AND policyname = 'Anyone can insert player callsigns'
        )
)
SELECT
    check_name,
    CASE WHEN ok THEN 'OK' ELSE 'MISSING' END AS status
FROM checks
ORDER BY check_name;
