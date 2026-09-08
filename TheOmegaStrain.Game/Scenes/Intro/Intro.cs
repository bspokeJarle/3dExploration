using TheOmegaStrain.Game.World.Objects.LogoCube;
using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Common.Persistence;
using TheOmegaStrain.Domain;
using TheOmegaStrain.Gameplay.Controls;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TheOmegaStrain.Game.Scenes.Intro
{
    public class Intro : IScene
    {
        // Controller line is only shown when a controller is actually detected.
        private static string BuildStoryFooter()
        {
            var activeScheme = GameState.SettingsState.EffectiveControlScheme;
            string keyboardActive = activeScheme == ControlInputMode.Keyboard || activeScheme == ControlInputMode.Mouse
                ? " <- ACTIVE"
                : string.Empty;
            string controllerActive = activeScheme == ControlInputMode.XboxController
                ? " <- ACTIVE"
                : string.Empty;

            string footer =
                "PRESS ANY KEY OR XBOX [A] TO START";

            if (GameState.InputDeviceState.AnyControllerConnected)
                footer += $"\n[X] CONTROLLER SETTINGS{controllerActive}";

            footer += $"\n[K] KEYBOARD / MOUSE SETTINGS{keyboardActive}";

            return footer + " | ESC QUIT";
        }

        private const string StoryPageTitle = "THE OMEGA STRAIN";

        /// <summary>
        /// Controllers can be plugged in after the overlay was built, so the story
        /// footer is refreshed whenever the detected controller state changes.
        /// </summary>
        public static void RefreshControlFooter()
        {
            var overlay = GameState.ScreenOverlayState;
            if (overlay.Type != ScreenOverlayType.Intro)
                return;

            string footer = BuildStoryFooter();

            for (int i = 0; i < overlay.Pages.Count; i++)
            {
                var page = overlay.Pages[i];
                if (page.Length < 4 || page[1] != StoryPageTitle)
                    continue;

                if (page[3] == footer)
                    return;

                page[3] = footer;
                if (overlay.CurrentPage == i)
                    overlay.Footer = footer;
                return;
            }
        }

        private const string InfoFooter =
            "PRESS ANY KEY OR XBOX [A] TO START\n" +
            "ARROWS / D-PAD CHANGE PAGE";

        public bool SkipLogoCube { get; set; } = false;

        public GameModes GameMode { get; } = GameModes.Playback;

        public string SceneMusic { get; } = "music_intro";

        public SceneTypes SceneType { get; } = SceneTypes.Intro;
        public SceneBiomeTypes SceneBiome { get; } = SceneBiomeTypes.HillsWoods;

        public void SetupGameOverlay()
        {
            //No need for that in the intro
        }

        public void SetupScene(I3dWorld world)
        {
            if (SkipLogoCube)
            {
                // Returning mid-game - show the overlay immediately without the logo animation
                GameState.ScreenOverlayState.ShowOverlay = true;
                return;
            }

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
                StoryPageTitle,
                "Year 2147.\n\n" +
                "A foreign organism has spread across the outer colonies.\n" +
                "Designated: OMEGA STRAIN.\n\n" +
                "Autonomous Seeder units detected.\n" +
                "Containment probability: 12%.",
                BuildStoryFooter());

            // Page 2: Gameplay tips
            o.AddPage(
                "RETROMESH // FIELD MANUAL",
                "TACTICAL TIPS",
                "COMBAT TIPS:\n" +
                "  - Destroy Seeders fast to control infection spread\n" +
                "  - Every Seeder kill helps slow the infection cascade\n" +
                "  - Kamikaze Drones will rush your ship - deploy Decoys!\n" +
                "  - Decoys unlock after collecting your first PowerUp\n" +
                "  - PowerUps drop from glowing Seeders\n" +
                "  - Eliminate all enemies to face the MotherShip",
                InfoFooter);

            // Page 3: Highscores
            o.AddPage(
                "RETROMESH // HALL OF FAME",
                "TOP PILOTS",
                HighscoreOverlayFormatter.BuildBody(),
                InfoFooter);

            o.CurrentPage = 0;
            o.ApplyPageContent();

            // LogoCube plays first
            o.ShowOverlay = false;

            // This is intro - don't auto-hide until player input
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

    }
}
