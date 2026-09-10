using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheOmegaStrain.Domain;

namespace TheOmegaStrain.Common.Persistence
{
    public sealed class CallsignConfirmationResult
    {
        public bool IsAccepted { get; init; }
        public string Callsign { get; init; } = "";
        public string ValidationMessage { get; init; } = "";
    }

    public static class PlayerCallsignService
    {
        public const string EmptyCallsignMessage = ">> CALLSIGN CANNOT BE EMPTY";
        public const string LocalCallsignTakenMessage = ">> CALLSIGN ALREADY IN USE - USE RIGHT FOR NEW";
        public const string RemoteCallsignTakenMessage = ">> CALLSIGN RESERVED ONLINE - USE RIGHT FOR NEW";
        public const string NumberedCallsignSuggestedMessage = ">> CALLSIGN TAKEN - NUMBERED SUGGESTION READY";

        public static string CreateSuggestedCallsign(string currentCallsign = "")
        {
            var reserved = LoadReservedLocalCallsigns();
            var current = PlayerNameFormatter.Normalize(currentCallsign);
            if (!string.IsNullOrEmpty(current))
                reserved.Add(current);

            return PlayerCallsignGenerator.CreateSuggestion(reserved);
        }

        public static string CreateNumberedSuggestedCallsign(string currentCallsign)
        {
            return PlayerCallsignGenerator.CreateNumberedSuggestion(
                currentCallsign,
                Random.Shared,
                LoadReservedLocalCallsigns());
        }

        public static IReadOnlyList<string> LoadLocalProfileCallsigns()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var lastPlayer = PersistenceSetup.LoadLastPlayerName();

            AddName(names, lastPlayer);
            AddLocalProfileFileNames(names);

            var ordered = names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
            var normalizedLastPlayer = PlayerNameFormatter.Normalize(lastPlayer);
            if (!string.IsNullOrEmpty(normalizedLastPlayer) && ordered.Remove(normalizedLastPlayer))
                ordered.Insert(0, normalizedLastPlayer);

            return ordered;
        }

        public static string SelectLocalProfileCallsign(string currentCallsign, int direction)
        {
            var profiles = LoadLocalProfileCallsigns();
            if (profiles.Count == 0)
                return "";

            var normalizedCurrent = PlayerNameFormatter.Normalize(currentCallsign);
            if (string.IsNullOrEmpty(normalizedCurrent))
                return profiles[0];

            var index = -1;
            for (int i = 0; i < profiles.Count; i++)
            {
                if (string.Equals(profiles[i], normalizedCurrent, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
                return profiles[0];

            var step = direction < 0 ? -1 : 1;
            var nextIndex = (index + step + profiles.Count) % profiles.Count;
            return profiles[nextIndex];
        }

        public static CallsignConfirmationResult TryConfirmCallsign(string callsign, string priorName = "")
        {
            var normalized = PlayerNameFormatter.Normalize(callsign);
            if (string.IsNullOrWhiteSpace(normalized))
                return Reject(EmptyCallsignMessage);

            bool isOwnName = IsOwnLocalCallsign(normalized, priorName);
            if (!isOwnName && IsLocalCallsignTaken(normalized))
                return Reject(LocalCallsignTakenMessage);

            if (!isOwnName)
            {
                var remoteStatus = SupabaseCallsignClient.TryReserveAsync(normalized)
                    .GetAwaiter()
                    .GetResult();

                if (remoteStatus == SupabaseCallsignReservationStatus.Taken)
                    return Reject(RemoteCallsignTakenMessage);
            }

            return new CallsignConfirmationResult
            {
                IsAccepted = true,
                Callsign = normalized
            };
        }

        private static CallsignConfirmationResult Reject(string message)
        {
            return new CallsignConfirmationResult
            {
                IsAccepted = false,
                ValidationMessage = message
            };
        }

        private static bool IsOwnLocalCallsign(string callsign, string priorName)
        {
            var normalizedPriorName = PlayerNameFormatter.Normalize(priorName);
            return string.Equals(callsign, normalizedPriorName, StringComparison.OrdinalIgnoreCase)
                   || HasAnyLocalProfileFile(callsign);
        }

        private static bool HasAnyLocalProfileFile(string callsign)
        {
            return PersistenceSetup.HasPlayerSaveFile(callsign)
                   || File.Exists(PersistenceSetup.GetPlayerProgressFilePath(callsign))
                   || File.Exists(PersistenceSetup.GetPlayerProgressBackupFilePath(callsign))
                   || File.Exists(PersistenceSetup.GetPlayerTutorialProgressFilePath(callsign));
        }

        private static bool IsLocalCallsignTaken(string callsign)
        {
            return LoadReservedLocalCallsigns().Contains(callsign);
        }

        internal static HashSet<string> LoadReservedLocalCallsigns()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddName(names, PersistenceSetup.LoadLastPlayerName());

            var highscores = HighscoreService.LoadLocalHighscores();
            foreach (var entry in highscores.Entries)
                AddName(names, entry.PlayerName);

            AddLocalProfileFileNames(names);
            return names;
        }

        private static void AddLocalProfileFileNames(HashSet<string> names)
        {
            try
            {
                if (!Directory.Exists(PersistenceSetup.LocalFolder))
                    return;

                AddNamesFromFiles(names, "save_", ".enc");
                AddNamesFromFiles(names, "progress_", ".enc");
                AddNamesFromFiles(names, "tutorial_", ".json");
            }
            catch (Exception ex)
            {
                // Never block callsign entry on a filesystem problem, but make it
                // diagnosable instead of silently returning an incomplete set.
                if (Logger.EnableFileLogging)
                    Logger.Log($"[Callsign] Failed to enumerate local profiles. {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static void AddNamesFromFiles(HashSet<string> names, string prefix, string suffix)
        {
            foreach (var filePath in Directory.EnumerateFiles(PersistenceSetup.LocalFolder, $"{prefix}*{suffix}"))
            {
                var fileName = Path.GetFileName(filePath);
                if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    !fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var raw = fileName[prefix.Length..^suffix.Length].Replace('_', ' ');
                AddName(names, raw);
            }
        }

        private static void AddName(HashSet<string> names, string? name)
        {
            var normalized = PlayerNameFormatter.Normalize(name);
            if (!string.IsNullOrEmpty(normalized))
                names.Add(normalized);
        }
    }
}
