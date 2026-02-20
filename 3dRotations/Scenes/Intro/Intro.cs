using _3dRotations.World.Objects.LogoCube;
using Domain;
using GameAiAndControls.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.Scenes.Intro
{
    public class Intro : IScene
    {
        public GameModes GameMode { get; } = GameModes.Playback;

        public string SceneMusic { get; } = "music_intro";

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
    }
}
