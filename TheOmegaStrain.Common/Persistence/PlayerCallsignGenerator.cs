using System;
using System.Collections.Generic;
using TheOmegaStrain.Common.CommonGlobalState.States;

namespace TheOmegaStrain.Common.Persistence
{
    public static class PlayerCallsignGenerator
    {
        // Every word is kept at 7 characters or less so that any
        // "PREFIX NAME" combination fits inside ScreenOverlayState.MaxCallsignLength (16).
        internal static readonly string[] Prefixes =
        {
            "AGILE", "ANGRY", "ASTRO", "ATOMIC", "BRAVE", "CHROME", "COSMIC", "CRIMSON",
            "CYBER", "DIESEL", "FROST", "FUZZY", "GAMMA", "GRUMPY", "HYPER", "IRON",
            "JOLLY", "LASER", "LLAMA", "LUCKY", "LUNAR", "MAGNET", "MEGA", "NEON",
            "NOBLE", "NUCLEAR", "ORBIT", "PIXEL", "PLASMA", "PROTON", "QUANTUM", "QUICK",
            "RAPID", "RETRO", "ROGUE", "RUSTY", "SILENT", "SOLAR", "SONIC", "STELLAR",
            "STORM", "TITAN", "TURBO", "VELVET", "VIVID", "VOLTAIC", "WICKED", "ZESTY"
        };

        internal static readonly string[] Names =
        {
            "ASTER", "ATLAS", "BADGER", "BRABEN", "CACTUS", "COMET", "CRATER", "DRAGON",
            "FALCON", "GECKO", "HALLEY", "HORNET", "IBEX", "JUNO", "KEPLER", "KESTREL",
            "LYNX", "MAGPIE", "MIRA", "NEBULA", "NOVA", "NYX", "ONYX", "ORION",
            "OTTER", "PHOENIX", "PULSAR", "QUASAR", "RAVEN", "RHEA", "SAGAN", "SPUTNIK",
            "TALON", "TESLA", "THORN", "TUNDRA", "VEGA", "VIKTOR", "VIPER", "VOSS",
            "WALRUS", "WOMBAT", "WREN", "YETI", "ZEBRA", "ZED", "ZENITH", "ZODIAC"
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
