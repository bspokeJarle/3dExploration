using System;
using System.Collections.Generic;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using CommonUtilities.CommonGlobalState;
using Domain;
using GameplayHelpers.ReplayIO;
using _3dTesting._3dWorld;
using _3dTesting.MainWindowClasses.Loops;
using static Domain._3dSpecificsImplementations;
using _3dRotations.World.Objects;

namespace BenchmarkSuite1.Benchmarks;
[CPUUsageDiagnoser]
public class PlaybackGameLoopBenchmarks
{
    private PlaybackGameLoop _loop = null !;
    private I3dWorld _world = null !;
    private List<_3dTesting._Coordinates._2dTriangleMesh> _projected = null !;
    private List<_3dTesting._Coordinates._2dTriangleMesh> _crash = null !;
    private FieldInfo _framesField = null !;
    private FieldInfo _replayLoadedField = null !;
    private FieldInfo _fpsField = null !;
    private FieldInfo _playbackFramePositionField = null !;
    private FieldInfo _lastFrameTimeField = null !;
    [GlobalSetup]
    public void Setup()
    {
        _loop = new PlaybackGameLoop();
        var surface = new Surface();
        _world = new BenchmarkWorld(new List<I3dObject>
        {
            CreateObject(1, "Ship", surface, new DummyMovement()),
            CreateObject(2, "Surface", surface, new DummyMovement())
        });
        var frames = BuildFrames();
        _framesField = typeof(PlaybackGameLoop).GetField("frames", BindingFlags.NonPublic | BindingFlags.Instance)!;
        _replayLoadedField = typeof(PlaybackGameLoop).GetField("replayLoaded", BindingFlags.NonPublic | BindingFlags.Instance)!;
        _fpsField = typeof(PlaybackGameLoop).GetField("fps", BindingFlags.NonPublic | BindingFlags.Instance)!;
        _playbackFramePositionField = typeof(PlaybackGameLoop).GetField("playbackFramePosition", BindingFlags.NonPublic | BindingFlags.Instance)!;
        _lastFrameTimeField = typeof(PlaybackGameLoop).GetField("lastFrameTime", BindingFlags.NonPublic | BindingFlags.Instance)!;
        _framesField.SetValue(_loop, frames);
        _replayLoadedField.SetValue(_loop, true);
        _fpsField.SetValue(_loop, 60);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _projected = new List<_3dTesting._Coordinates._2dTriangleMesh>();
        _crash = new List<_3dTesting._Coordinates._2dTriangleMesh>();
        _playbackFramePositionField.SetValue(_loop, 0d);
        _lastFrameTimeField.SetValue(_loop, DateTime.UtcNow - TimeSpan.FromSeconds(1.0 / 60.0));
    }

    [Benchmark]
    public int UpdateWorld()
    {
        _loop.UpdateWorld(_world, ref _projected, ref _crash);
        return _projected.Count;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _projected.Clear();
        _crash.Clear();
    }

    private static List<ReplayIO.FrameState> BuildFrames()
    {
        var frames = new List<ReplayIO.FrameState>(120);
        for (int frame = 0; frame < 120; frame++)
        {
            frames.Add(new ReplayIO.FrameState { FrameIndex = frame, GlobalMapPosition = new Vector3(frame, frame * 0.5f, frame * 2f), RecordedObjectCount = 2, ObjectStates = new List<ReplayIO.ReplayObjectState> { new ReplayIO.ReplayObjectState { ObjectId = 1, ObjectName = "Ship", WorldPosition = new Vector3(0, 0, 0), ObjectOffset = new Vector3(10 + frame, 20, 30), Rotation = new Vector3(0, frame % 360, 0) }, new ReplayIO.ReplayObjectState { ObjectId = 2, ObjectName = "Surface", WorldPosition = new Vector3(0, 0, 0), ObjectOffset = new Vector3(40, 500, 300), Rotation = new Vector3(70, 0, 0) } } });
        }

        return frames;
    }

    private static _3dObject CreateObject(int id, string name, ISurface surface, IObjectMovement movement)
    {
        var tri = new TriangleMeshWithColor
        {
            Color = "FFFFFF",
            vert1 = new Vector3(-1, -1, 0),
            vert2 = new Vector3(1, -1, 0),
            vert3 = new Vector3(0, 1, 0)
        };

        return new _3dObject
        {
            ObjectId = id,
            ObjectName = name,
            ParentSurface = surface,
            ObjectOffsets = new Vector3(),
            WorldPosition = new Vector3(),
            Rotation = new Vector3(),
            Movement = movement,
            CrashBoxes = new List<List<IVector3>>(),
            ObjectParts = new List<I3dObjectPart>
            {
                new _3dObjectPart
                {
                    PartName = "Main",
                    IsVisible = true,
                    Triangles = new List<ITriangleMeshWithColor> { tri }
                }
            },
            ImpactStatus = new ImpactStatus()
        };
    }

    private sealed class DummyMovement : IObjectMovement
    {
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject ParentObject { get; set; } = null !;
        public IPhysics Physics { get; set; } = null !;

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
        }

        public void Dispose()
        {
        }

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry) => theObject;
        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
        }

        public void ReleaseParticles(I3dObject theObject)
        {
        }
    }

    private sealed class BenchmarkWorld : I3dWorld
    {
        public BenchmarkWorld(List<I3dObject> inhabitants)
        {
            WorldInhabitants = inhabitants;
        }

        public List<I3dObject> WorldInhabitants { get; set; }
        public ISceneHandler SceneHandler { get; set; } = new BenchmarkSceneHandler();
        public bool IsPaused { get; set; }
    }

    private sealed class BenchmarkSceneHandler : ISceneHandler
    {
        private readonly IScene _scene = new BenchmarkScene();
        public void SetupActiveScene(I3dWorld world)
        {
        }

        public void ResetActiveScene(I3dWorld world)
        {
        }

        public void NextScene(I3dWorld world)
        {
        }

        public IScene GetActiveScene() => _scene;
        public void HandleKeyPress(System.Windows.Input.KeyEventArgs k, I3dWorld world)
        {
        }
    }

    private sealed class BenchmarkScene : IScene
    {
        public SceneTypes SceneType => SceneTypes.Game;
        public GameModes GameMode => GameModes.Playback;
        public string SceneMusic => "music_flight";

        public void SetupScene(I3dWorld world)
        {
        }

        public void SetupSceneOverlay()
        {
        }

        public void SetupGameOverlay()
        {
        }
    }
}