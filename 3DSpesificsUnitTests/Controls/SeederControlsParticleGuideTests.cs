using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using Domain;
using GameAiAndControls.Controls.SeederControls;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.Controls;

[TestClass]
public class SeederControlsParticleGuideTests
{
    [TestInitialize]
    public void Setup()
    {
        GameState.SurfaceState = new SurfaceState
        {
            SurfaceViewportObject = new _3dObject
            {
                ObjectId = 1,
                ObjectName = "Surface",
                ObjectOffsets = new Vector3 { x = 105f, y = 500f, z = 400f },
                WorldPosition = new Vector3()
            }
        };
    }

    [TestMethod]
    public void ReleaseParticles_UsesSeederWorldPositionWithoutSurfaceXOffset()
    {
        var particles = new CapturingParticles();
        var seeder = new _3dObject
        {
            ObjectId = 2,
            ObjectName = "Seeder",
            WorldPosition = new Vector3 { x = 1000f, y = 0f, z = 2000f },
            ObjectOffsets = new Vector3 { x = 0f, y = -200f, z = 600f },
            Particles = particles
        };
        var controls = new SeederControls
        {
            ParentObject = seeder
        };
        controls.SetParticleGuideCoordinates(PointTriangle(0f, 31.2f, 0f), PointTriangle(0f, 131.2f, 0f));

        controls.ReleaseParticles(seeder);

        Assert.IsNotNull(particles.WorldPosition);
        Assert.AreEqual(1000f, particles.WorldPosition!.x, 0.001f,
            "A centered Seeder guide must not be shifted sideways by the surface viewport X offset.");
        Assert.AreEqual(2000f, particles.WorldPosition.z, 0.001f);
    }

    private static TriangleMeshWithColor PointTriangle(float x, float y, float z)
    {
        return new TriangleMeshWithColor
        {
            vert1 = new Vector3 { x = x, y = y, z = z },
            vert2 = new Vector3 { x = x, y = y, z = z },
            vert3 = new Vector3 { x = x, y = y, z = z },
            noHidden = true
        };
    }

    private sealed class CapturingParticles : IParticles
    {
        public IObjectMovement ParentShip { get; set; } = null!;
        public List<IParticle> Particles { get; set; } = new();
        public float LifeMultiplier { get; set; } = 1f;
        public int MaxParticlesOverride { get; set; }
        public Vector3? WorldPosition { get; private set; }

        public void ReleaseParticles(
            ITriangleMeshWithColor Trajectory,
            ITriangleMeshWithColor StartPosition,
            IVector3 WorldPosition,
            IObjectMovement ParentShip,
            int Thrust,
            bool? explosion,
            float upwardVelocityBoost = 0f)
        {
            this.WorldPosition = new Vector3
            {
                x = WorldPosition.x,
                y = WorldPosition.y,
                z = WorldPosition.z
            };
        }

        public void MoveParticles()
        {
        }
    }
}
