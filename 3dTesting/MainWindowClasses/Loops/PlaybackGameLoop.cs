using _3dTesting._3dRotation;
using _3dTesting._Coordinates;
using _3dTesting.Helpers;
using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using Domain;
using GameAudioInstances;
using GameplayHelpers.ReplayIO;
using GameAiAndControls.Physics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using static Domain._3dSpecificsImplementations;
using _3dRotations.World.Objects;

namespace _3dTesting.MainWindowClasses.Loops
{
    public class PlaybackGameLoop : IGameLoop<_2dTriangleMesh>
    {
        private const bool enablePlaybackDiagnostics = false;
        private readonly _3dTo2d from3dTo2d = new();
        private readonly _3dRotationCommon rotate3d = new();
        private readonly IAudioPlayer audioPlayer = new NAudioAudioPlayer("Soundeffects");
        private readonly ISoundRegistry soundRegistry = new JsonSoundRegistry("Soundeffects\\sounds.json");
        private static SoundDefinition MusicDef { get; set; } = null;
        private static bool MusicIsPlaying { get; set; } = false;

        private readonly Dictionary<int, ExplosionState> explodingObjects = new();
        private readonly HashSet<int> triggeredExplosions = new();
        private readonly List<_3dObject> worldObjects = new();
        private readonly Dictionary<int, _3dObject> worldById = new();
        private readonly Dictionary<string, List<_3dObject>> worldByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> nameCursor = new(StringComparer.OrdinalIgnoreCase);
        private int worldObjectCount = -1;
        private List<ReplayIO.FrameState> frames = new();
        private List<FrameCache> frameCaches = new();
        private int maxCachedObjectId = -1;
        private bool replayLoaded = false;
        private int fps = 60;
        private DateTime lastFrameTime = DateTime.MinValue;
        private double playbackFramePosition = 0;
        private int debugLogCounter = 0;

        private List<_2dTriangleMesh> lastProjectedCoordinates = new();

        public string DebugMessage { get; set; } = string.Empty;
        public bool FadeOutWorld { get; set; }
        public bool FadeInWorld { get; set; }
        public I3dObject ShipCopy { get; set; }
        public I3dObject SurfaceCopy { get; set; }

        public List<_2dTriangleMesh> UpdateWorld(I3dWorld world, ref List<_2dTriangleMesh> projectedCoordinates, ref List<_2dTriangleMesh> crashBoxCoordinates)
        {
            if (!replayLoaded)
            {
                LoadReplayIfNeeded();
            }

            if (frames == null || frames.Count == 0)
            {
                projectedCoordinates = lastProjectedCoordinates;
                return lastProjectedCoordinates;
            }

            if (frameCaches.Count != frames.Count)
                BuildFrameCaches();

            var (frameIndex, lerpT) = GetFramePosition();
            if (frameIndex < 0)
            {
                projectedCoordinates = lastProjectedCoordinates;
                return lastProjectedCoordinates;
            }

            var nextFrameIndex = (frameIndex + 1) % frames.Count;
            var frame = frames[frameIndex];
            var nextFrame = frames[nextFrameIndex];
            var frameCache = frameCaches[frameIndex];
            var nextFrameCache = frameCaches[nextFrameIndex];

            if (enablePlaybackDiagnostics && Logger.EnableFileLogging)
            {
                var shipState = frame.ObjectStates.FirstOrDefault(s => string.Equals(s.ObjectName, "Ship", StringComparison.OrdinalIgnoreCase))
                    ?? frame.ObjectStates.FirstOrDefault();
                var shipOffset = shipState?.ObjectOffset ?? new Vector3();
                Logger.Log($"[PlaybackDiag] pos={playbackFramePosition:0.###} idx={frameIndex} lerp={lerpT:0.###} map=({frame.GlobalMapPosition.x:0.##},{frame.GlobalMapPosition.y:0.##},{frame.GlobalMapPosition.z:0.##}) shipOff=({shipOffset.x:0.##},{shipOffset.y:0.##},{shipOffset.z:0.##})", "Replay");
            }

            var deltaSummary = BuildFrameDeltaSummary(frame, nextFrame);
            LogFrameDelta(deltaSummary, frame.FrameIndex);
            if (Logger.EnableFileLogging)
            {
                Logger.Log($"[PlaybackFrame] {frame.FrameIndex}");
                var shipState = frame.ObjectStates.FirstOrDefault(s => string.Equals(s.ObjectName, "Ship", StringComparison.OrdinalIgnoreCase));
                if (shipState != null)
                {
                    var shipPos = shipState.WorldPosition;
                    var shipOffset = shipState.ObjectOffset;
                    var shipRot = shipState.Rotation;
                    Logger.Log($"[PlaybackShip] frame={frame.FrameIndex} pos=({shipPos.x:0.##},{shipPos.y:0.##},{shipPos.z:0.##}) offset=({shipOffset.x:0.##},{shipOffset.y:0.##},{shipOffset.z:0.##}) rot=({shipRot.x:0.##},{shipRot.y:0.##},{shipRot.z:0.##})");
                }
            }

            GameState.SurfaceState.GlobalMapPosition = LerpVector(frame.GlobalMapPosition, nextFrame.GlobalMapPosition, lerpT);

            var objectIds = frameCache.ObjectIdsWithNext;
            var stateById = frameCache.StateByIdArray;
            var nextStateById = nextFrameCache.StateByIdArray;

            EnsureWorldCaches(world);
            nameCursor.Clear();
            var transientObjects = new List<_3dObject>();

            foreach (var objectId in objectIds)
            {
                var stateA = objectId < stateById.Length ? stateById[objectId] : null;
                var stateB = objectId < nextStateById.Length ? nextStateById[objectId] : null;

                var state = stateA ?? stateB;
                if (state == null)
                    continue;

                var obj = ResolveObject(state, worldById, worldByName, nameCursor);
                if (obj == null)
                {
                    obj = TryCreateTransientObject(state, worldObjects);
                    if (obj != null)
                        transientObjects.Add(obj);
                    else
                        continue;
                }

                var worldPos = LerpVectorNullable(stateA?.WorldPosition, stateB?.WorldPosition, lerpT);
                var offsets = LerpVectorNullable(stateA?.ObjectOffset, stateB?.ObjectOffset, lerpT);
                var rotation = LerpVectorNullable(stateA?.Rotation, stateB?.Rotation, lerpT);

                obj.WorldPosition = worldPos;
                obj.ObjectOffsets = offsets;
                obj.Rotation = rotation;
                obj.IsOnScreen = true;

                if (obj.ObjectName == "Surface")
                {
                    obj.Movement?.MoveObject(obj, audioPlayer, soundRegistry);
                }

                if (obj.ObjectName == "Ship")
                    ShipCopy = obj;

                if (obj.ObjectName == "Surface")
                    SurfaceCopy = obj;

                bool triggerExplode = (stateA?.TriggerExplode ?? false) || (stateB?.TriggerExplode ?? false);
                if (triggerExplode && !triggeredExplosions.Contains(obj.ObjectId))
                {
                    StartExplosion(obj);
                    triggeredExplosions.Add(obj.ObjectId);
                }
            }

            for (int i = 0; i < worldObjects.Count; i++)
            {
                var obj = worldObjects[i];
                if (obj.ObjectId < 0 || obj.ObjectId >= frameCache.ObjectIdMask.Length || !frameCache.ObjectIdMask[obj.ObjectId])
                {
                    obj.IsOnScreen = false;
                }
            }

            UpdateExplosions();

            var activeObjects = new List<_3dObject>(worldObjects.Count + transientObjects.Count);
            for (int i = 0; i < worldObjects.Count; i++)
            {
                var obj = worldObjects[i];
                if (objectIds.Contains(obj.ObjectId) && obj.ObjectParts.Count > 0)
                    activeObjects.Add(obj);
            }

            if (transientObjects.Count > 0)
                activeObjects.AddRange(transientObjects);

            var deepCopiedWorld = Common3dObjectHelpers.DeepCopy3dObjects(activeObjects);
            var renderedList = new List<_3dObject>(deepCopiedWorld.Count);

            foreach (var inhabitant in deepCopiedWorld)
            {
                if (!inhabitant.CheckInhabitantVisibility()) continue;

                if (inhabitant.CrashBoxesFollowRotation)
                    inhabitant.CrashBoxes = RotateAllCrashboxes(inhabitant.CrashBoxes, (Vector3)inhabitant.Rotation);

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

                renderedList.Add(inhabitant);
            }

            projectedCoordinates = from3dTo2d.ConvertTo2dFromObjects(renderedList, frame.FrameIndex);
            if (projectedCoordinates.Count == 0 && lastProjectedCoordinates.Count > 0)
            {
                projectedCoordinates = lastProjectedCoordinates;
                return lastProjectedCoordinates;
            }
            lastProjectedCoordinates = projectedCoordinates;
            CrashDetection.HandleCrashboxes(renderedList, world.IsPaused);

            var activeScene = world.SceneHandler.GetActiveScene();
            if (activeScene != null)
            {
                HandleMusic(renderedList, activeScene.SceneMusic);
            }

            DebugMessage = $"Playback Frame {frame.FrameIndex} {deltaSummary}";
            return projectedCoordinates;
        }

        public void FinalizeRecording()
        {
        }

        private void LoadReplayIfNeeded()
        {
            var surfaceFile = GameState.SurfaceState.SurfaceFilePath;
            if (string.IsNullOrWhiteSpace(surfaceFile))
            {
                replayLoaded = true;
                return;
            }

            var replayPath = BuildReplayPath(surfaceFile);
            if (ReplayIO.TryLoad(replayPath, out _, out var loadedFps, out var loadedFrames))
            {
                frames = loadedFrames;
                fps = loadedFps > 0 ? loadedFps : 60;
                playbackFramePosition = 0;
                lastFrameTime = DateTime.MinValue;
                BuildFrameCaches();
            }

            replayLoaded = true;
        }

        private void BuildFrameCaches()
        {
            maxCachedObjectId = 0;
            for (int i = 0; i < frames.Count; i++)
            {
                var states = frames[i].ObjectStates;
                for (int j = 0; j < states.Count; j++)
                {
                    if (states[j].ObjectId > maxCachedObjectId)
                        maxCachedObjectId = states[j].ObjectId;
                }
            }

            frameCaches = new List<FrameCache>(frames.Count);
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                var stateById = new ReplayIO.ReplayObjectState?[maxCachedObjectId + 1];
                for (int j = 0; j < frame.ObjectStates.Count; j++)
                {
                    var state = frame.ObjectStates[j];
                    if (state.ObjectId >= 0 && state.ObjectId < stateById.Length)
                        stateById[state.ObjectId] = state;
                }

                var objectIdMask = new bool[maxCachedObjectId + 1];
                for (int j = 0; j < frame.ObjectStates.Count; j++)
                {
                    var id = frame.ObjectStates[j].ObjectId;
                    if (id >= 0 && id < objectIdMask.Length)
                        objectIdMask[id] = true;
                }

                if (i + 1 < frames.Count)
                {
                    var nextStates = frames[i + 1].ObjectStates;
                    for (int j = 0; j < nextStates.Count; j++)
                    {
                        var id = nextStates[j].ObjectId;
                        if (id >= 0 && id < objectIdMask.Length)
                            objectIdMask[id] = true;
                    }
                }

                var objectIds = new List<int>();
                for (int id = 0; id < objectIdMask.Length; id++)
                {
                    if (objectIdMask[id])
                        objectIds.Add(id);
                }

                frameCaches.Add(new FrameCache
                {
                    StateByIdArray = stateById,
                    ObjectIdsWithNext = objectIds.ToArray(),
                    ObjectIdMask = objectIdMask
                });
            }
        }

        private (int index, float t) GetFramePosition()
        {
            if (frames == null || frames.Count == 0)
                return (-1, 0f);

            int index = (int)playbackFramePosition;
            if (index < 0 || index >= frames.Count)
                index = 0;

            playbackFramePosition = (playbackFramePosition + 1) % frames.Count;
            return (index, 0f);
        }

        private void StartExplosion(_3dObject obj)
        {
            if (explodingObjects.ContainsKey(obj.ObjectId))
                return;

            var physics = new Physics();
            var exploded = physics.ExplodeObject(obj, 200f);

            obj.ObjectParts = exploded.ObjectParts;
            obj.CrashBoxes = new List<List<IVector3>>();

            if (obj.ImpactStatus == null)
                obj.ImpactStatus = new ImpactStatus();

            obj.ImpactStatus.HasExploded = false;

            explodingObjects[obj.ObjectId] = new ExplosionState
            {
                Physics = physics,
                StartTime = DateTime.Now,
                Target = obj
            };
        }

        private void UpdateExplosions()
        {
            if (explodingObjects.Count == 0)
                return;

            var explodedIds = new List<int>();
            foreach (var kvp in explodingObjects)
            {
                var state = kvp.Value;
                state.Physics.UpdateExplosion(state.Target, state.StartTime);

                if (state.Target.ImpactStatus?.HasExploded == true)
                {
                    state.Target.ObjectParts = new List<I3dObjectPart>();
                    state.Target.CrashBoxes = new List<List<IVector3>>();
                    explodedIds.Add(kvp.Key);
                }
            }

            foreach (var id in explodedIds)
            {
                explodingObjects.Remove(id);
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
            var rotatedPoint = rotate3d.RotatePoint(rotation.z, point, 'Z');
            rotatedPoint = rotate3d.RotatePoint(rotation.y, rotatedPoint, 'Y');
            rotatedPoint = rotate3d.RotatePoint(rotation.x, rotatedPoint, 'X');
            return rotatedPoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<ITriangleMeshWithColor> RotateMesh(List<ITriangleMeshWithColor> mesh, Vector3 rotation)
        {
            var rotatedMesh = rotate3d.RotateMesh(mesh, rotation.z, 'Z');
            rotatedMesh = rotate3d.RotateMesh(rotatedMesh, rotation.y, 'Y');
            rotatedMesh = rotate3d.RotateMesh(rotatedMesh, rotation.x, 'X');
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
                    inhabitant.Movement.SetParticleGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
                    break;
            }
        }

        private void EnsureWorldCaches(I3dWorld world)
        {
            var inhabitants = world.WorldInhabitants;
            int count = inhabitants?.Count ?? 0;
            if (count == worldObjectCount)
                return;

            worldObjects.Clear();
            worldById.Clear();
            worldByName.Clear();

            if (inhabitants != null)
            {
                for (int i = 0; i < inhabitants.Count; i++)
                {
                    if (inhabitants[i] is not _3dObject obj)
                        continue;

                    worldObjects.Add(obj);
                    worldById[obj.ObjectId] = obj;

                    var name = obj.ObjectName ?? string.Empty;
                    if (!worldByName.TryGetValue(name, out var list))
                    {
                        list = new List<_3dObject>();
                        worldByName[name] = list;
                    }
                    list.Add(obj);
                }
            }

            worldObjectCount = count;

        }

        private static string BuildReplayPath(string surfaceFile)
        {
            var directory = Path.GetDirectoryName(surfaceFile) ?? string.Empty;
            var fileName = Path.GetFileNameWithoutExtension(surfaceFile);
            var extension = Path.GetExtension(surfaceFile);
            var replayName = string.IsNullOrWhiteSpace(extension)
                ? $"{fileName}_playback"
                : $"{fileName}_playback{extension}";

            return Path.Combine(directory, replayName);
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

        private static string BuildFrameDeltaSummary(ReplayIO.FrameState current, ReplayIO.FrameState next)
        {
            var mapDelta = new Vector3(
                next.GlobalMapPosition.x - current.GlobalMapPosition.x,
                next.GlobalMapPosition.y - current.GlobalMapPosition.y,
                next.GlobalMapPosition.z - current.GlobalMapPosition.z);

            Vector3 objectDelta = new Vector3();
            var curObj = current.ObjectStates.FirstOrDefault(s => string.Equals(s.ObjectName, "Ship", StringComparison.OrdinalIgnoreCase))
                ?? (current.ObjectStates.Count > 0 ? current.ObjectStates[0] : null);
            var nextObj = next.ObjectStates.FirstOrDefault(s => string.Equals(s.ObjectName, "Ship", StringComparison.OrdinalIgnoreCase))
                ?? (next.ObjectStates.Count > 0 ? next.ObjectStates[0] : null);

            if (curObj != null && nextObj != null)
            {
                objectDelta = new Vector3(
                    nextObj.ObjectOffset.x - curObj.ObjectOffset.x,
                    nextObj.ObjectOffset.y - curObj.ObjectOffset.y,
                    nextObj.ObjectOffset.z - curObj.ObjectOffset.z);
            }

            return $"?Map({mapDelta.x:0.##},{mapDelta.y:0.##},{mapDelta.z:0.##}) ?Ship({objectDelta.x:0.##},{objectDelta.y:0.##},{objectDelta.z:0.##})";
        }

        private static _3dObject ResolveObject(ReplayIO.ReplayObjectState state, Dictionary<int, _3dObject> byId, Dictionary<string, List<_3dObject>> byName, Dictionary<string, int> cursors)
        {
            if (byId.TryGetValue(state.ObjectId, out var obj))
                return obj;

            if (!string.IsNullOrWhiteSpace(state.ObjectName) && byName.TryGetValue(state.ObjectName, out var list) && list.Count > 0)
            {
                if (!cursors.TryGetValue(state.ObjectName, out var index))
                    index = 0;

                var picked = list[index % list.Count];
                cursors[state.ObjectName] = index + 1;
                return picked;
            }

            return null;
        }

        private _3dObject TryCreateTransientObject(ReplayIO.ReplayObjectState state, List<_3dObject> worldObjects)
        {
            if (string.Equals(state.ObjectName, "Lazer", StringComparison.OrdinalIgnoreCase))
            {
                var surface = worldObjects.FirstOrDefault(obj => obj.ObjectName == "Surface")?.ParentSurface;
                if (surface == null)
                    return null;

                var lazer = Lazer.CreateLazer(surface);
                lazer.ObjectId = state.ObjectId;
                return lazer;
            }

            if (string.Equals(state.ObjectName, "Particle", StringComparison.OrdinalIgnoreCase))
            {
                var surface = worldObjects.FirstOrDefault(obj => obj.ObjectName == "Surface")?.ParentSurface;
                var tri = new TriangleMeshWithColor
                {
                    Color = "FFFFFF",
                    vert1 = new Vector3 { x = -2, y = -2, z = 0 },
                    vert2 = new Vector3 { x = 2, y = -2, z = 0 },
                    vert3 = new Vector3 { x = 0, y = 2, z = 0 }
                };

                return new _3dObject
                {
                    ObjectId = state.ObjectId,
                    ObjectName = "Particle",
                    ObjectOffsets = new Vector3(),
                    WorldPosition = new Vector3(),
                    Rotation = new Vector3(),
                    ParentSurface = surface,
                    ObjectParts = new List<I3dObjectPart>
                    {
                        new _3dObjectPart
                        {
                            PartName = "Particle",
                            Triangles = new List<ITriangleMeshWithColor> { tri },
                            IsVisible = true
                        }
                    },
                    ImpactStatus = new ImpactStatus { ObjectName = "Particle" }
                };
            }

            return null;
        }

        private static Vector3 LerpVector(Vector3 a, Vector3 b, float t)
        {
            return new Vector3(
                a.x + (b.x - a.x) * t,
                a.y + (b.y - a.y) * t,
                a.z + (b.z - a.z) * t);
        }

        private static Vector3 LerpVectorNullable(Vector3? a, Vector3? b, float t)
        {
            var start = a ?? b ?? new Vector3();
            var end = b ?? a ?? new Vector3();
            return LerpVector(start, end, t);
        }

        private void LogFrameDelta(string summary, int frameIndex)
        {
            debugLogCounter++;
            if (debugLogCounter % 60 != 0)
                return;

            if (Logger.EnableFileLogging)
                Logger.Log($"Playback frame {frameIndex}: {summary}");
        }

        private sealed class ExplosionState
        {
            public Physics Physics { get; set; }
            public DateTime StartTime { get; set; }
            public _3dObject Target { get; set; }
        }

        private sealed class FrameCache
        {
            public ReplayIO.ReplayObjectState?[] StateByIdArray { get; set; } = Array.Empty<ReplayIO.ReplayObjectState?>();
            public int[] ObjectIdsWithNext { get; set; } = Array.Empty<int>();
            public bool[] ObjectIdMask { get; set; } = Array.Empty<bool>();
        }
    }
}
