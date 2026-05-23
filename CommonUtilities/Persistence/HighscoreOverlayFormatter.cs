using System.Linq;
using System.Text;

namespace CommonUtilities.Persistence
{
    public static class HighscoreOverlayFormatter
    {
        public static string BuildBody(int count = 25)
        {
            var list = HighscoreService.LoadLocalHighscores();
            var entries = list.Entries
                .OrderByDescending(e => e.Score)
                .Take(count)
                .ToList();

            if (entries.Count == 0)
                return "No highscores recorded yet.\n\nBe the first pilot to make history!";

            var sb = new StringBuilder();
            sb.AppendLine("RANK  PILOT             SCORE      KILLS");
            sb.AppendLine("----  -----             -----      -----");

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                var name = e.PlayerName.Length > 16
                    ? e.PlayerName[..16]
                    : e.PlayerName.PadRight(16);
                sb.AppendLine($" {(i + 1),2}.  {name}  {e.Score,9}  {e.TotalKills,5}");
            }

            return sb.ToString().TrimEnd();
        }
    }
}
