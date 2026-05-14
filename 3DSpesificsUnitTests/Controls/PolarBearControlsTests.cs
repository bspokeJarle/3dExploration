using _3dRotations.World.Objects;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Controls;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.Controls;

[TestClass]
public class PolarBearControlsTests
{
    [TestInitialize]
    public void Setup()
    {
        ScreenSetup.Initialize(1500, 1024);
        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState
        {
            GlobalMapPosition = new Vector3 { x = 1000f, y = 500f, z = 1000f },
            SurfaceViewportObject = new _3dObject
            {
                ObjectId = 77,
                ObjectName = "Surface",
                ObjectOffsets = new Vector3 { x = 75f, y = 500f, z = 400f },
                WorldPosition = new Vector3(),
                Rotation = new Vector3(),
                ImpactStatus = new ImpactStatus()
            }
        };
        GameState.DeltaTime = 0f;
        GameState.ObjectIdCounter = 1;
    }

    [TestMethod]
    public void MoveObject_OnTurnPause_OpensMouthAndPlaysGrowlWithBearSpatialPosition()
    {
        var control = new PolarBearControls(-20f, 20f);
        var audio = new CapturingAudioPlayer();
        var registry = new FakeSoundRegistry();
        var bear = CreateBear();

        MoveOneFrame(control, bear, audio, registry);
        MoveOneFrame(control, bear, audio, registry);
        MoveOneFrame(control, bear, audio, registry);
        MoveOneFrame(control, bear, audio, registry);

        var preGrowlOffsets = new Vector3
        {
            x = bear.ObjectOffsets!.x,
            y = bear.ObjectOffsets.y,
            z = bear.ObjectOffsets.z
        };

        var lowerJaw = bear.ObjectParts.First(p => p.PartName == "PolarBearLowerJaw");
        float jawYBefore = lowerJaw.Triangles[0].vert1.y;
        float jawZBefore = lowerJaw.Triangles[0].vert1.z;

        MoveOneFrame(control, bear, audio, registry);

        Assert.AreEqual(1, audio.PlayCount, "Bear should growl once when mouth opens during turn pause.");
        Assert.IsNotNull(audio.LastWorldPosition);

        var expected = ExpectedAudioPosition(bear.WorldPosition!, preGrowlOffsets, GameState.SurfaceState.GlobalMapPosition);
        Assert.AreEqual(expected.x, audio.LastWorldPosition!.Value.X, 0.001f);
        Assert.AreEqual(expected.y, audio.LastWorldPosition.Value.Y, 0.001f);
        Assert.AreEqual(expected.z, audio.LastWorldPosition.Value.Z, 0.001f);

        float jawYAfter = lowerJaw.Triangles[0].vert1.y;
        float jawZAfter = lowerJaw.Triangles[0].vert1.z;
        Assert.IsTrue(
            MathF.Abs(jawYAfter - jawYBefore) > 0.01f || MathF.Abs(jawZAfter - jawZBefore) > 0.01f,
            "Lower jaw triangles should move when mouth opens.");

        control.Dispose();
    }

    [TestMethod]
    public void MoveObject_KeepsBearYOffsetJustAboveShadowBaseline()
    {
        var control = new PolarBearControls(-20f, 20f);
        var bear = CreateBear();

        MoveOneFrame(control, bear, null, null);

        Assert.AreEqual(400f, bear.ObjectOffsets!.z, 0.001f, "Bear should stay on the same depth layer as its shadow baseline.");
        Assert.IsTrue(
            bear.ObjectOffsets.y < 275.5f && bear.ObjectOffsets.y > 273.5f,
            $"Bear Y offset should stay close above shadow baseline (expected about 274-275). Actual: {bear.ObjectOffsets.y:0.###}");

        control.Dispose();
    }

    private static _3dObject CreateBear()
    {
        const int bearTileId = 1234;
        var surface = new Surface
        {
            RotatedSurfaceTriangles = new List<ITriangleMeshWithColor>
            {
                new TriangleMeshWithColor
                {
                    landBasedPosition = bearTileId,
                    vert1 = new Vector3 { x = 0f, y = -205f, z = 0f },
                    vert2 = new Vector3 { x = 20f, y = -205f, z = 0f },
                    vert3 = new Vector3 { x = 10f, y = -205f, z = 20f }
                }
            },
            RotatedSurfaceTriangleByLandId = new Dictionary<long, ITriangleMeshWithColor>
            {
                [bearTileId] = new TriangleMeshWithColor
                {
                    landBasedPosition = bearTileId,
                    vert1 = new Vector3 { x = 0f, y = -205f, z = 0f },
                    vert2 = new Vector3 { x = 20f, y = -205f, z = 0f },
                    vert3 = new Vector3 { x = 10f, y = -205f, z = 20f }
                }
            }
        };

        var bear = PolarBear.CreatePolarBear(surface);
        bear.ObjectId = 1001;
        bear.ObjectName = "PolarBear";
        bear.SurfaceBasedId = bearTileId;
        bear.WorldPosition = new Vector3 { x = 920f, y = 20f, z = 1180f };
        bear.ObjectOffsets = new Vector3 { x = 0f, y = 280f, z = 400f };
        bear.Rotation = new Vector3 { x = 70f, y = 0f, z = 0f };
        bear.ImpactStatus = new ImpactStatus();
        bear.IsOnScreen = true;
        bear.IsActive = true;
        return bear;
    }

    private static Vector3 ExpectedAudioPosition(IVector3 worldPosition, IVector3 objectOffsets, IVector3 globalMapPosition)
    {
        float localX = globalMapPosition.x - worldPosition.x;
        float localY = globalMapPosition.y - worldPosition.y;
        float localZ = globalMapPosition.z - worldPosition.z;

        return new Vector3
        {
            x = -localX + objectOffsets.x,
            y = -localY + objectOffsets.y,
            z = localZ + objectOffsets.z
        };
    }

    private static void MoveOneFrame(PolarBearControls control, I3dObject bear, IAudioPlayer? audio, ISoundRegistry? registry)
    {
        GameState.DeltaTime = 0.1f;
        control.MoveObject(bear, audio, registry);
    }

    private sealed class FakeSoundRegistry : ISoundRegistry
    {
        private readonly SoundDefinition _bearGrowl = new()
        {
            Id = "bear_growl",
            Usage = "BearGrowl",
            File = "OmegaStrain_Bear_Growl.wav"
        };

        public SoundDefinition Get(string id)
        {
            if (id == _bearGrowl.Id)
                return _bearGrowl;

            throw new KeyNotFoundException(id);
        }

        public bool TryGet(string id, out SoundDefinition definition)
        {
            if (id == _bearGrowl.Id)
            {
                definition = _bearGrowl;
                return true;
            }

            definition = null!;
            return false;
        }
    }

    private sealed class CapturingAudioPlayer : IAudioPlayer
    {
        public int PlayCount { get; private set; }
        public System.Numerics.Vector3? LastWorldPosition { get; private set; }

        public IAudioInstance Play(SoundDefinition definition, AudioPlayMode mode, AudioPlayOptions? options = null)
        {
            PlayCount++;
            LastWorldPosition = options?.WorldPosition;
            return new FakeAudioInstance(definition.Id, mode == AudioPlayMode.SegmentedLoop);
        }

        public void PlayOneShot(SoundDefinition definition, AudioPlayOptions? options = null)
        {
            PlayCount++;
            LastWorldPosition = options?.WorldPosition;
        }

        public void Stop(IAudioInstance instance, bool playEndSegment)
        {
        }

        public void StopAll()
        {
        }

        public void PlayMusic(SoundDefinition definition, float? volumeOverride = null)
        {
        }

        public void StopMusic()
        {
        }

        public void Update(double deltaTimeSeconds)
        {
        }
    }

    private sealed class FakeAudioInstance : IAudioInstance
    {
        public FakeAudioInstance(string soundId, bool isLooping)
        {
            Id = Guid.NewGuid();
            SoundId = soundId;
            IsLooping = isLooping;
            IsPlaying = true;
        }

        public Guid Id { get; }
        public string SoundId { get; }
        public bool IsPlaying { get; private set; }
        public bool IsLooping { get; }

        public void SetVolume(float volume)
        {
        }

        public void SetSpeed(float speed)
        {
        }

        public void SetWorldPosition(System.Numerics.Vector3 position)
        {
        }

        public void Stop(bool playEndSegment)
        {
            IsPlaying = false;
        }
    }
}
