using System;
using System.Collections.Generic;
using TheOmegaStrain.Common.CommonGlobalState.States;

namespace TheOmegaStrain.Common.Persistence
{
    public static class PlayerCallsignGenerator
    {
        private static readonly string[] Prefixes =
        {
            "AGILE", "BRAVE", "COSMIC", "FROST", "IRON", "LLAMA", "LUCKY", "LUNAR",
            "NEON", "NOBLE", "ORBIT", "QUICK", "RAPID", "ROGUE", "SOLAR", "TURBO"
        };

        private static readonly string[] Names =
        {
            "ASTER", "ATLAS", "BRABEN", "HALLEY", "KEPLER", "MIRA", "NOVA", "NYX",
            "ORION", "RHEA", "SAGAN", "VEGA", "VIKTOR", "VOSS", "WREN", "ZED"
        };

        public static string CreateSuggestion(IReadOnlyCollection<string>? reservedCallsigns = null)
        {
            return CreateSuggestion(Random.Shared, reservedCallsigns);
        }

        internal static string CreateSuggestion(Random random, IReadOnlyCollection<string>? reservedCallsigns = null)
        {
            var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (reservedCallsigns != null)
            {
                foreach (var callsign in reservedCallsigns)
                {
                    var normalized = PlayerNameFormatter.Normalize(callsign);
                    if (!string.IsNullOrEmpty(normalized))
                        reserved.Add(normalized);
                }
            }

            for (int attempt = 0; attempt < 256; attempt++)
            {
                var candidate = $"{Prefixes[random.Next(Prefixes.Length)]} {Names[random.Next(Names.Length)]}";
                if (candidate.Length <= ScreenOverlayState.MaxCallsignLength && !reserved.Contains(candidate))
                    return candidate;
            }

            for (int suffix = 1; suffix <= 999; suffix++)
            {
                var candidate = $"OMEGA {suffix}";
                if (candidate.Length <= ScreenOverlayState.MaxCallsignLength && !reserved.Contains(candidate))
                    return candidate;
            }

            return "OMEGA PILOT";
        }
    }
}
