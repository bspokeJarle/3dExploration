using _3dRotations.World.Objects.LogoCube;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.Persistence;
using Domain;
using GameAiAndControls.Controls;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.Scenes.Intro
{
    public class Intro : IScene
    {
        public GameModes GameMode { get; } = GameModes.Live;

        public string SceneMusic { get; } = "music_intro";

        public SceneTypes SceneType { get; } = SceneTypes.Intro;

        public void SetupGameOverlay()
        {
            //No need for that in the intro
        }

        public void SetupScene(I3dWorld world)
        {
            var TheOmegaStrainLogo = LogoCube.CreateLogoCube();
            TheOmegaStrainLogo.ObjectOffsets = new Vector3 { x = 1000, y = 0, z = 0 };
            TheOmegaStrainLogo.Rotation = new Vector3 { x = 0, y = 0, z = 0 };
            //This object is centered on the world origin, so no offsets are needed, and it starts with no rotation.
            TheOmegaStrainLogo.WorldPosition = new Vector3 { x = 0, y = 0, z = 0 };
            //In here the logo will be moved according to the intro design, but it starts at the world origin.
            TheOmegaStrainLogo.Movement = new OmegaStrainLogoControls();
            world.WorldInhabitants.Add(TheOmegaStrainLogo);
        }

        public void SetupSceneOverlay()
        {
            GameState.ScreenOverlayState.ResetToDefaults();
            var o = GameState.ScreenOverlayState;

            o.Type = ScreenOverlayType.Intro;
            o.Anchor = ScreenOverlayAnchor.Top;

            // Page 1: Story
            o.AddPage(
                "RETROMESH SYSTEM INITIALIZING",
                "THE OMEGA STRAIN",
                "Year 2147.\n\n" +
                "A foreign organism has spread across the outer colonies.\n" +
                "Designated: OMEGA STRAIN.\n\n" +
                "Autonomous Seeder units detected.\n" +
                "Containment probability: 12%.",
                "PRESS ANY KEY TO INITIATE PROTOCOL");

            // Page 2: Gameplay tips
            o.AddPage(
                "RETROMESH // FIELD MANUAL",
                "TACTICAL BRIEFING",
                "WEAPONS:\n" +
                "  [1] BULLET  — High fire rate, effective vs Seeders\n" +
                "  [2] DECOY   — Lures Kamikaze Drones away from your ship\n" +
                "  [3] LAZER   — Powerful beam, cuts through targets\n\n" +
                "COMBAT TIPS:\n" +
                "  \u2022 Destroy Seeders to stop the infection from spreading\n" +
                "  \u2022 Kamikaze Drones will rush your ship — deploy Decoys!\n" +
                "  \u2022 Decoys unlock after collecting your first PowerUp\n" +
                "  \u2022 PowerUps drop from glowing Seeders\n" +
                "  \u2022 Eliminate all enemies to face the MotherShip\n\n" +
                "NAVIGATION:\n" +
                "  \u2022 Arrow keys or WASD to move\n" +
                "  \u2022 Follow the guidance arrow to find Seeders",
                "PRESS ANY KEY TO INITIATE PROTOCOL");

            // Page 3: Highscores
            o.AddPage(
                "RETROMESH // HALL OF FAME",
                "TOP PILOTS",
                BuildHighscoreBody(),
                "PRESS ANY KEY TO INITIATE PROTOCOL");

            o.CurrentPage = 0;
            o.ApplyPageContent();

            // LogoCube plays first
            o.ShowOverlay = false;

            // This is intro — don't auto-hide until player input
            o.AutoHide = false;
            o.AutoHideSeconds = 0f;

            // Optional: stronger cinematic feel
            o.DimStrength = 0.55f;
            o.PanelWidthRatio = 0.72f;
            o.PanelHeightRatio = 0.32f;
            //Hide Debug overlay
            o.ShowDebugOverlay = false;
        }

        public void SetupVideoOverlay(string fileName)
        {
            GameState.ScreenOverlayState.ShowVideoOverlay = true;
            GameState.ScreenOverlayState.VideoClipPath = Path.Combine("gamegraphics", "introclip.mp4");
        }

        private static string BuildHighscoreBody()
        {
            var list = HighscoreService.LoadLocalHighscores();
            var entries = list.Entries
                .OrderByDescending(e => e.Score)
                .Take(25)
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
