using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TheOmegaStrain.Common.Persistence
{
    /// <summary>
    /// Lightweight Supabase REST client for the highscores table.
    /// Uses the PostgREST API (no SDK dependency).
    ///
    /// Expected table schema (Supabase → SQL Editor):
    /// <code>
    /// CREATE TABLE highscores (
    ///     id          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    ///     player_name TEXT        NOT NULL,
    ///     score       BIGINT      NOT NULL,
    ///     wave_reached INT        NOT NULL DEFAULT 1,
    ///     total_kills  INT        NOT NULL DEFAULT 0,
    ///     total_shots_fired INT   NOT NULL DEFAULT 0,
    ///     total_deaths INT        NOT NULL DEFAULT 0,
    ///     accuracy     REAL       NOT NULL DEFAULT 0,
    ///     date_utc     TEXT       NOT NULL,
    ///     created_at   TIMESTAMPTZ NOT NULL DEFAULT now()
    /// );
    ///
    /// -- Allow anonymous inserts and reads (RLS)
    /// ALTER TABLE highscores ENABLE ROW LEVEL SECURITY;
    /// CREATE POLICY "Anyone can read"  ON highscores FOR SELECT USING (true);
    /// CREATE POLICY "Anyone can insert" ON highscores FOR INSERT WITH CHECK (true);
    /// CREATE POLICY "Anyone can update highscores" ON highscores FOR UPDATE USING (true) WITH CHECK (true);
    /// CREATE UNIQUE INDEX highscores_player_name_unique ON highscores (player_name);
    /// </code>
    /// </summary>
    public static class SupabaseHighscoreClient
    {
        private const int ExistingPlayerFetchLimit = 1000;

        private static readonly HttpClient DefaultHttp = CreateHttpClient();
        private static HttpClient Http = DefaultHttp;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        private static HttpClient CreateHttpClient() => new() { Timeout = TimeSpan.FromSeconds(8) };

        // -----------------------------------------------------------------
        // Push a single entry to Supabase
        // -----------------------------------------------------------------

        /// <summary>
        /// Inserts or updates a highscore entry in the remote table.
        /// Fire-and-forget safe — exceptions are swallowed.
        /// </summary>
        public static async Task PushEntryAsync(HighscoreEntry entry)
        {
            if (!PersistenceSetup.IsSupabaseConfigured) return;

            try
            {
                var normalizedName = PlayerNameFormatter.Normalize(entry.PlayerName);
                if (string.IsNullOrWhiteSpace(normalizedName)) return;

                var payload = CreatePayload(entry, normalizedName);
                var existingRows = await FetchRowsByPlayerNameAsync(normalizedName).ConfigureAwait(false);

                // A failed lookup must not fall back to blind insert; that was the duplicate source.
                if (existingRows == null) return;

                if (existingRows.Count > 0)
                {
                    var keeper = SelectBestRow(existingRows);
                    var merged = MergeRows(existingRows, payload, normalizedName);
                    await UpdateExistingRowAsync(keeper, merged, normalizedName).ConfigureAwait(false);
                    return;
                }

                await InsertRowAsync(payload).ConfigureAwait(false);
                // Silently accept any result — local file is the source of truth
            }
            catch
            {
                // Offline or error — local file already has the entry
            }
        }

        // -----------------------------------------------------------------
        // Fetch top scores from Supabase
        // -----------------------------------------------------------------

        /// <summary>
        /// Fetches the top N highscores from the remote table, ordered by score descending.
        /// Returns null on failure.
        /// </summary>
        public static async Task<List<HighscoreEntry>?> FetchTopScoresAsync(int limit = 100)
        {
            if (!PersistenceSetup.IsSupabaseConfigured) return null;

            try
            {
                var url = $"{PersistenceSetup.SupabaseUrl}/rest/v1/{PersistenceSetup.SupabaseTableName}" +
                          $"?select=player_name,score,wave_reached,total_kills,total_shots_fired,total_deaths,accuracy,date_utc" +
                          $"&order=score.desc&limit={limit}";

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                SetSupabaseHeaders(request);

                using var response = await Http.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return null;

                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return ParseHighscoreRows(body);
            }
            catch
            {
                return null;
            }
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private static void SetSupabaseHeaders(HttpRequestMessage request)
        {
            request.Headers.Add("apikey", PersistenceSetup.SupabaseAnonKey!);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", PersistenceSetup.SupabaseAnonKey!);
        }

        private static SupabaseHighscoreRow CreatePayload(HighscoreEntry entry, string normalizedName)
        {
            return new SupabaseHighscoreRow
            {
                PlayerName = normalizedName,
                Score = entry.Score,
                WaveReached = entry.WaveReached,
                TotalKills = entry.TotalKills,
                TotalShotsFired = entry.TotalShotsFired,
                TotalDeaths = entry.TotalDeaths,
                Accuracy = entry.Accuracy,
                DateUtc = entry.DateUtc
            };
        }

        private static async Task<List<SupabaseHighscoreRow>?> FetchRowsByPlayerNameAsync(string normalizedName)
        {
            var escapedName = Uri.EscapeDataString(normalizedName);
            var url = $"{PersistenceSetup.SupabaseUrl}/rest/v1/{PersistenceSetup.SupabaseTableName}" +
                      $"?select=id,player_name,score,wave_reached,total_kills,total_shots_fired,total_deaths,accuracy,date_utc" +
                      $"&player_name=ilike.{escapedName}&limit={ExistingPlayerFetchLimit}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            SetSupabaseHeaders(request);

            using var response = await Http.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;

            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var rows = JsonSerializer.Deserialize<List<SupabaseHighscoreRow>>(body, JsonOptions);
            return rows?
                .Where(row => string.Equals(
                    PlayerNameFormatter.Normalize(row.PlayerName),
                    normalizedName,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static async Task InsertRowAsync(SupabaseHighscoreRow row)
        {
            var url = $"{PersistenceSetup.SupabaseUrl}/rest/v1/{PersistenceSetup.SupabaseTableName}";
            var json = JsonSerializer.Serialize(row, JsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            SetSupabaseHeaders(request);
            request.Headers.Add("Prefer", "return=minimal");

            using var response = await Http.SendAsync(request).ConfigureAwait(false);
        }

        private static async Task UpdateExistingRowAsync(
            SupabaseHighscoreRow keeper,
            SupabaseHighscoreRow merged,
            string normalizedName)
        {
            string url = keeper.Id.HasValue
                ? $"{PersistenceSetup.SupabaseUrl}/rest/v1/{PersistenceSetup.SupabaseTableName}?id=eq.{keeper.Id.Value}"
                : $"{PersistenceSetup.SupabaseUrl}/rest/v1/{PersistenceSetup.SupabaseTableName}?player_name=ilike.{Uri.EscapeDataString(normalizedName)}";

            var json = JsonSerializer.Serialize(merged, JsonOptions);
            using var request = new HttpRequestMessage(HttpMethod.Patch, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            SetSupabaseHeaders(request);
            request.Headers.Add("Prefer", "return=minimal");

            using var response = await Http.SendAsync(request).ConfigureAwait(false);
        }

        private static SupabaseHighscoreRow MergeRows(
            List<SupabaseHighscoreRow> existingRows,
            SupabaseHighscoreRow incoming,
            string normalizedName)
        {
            var best = SelectBestRow(existingRows.Concat(new[] { incoming }));
            var merged = CloneForWrite(best, normalizedName);

            foreach (var row in existingRows.Append(incoming))
            {
                PreserveNonZeroStats(merged, row);
            }

            return merged;
        }

        private static SupabaseHighscoreRow SelectBestRow(IEnumerable<SupabaseHighscoreRow> rows)
        {
            return rows
                .OrderByDescending(row => row.Score)
                .ThenByDescending(row => ParseDate(row.DateUtc))
                .ThenBy(row => row.Id ?? long.MaxValue)
                .First();
        }

        private static SupabaseHighscoreRow CloneForWrite(SupabaseHighscoreRow source, string normalizedName)
        {
            return new SupabaseHighscoreRow
            {
                PlayerName = normalizedName,
                Score = source.Score,
                WaveReached = source.WaveReached,
                TotalKills = source.TotalKills,
                TotalShotsFired = source.TotalShotsFired,
                TotalDeaths = source.TotalDeaths,
                Accuracy = source.Accuracy,
                DateUtc = source.DateUtc
            };
        }

        private static DateTime ParseDate(string? dateUtc)
        {
            return DateTime.TryParse(dateUtc, out var parsed)
                ? parsed.ToUniversalTime()
                : DateTime.MinValue;
        }

        private static void PreserveNonZeroStats(SupabaseHighscoreRow target, SupabaseHighscoreRow fallback)
        {
            if (target.WaveReached <= 0 && fallback.WaveReached > 0)
                target.WaveReached = fallback.WaveReached;

            if (target.TotalKills <= 0 && fallback.TotalKills > 0)
                target.TotalKills = fallback.TotalKills;

            if (target.TotalShotsFired <= 0 && fallback.TotalShotsFired > 0)
                target.TotalShotsFired = fallback.TotalShotsFired;

            if (target.TotalDeaths <= 0 && fallback.TotalDeaths > 0)
                target.TotalDeaths = fallback.TotalDeaths;

            if (target.Accuracy <= 0f && fallback.Accuracy > 0f)
                target.Accuracy = fallback.Accuracy;
        }

        internal static List<HighscoreEntry>? ParseHighscoreRows(string body)
        {
            var rows = JsonSerializer.Deserialize<List<SupabaseHighscoreRow>>(body, JsonOptions);
            if (rows == null) return null;

            var entries = new List<HighscoreEntry>(rows.Count);
            foreach (var row in rows)
            {
                entries.Add(new HighscoreEntry
                {
                    PlayerName = PlayerNameFormatter.Normalize(row.PlayerName),
                    Score = row.Score,
                    WaveReached = row.WaveReached,
                    TotalKills = row.TotalKills,
                    TotalShotsFired = row.TotalShotsFired,
                    TotalDeaths = row.TotalDeaths,
                    Accuracy = row.Accuracy,
                    DateUtc = row.DateUtc ?? ""
                });
            }

            return entries;
        }

        internal static void UseHttpClientForTests(HttpClient http)
        {
            Http = http;
        }

        internal static void ResetHttpClientForTests()
        {
            Http = DefaultHttp;
        }

        /// <summary>
        /// Internal DTO matching the Supabase/PostgREST snake_case column names.
        /// </summary>
        private sealed class SupabaseHighscoreRow
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public long? Id { get; set; }
            public string? PlayerName { get; set; }
            public long Score { get; set; }
            public int WaveReached { get; set; }
            public int TotalKills { get; set; }
            public int TotalShotsFired { get; set; }
            public int TotalDeaths { get; set; }
            public float Accuracy { get; set; }
            public string? DateUtc { get; set; }
        }
    }
}
