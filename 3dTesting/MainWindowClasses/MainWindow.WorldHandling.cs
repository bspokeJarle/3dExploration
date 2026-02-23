using _3dTesting._3dRotation;
using _3dTesting._Coordinates;
using _3dTesting.Helpers;
using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using Domain;
using GameAudioInstances;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.MainWindowClasses
{
    public class GameWorldManager
    {
        private long FrameCounter = 0;
        private int AiUpdateCounter = 0;
        private const int AiUpdateInterval = 5; // Update offscreen AI every 5 frames
        private readonly _3dTo2d From3dTo2d = new();
        private readonly _3dRotationCommon Rotate3d = new();
        private readonly ParticleManager particleManager = new();
        private readonly WeaponsManager weaponsManager = new();
        private StarFieldHandler StarFieldHandler { get; set; }

        readonly IAudioPlayer audioPlayer = new NAudioAudioPlayer("Soundeffects");
        private readonly ISoundRegistry soundRegistry = new JsonSoundRegistry("Soundeffects\\sounds.json");
        static SoundDefinition MusicDef { get; set; } = null;
        static bool MusicIsPlaying { get; set; } = false;

        public string DebugMessage { get; set; }
        private bool enableLocalLogging = false;
        public bool FadeOutWorld { get; set; } = false;
        public bool FadeInWorld { get; set; } = false;

        private readonly object _lock = new object();  // Lock object for thread safety
        public I3dObject ShipCopy { get; set; }
        public I3dObject SurfaceCopy { get; set; }

        public List<_2dTriangleMesh> UpdateWorld(_3dWorld._3dWorld world, ref List<_2dTriangleMesh> projectedCoordinates, ref List<_2dTriangleMesh> crashBoxCoordinates)
        {
            FrameCounter++;
            List<_3dObject> deepCopiedWorld;
            List<_3dObject> activeWorld;
            lock (_lock)
            {
                var inhabitants = world.WorldInhabitants;
                activeWorld = new List<_3dObject>(inhabitants.Count);

                foreach (var inhabitant in inhabitants)
                {
                    if (inhabitant.ObjectParts.Count == 0) continue;

                    if (inhabitant is _3dObject concreteInhabitant && concreteInhabitant.CheckInhabitantVisibility())
                    {
                        activeWorld.Add(concreteInhabitant);
                    }
                }

                deepCopiedWorld = Common3dObjectHelpers.DeepCopy3dObjects(activeWorld);
            }
            if (StarFieldHandler == null)
            {
                var parentSurface = world.WorldInhabitants?.FirstOrDefault(obj => obj.ObjectName == "Surface")?.ParentSurface;
                if (parentSurface != null)
                {
                    StarFieldHandler = new StarFieldHandler(parentSurface);
                }
            }
            //Generate starfield will do nothing if not needed
            if (StarFieldHandler != null)
            {
                StarFieldHandler.GenerateStarfield();
                if (StarFieldHandler.HasStars()) deepCopiedWorld.AddRange(StarFieldHandler.GetStars());
            }

            var particleObjectList = new List<_3dObject>();
            var weaponObjectList = new List<_3dObject>();
            var renderedList = new List<_3dObject>(deepCopiedWorld.Count);
            DebugMessage = string.Empty;

            AiUpdateCounter++;
            bool doAiMark = AiUpdateCounter >= AiUpdateInterval;
            if (doAiMark) AiUpdateCounter = 0;

            Dictionary<int, _3dObject> aiById = null;
            if (doAiMark)
            {
                aiById = InitializeAiOnScreenTracking();
            }

            foreach (var inhabitant in deepCopiedWorld)
            {
                if (!inhabitant.CheckInhabitantVisibility()) continue;
                inhabitant.IsOnScreen = true;
                if (doAiMark)
                {
                    SetAiIsOnScreen(aiById, inhabitant.ObjectId);
                }

                inhabitant.Movement?.MoveObject(inhabitant, audioPlayer, soundRegistry);
                if (inhabitant.CrashBoxesFollowRotation) inhabitant.CrashBoxes = RotateAllCrashboxes(inhabitant.CrashBoxes, (Vector3)inhabitant.Rotation);
                //Make copies of Ship and Surface for HUD display purposes
                if (inhabitant.ObjectName == "Ship")
                {
                    ShipCopy = inhabitant;
                }
                if (inhabitant.ObjectName == "Surface")
                {
                    SurfaceCopy = inhabitant;
                }

                foreach (var part in inhabitant.ObjectParts)
                {
                    part.Triangles = RotateMesh(part.Triangles, (Vector3)inhabitant.Rotation);

                    // Store the RotatedSurfaceTriangles
                    if (inhabitant.ObjectName == "Surface")
                    {
                        inhabitant.ParentSurface.RotatedSurfaceTriangles = part.Triangles;

                        var landBasedIds = new HashSet<long?>(part.Triangles.Count);
                        foreach (var triangle in part.Triangles)
                        {
                            landBasedIds.Add(triangle.landBasedPosition);
                        }
                        inhabitant.ParentSurface.LandBasedIds = landBasedIds;
                    }

                    SetMovementGuides(inhabitant, part, part.Triangles);
                }

                if (inhabitant.ObjectName == "Surface")
                    DebugMessage += $" Surface: Y Pos: {inhabitant.ObjectOffsets.y}";

                if (inhabitant.ObjectName == "Ship")
                    DebugMessage += $" Ship: Y Pos: {inhabitant.ObjectOffsets.y + 300} Z Rotation: {inhabitant.Rotation.z}";

                particleManager.HandleParticles(inhabitant, particleObjectList);
                weaponsManager.HandleWeapons(inhabitant, weaponObjectList);
                renderedList.Add(inhabitant);
            }

            if (particleObjectList.Count > 0)
            {
                renderedList.AddRange(particleObjectList);
                DebugMessage += $" Number of Particles on screen {particleObjectList.Count}";
            }
            if (weaponObjectList.Count > 0)
            {
                renderedList.AddRange(weaponObjectList);
                DebugMessage += $" Number of Weapons on screen {weaponObjectList.Count}";
            }

            var ship = activeWorld.FirstOrDefault(x => x.ObjectName == "Ship");
            if (ship != null && ship.ImpactStatus.ObjectHealth <= 0 && !FadeOutWorld)
            {
                //When the ship health is 0, start the fade out effect (explosion will also be triggered)
                FadeOutWorld = true;
            }
            if (ship != null && ship.ImpactStatus.HasExploded)
            {
                //When the ship has exploded, start the fade in effect, should take longer than fade out
                FadeOutWorld = false;
                FadeInWorld = true;
                //Dispose the ship movement to free resources
                ship.Movement.Dispose();
                //Dispose weapon systems to free resources
                world.WorldInhabitants.Clear();
                //Clear AI objects and dirty tiles to free resources, should be no need to keep them after explosion
                GameState.SurfaceState.AiObjects.Clear();
                GameState.SurfaceState.DirtyTiles.Clear();
                //Remove stars
                StarFieldHandler.ClearStars();
                StarFieldHandler = null;
                //When explosion has happened, reset the scene
                world.SceneHandler.ResetActiveScene(world);
                //Reset back to Default Map Position
                return [];
            }

            if (doAiMark)
            {
                AiUpdateCounter = 0;
                //Separate loop for objects that need to interact with AI while off screen
                foreach (var aiObject in GameState.SurfaceState.AiObjects)
                {
                    //Move object offscreen, for now no audio when moving off screen
                    if (aiObject.IsOnScreen == false)
                    {
                        aiObject.Movement.MoveObject(aiObject, null, null);
                        aiObject.IsOnScreen = false;
                    }
                }
            }

            projectedCoordinates = From3dTo2d.ConvertTo2dFromObjects(renderedList, FrameCounter);
            CrashDetection.HandleCrashboxes(renderedList, world.IsPaused);
            var activeScene = world.SceneHandler.GetActiveScene();
            if (activeScene != null)
            {
                HandleMusic(renderedList,activeScene.SceneMusic);
            }
            return projectedCoordinates;
        }

        private Dictionary<int, _3dObject> InitializeAiOnScreenTracking()
        {
            var aiObjects = GameState.SurfaceState.AiObjects;
            if (aiObjects == null || aiObjects.Count == 0)
                return null;

            var aiById = new Dictionary<int, _3dObject>(aiObjects.Count);

            foreach (var ai in aiObjects)
            {
                ai.IsOnScreen = false;          // reset for this AI tick
                aiById[ai.ObjectId] = ai;       // O(1) lookup later
            }

            return aiById;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetAiIsOnScreen(
            Dictionary<int, _3dObject> aiById,
            int objectId
        )
        {
            if (aiById != null && aiById.TryGetValue(objectId, out var aiObj))
            {
                aiObj.IsOnScreen = true;
            }
        }

        public void HandleMusic(List<_3dObject> renderedObjects, string sceneMusic)
        {
            //var music = wor
            if (MusicDef == null) MusicDef = soundRegistry.Get(sceneMusic);
            if (!MusicIsPlaying)
            {
                MusicIsPlaying = true;
                audioPlayer.PlayMusic(MusicDef, 0.2f);
            }
            //Work with renderedobjects to decide what music to play, for now just play mainmusicloop
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<List<IVector3>> RotateAllCrashboxes(List<List<IVector3>> crashboxes, Vector3 rotation)
        {
            var rotatedCrashboxes = new List<List<IVector3>>(crashboxes.Count);
            foreach (var crashbox in crashboxes)
            {
                var rotated = new List<IVector3>(crashbox.Count);
                foreach (var point in crashbox)
                {
                    rotated.Add(RotatePoint((Vector3)point, rotation));
                }
                rotatedCrashboxes.Add(rotated);
            }
            return rotatedCrashboxes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IVector3 RotatePoint(Vector3 point, Vector3 rotation)
        {
            var rotatedPoint = Rotate3d.RotatePoint(rotation.z, point, 'Z');
            rotatedPoint = Rotate3d.RotatePoint(rotation.y, rotatedPoint, 'Y');
            rotatedPoint = Rotate3d.RotatePoint(rotation.x, rotatedPoint, 'X');
            return rotatedPoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<ITriangleMeshWithColor> RotateMesh(List<ITriangleMeshWithColor> mesh, Vector3 rotation)
        {
            var rotatedMesh = Rotate3d.RotateMesh(mesh, rotation.z, 'Z');
            rotatedMesh = Rotate3d.RotateMesh(rotatedMesh, rotation.y, 'Y');
            rotatedMesh = Rotate3d.RotateMesh(rotatedMesh, rotation.x, 'X');
            return rotatedMesh;
        }

        private void SetMovementGuides(_3dObject inhabitant, I3dObjectPart part, List<ITriangleMeshWithColor> rotatedMesh)
        {
            switch (part.PartName)
            {
                case "SeederParticlesStartGuide":
                    inhabitant.Movement.SetParticleGuideCoordinates(rotatedMesh.First() as TriangleMeshWithColor, null);
                    break;
                case "SeederParticlesGuide":
                    inhabitant.Movement.SetParticleGuideCoordinates(null,rotatedMesh.First() as TriangleMeshWithColor);
                    break;
                case "JetMotor":
                    inhabitant.Movement.SetParticleGuideCoordinates(rotatedMesh.First() as TriangleMeshWithColor, null);
                    break;
                case "WeaponDirectionGuide":
                    inhabitant.Movement.SetWeaponGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
                    break;
                case "WeaponStartGuide":
                    inhabitant.Movement.SetWeaponGuideCoordinates(rotatedMesh.First() as TriangleMeshWithColor, null);
                    break;
                case "JetMotorDirectionGuide":
                    if (enableLocalLogging) Logger.Log($"MainLoop Set Guide after rotation: {rotatedMesh.First().vert1.x + ", " + rotatedMesh.First().vert1.y + ", " + rotatedMesh.First().vert1.z} Inhabitant:{inhabitant.ObjectName} ");
                    inhabitant.Movement.SetParticleGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
                    break;
            }
        }
    }
}
