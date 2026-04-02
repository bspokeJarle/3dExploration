using _3dRotations.World.Objects.LogoCube;
using CommonUtilities.CommonGlobalState;
using Domain;
using GameAiAndControls.Controls;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.Scenes.Outro
{
    public class Outro : IScene
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

            o.Header = "RETROMESH SYSTEM INITIALIZING";
            o.Title = "THE OMEGA STRAIN";

            o.Body =
                "Year 2147.\n\n" +
                "A foreign organism has spread across the outer colonies.\n" +
                "Designated: OMEGA STRAIN.\n\n" +
                "Autonomous Seeder units detected.\n" +
                "Containment probability: 12%.";

            o.Footer = "PRESS ANY KEY TO INITIATE PROTOCOL";

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
    }
}
