using Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CommonUtilities.Persistence
{
    /// <summary>
    /// Manages highscores with a local-first strategy:
    /// • All reads/writes go to an encrypted local file immediately (offline-safe).
    /// • On submit, the entry is pushed to Supabase only if it qualifies for the top list.
    /// • Remote fetches are throttled by a cooldown to minimise API usage.
    /// </summary>
    public static class HighscoreService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Timestamp of last successful remote fetch — used for cooldown
        private static DateTime _lastRemoteFetch = DateTime.MinValue;

        // -----------------------------------------------------------------
        // Local file operations
        // -----------------------------------------------------------------

        /// <summary>
        /// Loads the highscore list from the local encrypted file.
        /// Returns an empty list if the file does not exist or cannot be read.
        /// </summary>
        public static HighscoreList LoadLocalHighscores()
        {
            try
            {
                var path = PersistenceSetup.LocalHighscoreFilePath;
                var keyPath = PersistenceSetup.LocalKeyFilePath;

                if (!File.Exists(path)) return new HighscoreList();

                var json = EncryptionHelper.DecryptFromFile(path, keyPath);
                if (json == null) return new HighscoreList();

                return JsonSerializer.Deserialize<HighscoreList>(json, JsonOptions)
                    ?? new HighscoreList();
            }
            catch
            {
                return new HighscoreList();
            }
        }

        /// <summary>
        /// Writes the highscore list to the local encrypted file.
        /// </summary>
        public static void SaveLocalHighscores(HighscoreList list)
        {
            var path = PersistenceSetup.LocalHighscoreFilePath;
            var keyPath = PersistenceSetup.LocalKeyFilePath;

            var dir = Path.GetDirectoryName(path);
            if (dir != null) Directory.CreateDirectory(dir);

            EncryptionHelper.EnsureKeyFile(keyPath);

            var json = JsonSerializer.Serialize(list, JsonOptions);
            EncryptionHelper.EncryptToFile(path, json, keyPath);
        }

        // -----------------------------------------------------------------
        // Submit score (local + remote push)
        // -----------------------------------------------------------------

        /// <summary>
        /// Convenience overload: extracts all scoring fields from the current
        /// <see cref="GamePlayState"/> and submits them in one call.
        /// </summary>
        public static bool SubmitFromGamePlay(GamePlayState gps)
        {
            return TrySubmitScore(
                gps.PlayerName,
                gps.Score,
                gps.SceneIndex,
                gps.TotalKills,
                gps.TotalShotsFired,
                gps.TotalDeaths,
                gps.Accuracy);
        }

        /// <summary>
        /// Submits a score: saves to local file immediately, then pushes to
        /// Supabase in the background only if the score qualified for the
        /// local top list. Returns true when it qualified.
        /// </summary>
        public static bool TrySubmitScore(
            string playerName,
            long score,
            int waveReached,
            int totalKills = 0,
            int totalShotsFired = 0,
            int totalDeaths = 0,
            float accuracy = 0f)
        {
            var entry = new HighscoreEntry
            {
                PlayerName = playerName,
                Score = score,
                WaveReached = waveReached,
                TotalKills = totalKills,
                TotalShotsFired = totalShotsFired,
                TotalDeaths = totalDeaths,
                Accuracy = accuracy,
                DateUtc = DateTime.UtcNow.ToString("o")
            };

            bool qualified = InsertIntoLocalList(entry);

            // Only push to Supabase when the score actually made the top list
            if (qualified && PersistenceSetup.IsSupabaseConfigured)
            {
                _ = SupabaseHighscoreClient.PushEntryAsync(entry);
            }

            return qualified;
        }

        /// <summary>
        /// Inserts or updates an entry in the local list, sorts, trims to max, and saves.
        /// If the player already has an entry, it is updated only when the new score is higher.
        /// </summary>
        private static bool InsertIntoLocalList(HighscoreEntry entry)
        {
            try
            {
                var list = LoadLocalHighscores();
                int max = PersistenceSetup.MaxHighscoreEntries;

                var existing = list.Entries.Find(e =>
                    string.Equals(e.PlayerName, entry.PlayerName, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    if (entry.Score <= existing.Score)
                        return false;

                    existing.Score = entry.Score;
                    existing.WaveReached = entry.WaveReached;
                    existing.TotalKills = entry.TotalKills;
                    existing.TotalShotsFired = entry.TotalShotsFired;
                    existing.TotalDeaths = entry.TotalDeaths;
                    existing.Accuracy = entry.Accuracy;
                    existing.DateUtc = entry.DateUtc;
                }
                else
                {
                    if (list.Entries.Count >= max && entry.Score <= list.Entries[^1].Score)
                        return false;

                    list.Entries.Add(entry);
                }

                list.Entries.Sort((a, b) => b.Score.CompareTo(a.Score));

                if (list.Entries.Count > max)
                    list.Entries.RemoveRange(max, list.Entries.Count - max);

                SaveLocalHighscores(list);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // -----------------------------------------------------------------
        // View highscores (fetch remote → merge → return)
        // -----------------------------------------------------------------

        /// <summary>
        /// Returns the top N highscores. Fetches from Supabase only when
        /// the cooldown has elapsed (default 10 min), otherwise returns
        /// the locally cached list. Merges and saves when a remote fetch
        /// occurs.
        /// </summary>
        public static List<HighscoreEntry> GetTopScores(int count = 10)
        {
            var local = LoadLocalHighscores();

            bool cooldownElapsed = (DateTime.UtcNow - _lastRemoteFetch)
                                    >= PersistenceSetup.RemoteFetchCooldown;

            if (PersistenceSetup.IsSupabaseConfigured && cooldownElapsed)
            {
                try
                {
                    var remote = SupabaseHighscoreClient.FetchTopScoresAsync(
                        PersistenceSetup.MaxHighscoreEntries).Result;

                    if (remote != null && remote.Count > 0)
                    {
                        var merged = MergeLists(local.Entries, remote);
                        local.Entries = merged;
                        SaveLocalHighscores(local);
                    }

                    _lastRemoteFetch = DateTime.UtcNow;
                }
                catch
                {
                    // Offline — use local cache
                }
            }

            return local.Entries.Take(count).ToList();
        }

        /// <summary>
        /// Checks whether the given score would qualify for the local highscore list.
        /// </summary>
        public static bool IsHighscore(long score)
        {
            var list = LoadLocalHighscores();
            if (list.Entries.Count < PersistenceSetup.MaxHighscoreEntries) return true;
            return score > list.Entries[^1].Score;
        }

        // -----------------------------------------------------------------
        // Merge helper
        // -----------------------------------------------------------------

        /// <summary>
        /// Merges two lists, de-duplicates by (PlayerName + DateUtc),
        /// sorts descending by score, and trims to max entries.
        /// </summary>
        private static List<HighscoreEntry> MergeLists(
            List<HighscoreEntry> listA,
            List<HighscoreEntry> listB)
        {
            var seen = new HashSet<string>();
            var merged = new List<HighscoreEntry>();

            foreach (var entry in listA.Concat(listB))
            {
                var key = $"{entry.PlayerName}|{entry.DateUtc}";
                if (seen.Add(key))
                    merged.Add(entry);
            }

            merged.Sort((a, b) => b.Score.CompareTo(a.Score));

            int max = PersistenceSetup.MaxHighscoreEntries;
            if (merged.Count > max)
                merged.RemoveRange(max, merged.Count - max);

            return merged;
        }
    }
}
