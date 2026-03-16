using _3dTesting._3dRotation;
using _3dTesting._Coordinates;
using _3dTesting.Helpers;
using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using Domain;
using GameAudioInstances;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.MainWindowClasses.Loops
{
    public class LiveGameLoop : IGameLoop<_2dTriangleMesh>
    {
        private const int perfLogInterval = 10;

        private long FrameCounter = 0;
        private readonly Stopwatch frameTimer = new();
        private long performanceFrameCount = 0;
        private double averageFrameMs = 0;
        private double averageHeadroomMs = 0;
        private int AiUpdateCounter = 0;
        private const int AiUpdateInterval = 5; // Update offscreen AI every 5 frames
        private readonly _3dTo2d From3dTo2d = new();
        private readonly _3dRotationCommon Rotate3d = new();
        private readonly ParticleManager particleManager = new();
        private readonly WeaponsManager weaponsManager = new();
        private StarFieldHandler StarFieldHandler { get; set; }

        private readonly IAudioPlayer audioPlayer = new NAudioAudioPlayer("Soundeffects");
        private readonly ISoundRegistry soundRegistry = new JsonSoundRegistry("Soundeffects\\sounds.json");
        private static SoundDefinition MusicDef { get; set; } = null;
        private static bool MusicIsPlaying { get; set; } = false;

        public string DebugMessage { get; set; }
        private bool enableLocalLogging = false;
        public bool FadeOutWorld { get; set; } = false;
        public bool FadeInWorld { get; set; } = false;

        private readonly object _lock = new object();
        public I3dObject ShipCopy { get; set; }
        public I3dObject SurfaceCopy { get; set; }

        public List<_2dTriangleMesh> UpdateWorld(I3dWorld world, ref List<_2dTriangleMesh> projectedCoordinates, ref List<_2dTriangleMesh> crashBoxCoordinates)
        {
            frameTimer.Restart();
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

            var activeScene = world.SceneHandler.GetActiveScene();

            var ship = activeWorld.FirstOrDefault(x => x.ObjectName == "Ship");
            if (ship != null && ship.ImpactStatus.ObjectHealth <= 0 && !FadeOutWorld)
            {
                FadeOutWorld = true;
            }
            if (ship != null && ship.ImpactStatus.HasExploded)
            {
                FadeOutWorld = false;
                FadeInWorld = true;
                CleanupWorldObjects(world.WorldInhabitants.OfType<_3dObject>().ToList());
                world.WorldInhabitants.Clear();
                GameState.SurfaceState.AiObjects.Clear();
                GameState.SurfaceState.DirtyTiles.Clear();
                GameState.ShipState.BestCandidateStates.Clear();
                StarFieldHandler.ClearStars();
                StarFieldHandler = null;
                world.SceneHandler.ResetActiveScene(world);
                TrackFrameTiming((int)FrameCounter);
                return [];
            }

            if (doAiMark)
            {
                AiUpdateCounter = 0;
                foreach (var aiObject in GameState.SurfaceState.AiObjects)
                {
                    if (aiObject.IsOnScreen == false)
                    {
                        aiObject.Movement.MoveObject(aiObject, null, null);
                        aiObject.IsOnScreen = false;
                    }
                }
            }

            projectedCoordinates = From3dTo2d.ConvertTo2dFromObjects(renderedList, FrameCounter);
            CrashDetection.HandleCrashboxes(renderedList, world.IsPaused);
            CleanupExplodedObjects(world);
            if (activeScene != null)
            {
                HandleMusic(renderedList, activeScene.SceneMusic);
            }

            TrackFrameTiming((int)FrameCounter);
            return projectedCoordinates;
        }

        private void CleanupExplodedObjects(I3dWorld world)
        {
            lock (_lock)
            {
                var explodedObjects = world.WorldInhabitants
                    .OfType<_3dObject>()
                    .Where(obj => obj.ObjectName != "Ship" && obj.ImpactStatus?.HasExploded == true)
                    .ToList();

                if (explodedObjects.Count == 0)
                {
                    return;
                }

                CleanupWorldObjects(explodedObjects);

                var explodedIds = explodedObjects.Select(obj => obj.ObjectId).ToHashSet();
                world.WorldInhabitants.RemoveAll(obj => explodedIds.Contains(obj.ObjectId));
                GameState.SurfaceState.AiObjects.RemoveAll(obj => explodedIds.Contains(obj.ObjectId));
                GameState.ShipState.BestCandidateStates.RemoveAll(state =>
                    state?.BestEnemyCandidate?.EnemyObject != null &&
                    explodedIds.Contains(state.BestEnemyCandidate.EnemyObject.ObjectId));
            }
        }

        private static void CleanupWorldObjects(List<_3dObject> objects)
        {
            foreach (var obj in objects)
            {
                TryDisposeMovement(obj);

                if (obj.Particles != null)
                {
                    obj.Particles.Particles.Clear();
                    obj.Particles = null;
                }

                if (obj.WeaponSystems != null)
                {
                    obj.WeaponSystems.ActiveWeapons.Clear();
                    obj.WeaponSystems = null;
                }

                obj.CrashBoxes?.Clear();
                obj.ObjectParts?.Clear();
                obj.CalculatedCrashOffset = null;
                obj.WorldPosition = null;
                obj.ObjectOffsets = null;
                obj.ParentSurface = null;
                obj.Movement = null;
                obj.ImpactStatus = null;
            }
        }

        private static void TryDisposeMovement(_3dObject obj)
        {
            if (obj?.Movement == null)
            {
                return;
            }

            try
            {
                obj.Movement.Dispose();
            }
            catch (NotImplementedException)
            {
            }
        }

        private Dictionary<int, _3dObject> InitializeAiOnScreenTracking()
        {
            var aiObjects = GameState.SurfaceState.AiObjects;
            if (aiObjects == null || aiObjects.Count == 0)
                return null;

            var aiById = new Dictionary<int, _3dObject>(aiObjects.Count);

            foreach (var ai in aiObjects)
            {
                ai.IsOnScreen = false;
                aiById[ai.ObjectId] = ai;
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
            if (MusicDef == null) MusicDef = soundRegistry.Get(sceneMusic);
            if (!MusicIsPlaying)
            {
                MusicIsPlaying = true;
                audioPlayer.PlayMusic(MusicDef, 0.2f);
            }
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
                    inhabitant.Movement.SetParticleGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
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

        private void TrackFrameTiming(int frameIndex)
        {
            if (!frameTimer.IsRunning)
                return;

            frameTimer.Stop();
            if (!enableLocalLogging)
                return;
            if (!Logger.EnableFileLogging)
                return;

            var budgetMs = 1000.0 / CommonUtilities.CommonSetup.ScreenSetup.targetFps;
            var elapsedMs = frameTimer.Elapsed.TotalMilliseconds;
            var headroomMs = budgetMs - elapsedMs;
            var headroomPct = (headroomMs / budgetMs) * 100.0;

            performanceFrameCount++;
            averageFrameMs += (elapsedMs - averageFrameMs) / performanceFrameCount;
            averageHeadroomMs += (headroomMs - averageHeadroomMs) / performanceFrameCount;

            DebugMessage += $" PerfHeadroom: {headroomPct:0.#}%";

            if (performanceFrameCount % perfLogInterval == 0)
            {
                var avgHeadroomPct = (averageHeadroomMs / budgetMs) * 100.0;
                Logger.Log($"[LivePerf] frame={frameIndex} frameMs={elapsedMs:0.###} headroomMs={headroomMs:0.###} headroomPct={headroomPct:0.#} avgFrameMs={averageFrameMs:0.###} avgHeadroomMs={averageHeadroomMs:0.###} avgHeadroomPct={avgHeadroomPct:0.#}");
            }
        }

    }
}
