using System;
using System.Collections.Generic;
using System.Linq;
using TheOmegaStrain.Common.CommonGlobalState.States;

namespace TheOmegaStrain.Common.Persistence
{
    public static class PlayerCallsignGenerator
    {
        internal static readonly string[] Prefixes =
        {
            "ABLE", "AGILE", "ALERT", "AMBER", "ANCIENT", "ANGRY", "ARCTIC", "ARDENT",
            "ASTRAL", "ASTRO", "ATOMIC", "AZURE", "BLAZING", "BOLD", "BRAVE", "BRIGHT",
            "BRISK", "BRONZE", "CALM", "CHARMED", "CHROME", "CLEVER", "COLD", "COSMIC",
            "CRIMSON", "CYBER", "DARING", "DARK", "DAUNTLESS", "DEEP", "DIESEL", "DIRE",
            "EAGER", "ELECTRIC", "ELITE", "EMBER", "EPIC", "FABLED", "FAST", "FEARLESS",
            "FERAL", "FIERY", "FIERCE", "FLYING", "FOCUSED", "FROST", "FROZEN", "FUZZY",
            "GALLANT", "GAMMA", "GIANT", "GILDED", "GOLDEN", "GRAND", "GRIM", "GRUMPY",
            "HAPPY", "HARDY", "HIDDEN", "HOLLOW", "HOT", "HYPER", "ICY", "IRON",
            "JADE", "JOLLY", "KEEN", "LASER", "LIGHT", "LIVELY", "LONE", "LUCKY",
            "LUNAR", "MAD", "MAGIC", "MAGNET", "MIGHTY", "MINT", "MISTY", "NEON",
            "NIMBLE", "NOBLE", "NUCLEAR", "OMEGA", "ORBITAL", "PALE", "PHANTOM", "PIXEL",
            "PLASMA", "POLAR", "PRIME", "PROUD", "PROTON", "PURPLE", "QUANTUM", "QUICK",
            "QUIET", "RADIANT", "RAGING", "RAPID", "RARE", "RED", "RETRO", "RISING",
            "ROYAL", "ROGUE", "RUSTY", "SAVAGE", "SCARLET", "SHARP", "SHINING", "SILENT",
            "SILVER", "SOLAR", "SONIC", "STEADY", "STELLAR", "STORMY", "STRONG", "SWIFT",
            "THUNDER", "TIDAL", "TITAN", "TOUGH", "TURBO", "ULTRA", "VALIANT", "VELVET",
            "VIVID", "VOLTAIC", "WILD", "WICKED", "WISE", "YOUNG", "ZANY", "ZESTY"
        };

        internal static readonly string[] Names =
        {
            "ARCHER", "ATLAS", "AVELLONE", "BADGER", "BARLOG", "BARONE", "BELL", "BENZIES",
            "BLESZINSKI", "BLOW", "BOON", "BOYARSKY", "BRABEN", "BUSHNELL", "CAIN", "CARMACK",
            "CACTUS", "COMET", "CROWE", "DRAGON", "DRUCKMANN", "FALCON", "FOX", "FREESE",
            "GAIDER", "GARROTT", "GECKO", "GILBERT", "HALL", "HALLEY", "HAWKINS", "HOLLIS",
            "HORII", "HORNET", "HOUZER", "HOWARD", "IBEX", "INAFUNE", "JONES", "JUNO",
            "KAMIYA", "KAPLAN", "KASAVIN", "KEPLER", "KESTREL", "KOJIMA", "LAIDLAW", "LEVINE",
            "LYNX", "MAGPIE", "MEIER", "MIKAMI", "MINTER", "MIYAMOTO", "MOLYNEUX", "NAKA",
            "NEBULA", "NEWELL", "NISHIKADO", "NOVA", "NYX", "ONYX", "ORION", "PAJITNOV",
            "PERSSON", "PHOENIX", "POPE", "PULSAR", "QUASAR", "RAVEN", "REYNOLDS", "RHEA",
            "ROMERO", "SAGAN", "SAKAGUCHI", "SAWYER", "SCHAFER", "SPECTOR", "SPUTNIK", "SUZUKI",
            "TALON", "TAYLOR", "TESLA", "THORN", "TOBIAS", "TORVALDS", "TUNDRA", "UEDA",
            "VEGA", "VIKTOR", "VIPER", "VOSS", "WALRUS", "WILLIAMS", "WOMBAT", "WRIGHT",
            "WREN", "YERLI", "YETI", "ZEBRA", "ZED", "ZENITH", "ZESCHUK", "ZODIAC"
        };

        private const int MaximumNumericDigits = 4;
        private static readonly string[] ValidCombinations = BuildValidCombinations();

        public static int UniqueCombinationCount => ValidCombinations.Length;

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

            int startIndex = random.Next(ValidCombinations.Length);
            for (int offset = 0; offset < ValidCombinations.Length; offset++)
            {
                string candidate = ValidCombinations[(startIndex + offset) % ValidCombinations.Length];
                if (!reserved.Contains(candidate))
                    return candidate;
            }

            return "OMEGA PILOT";
        }

        internal static string CreateNumberedSuggestion(
            string currentCallsign,
            Random random,
            IReadOnlyCollection<string>? reservedCallsigns = null)
        {
            var reserved = new HashSet<string>(reservedCallsigns ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            string normalizedCurrent = PlayerNameFormatter.Normalize(currentCallsign);
            reserved.Add(normalizedCurrent);
            string baseCallsign = RemoveNumericSuffix(normalizedCurrent);
            if (baseCallsign.Length + 2 > ScreenOverlayState.MaxCallsignLength)
            {
                string[] numberable = ValidCombinations
                    .Where(candidate => candidate.Length + 2 <= ScreenOverlayState.MaxCallsignLength)
                    .ToArray();
                baseCallsign = numberable[random.Next(numberable.Length)];
            }

            int digits = GetNumericDigitCount(baseCallsign);
            int first = digits == 1 ? 0 : Pow10(digits - 1);
            int lastExclusive = Pow10(digits);
            int start = TryGetNumericSuffix(normalizedCurrent, out int priorSuffix)
                ? first + ((priorSuffix - first + 1) % (lastExclusive - first))
                : random.Next(first, lastExclusive);
            int variantCount = lastExclusive - first;
            for (int offset = 0; offset < variantCount; offset++)
            {
                int suffix = first + ((start - first + offset) % variantCount);
                string candidate = $"{baseCallsign} {suffix}";
                if (!reserved.Contains(candidate))
                    return candidate;
            }

            return CreateSuggestion(random, reserved);
        }

        private static int GetNumericDigitCount(string baseCallsign) =>
            Math.Min(MaximumNumericDigits, ScreenOverlayState.MaxCallsignLength - baseCallsign.Length - 1);

        private static string RemoveNumericSuffix(string callsign)
        {
            int separator = callsign.LastIndexOf(' ');
            return separator > 0 && callsign[(separator + 1)..].All(char.IsDigit)
                ? callsign[..separator]
                : callsign;
        }

        private static bool TryGetNumericSuffix(string callsign, out int suffix)
        {
            suffix = 0;
            int separator = callsign.LastIndexOf(' ');
            return separator > 0 && int.TryParse(callsign[(separator + 1)..], out suffix);
        }

        private static int Pow10(int exponent)
        {
            int value = 1;
            for (int i = 0; i < exponent; i++)
                value *= 10;
            return value;
        }

        private static string[] BuildValidCombinations()
        {
            return Prefixes
                .SelectMany(prefix => Names.Select(name => $"{prefix} {name}"))
                .Where(candidate => candidate.Length <= ScreenOverlayState.MaxCallsignLength)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
