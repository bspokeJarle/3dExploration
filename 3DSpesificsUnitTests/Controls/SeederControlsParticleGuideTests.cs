using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
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
            AiObjects = new List<_3dObject>(),
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

    [TestMethod]
    public void HandleCrash_BulletRequiresThreeHitsToDestroySeeder()
    {
        var seeder = CreateSeederCollisionObject();
        var controls = new SeederControls();

        HitSeeder(controls, seeder, "Bullet");

        Assert.AreEqual(EnemySetup.SeederHealth - WeaponSetup.GetWeaponDamage("Bullet"), seeder.ImpactStatus!.ObjectHealth);
        Assert.IsFalse(seeder.ImpactStatus.HasCrashed, "Non-fatal bullet hit should be consumed after applying damage.");
        Assert.IsTrue(seeder.CrashBoxes.Count > 0);

        HitSeeder(controls, seeder, "Bullet");

        Assert.AreEqual(EnemySetup.SeederHealth - (WeaponSetup.GetWeaponDamage("Bullet") * 2), seeder.ImpactStatus.ObjectHealth);
        Assert.IsFalse(seeder.ImpactStatus.HasCrashed);
        Assert.IsTrue(seeder.CrashBoxes.Count > 0);

        HitSeeder(controls, seeder, "Bullet");

        Assert.IsTrue(seeder.ImpactStatus.ObjectHealth <= 0);
        Assert.AreEqual(0, seeder.CrashBoxes.Count, "Third bullet should destroy the seeder.");
    }

    [TestMethod]
    public void HandleCrash_LazerDestroysSeederInOneHit()
    {
        var seeder = CreateSeederCollisionObject();
        var controls = new SeederControls();

        HitSeeder(controls, seeder, "Lazer");

        Assert.IsTrue(seeder.ImpactStatus!.ObjectHealth <= 0);
        Assert.AreEqual(0, seeder.CrashBoxes.Count, "One lazer hit should destroy the seeder.");
    }

    private static void HitSeeder(SeederControls controls, _3dObject seeder, string weaponName)
    {
        seeder.ImpactStatus!.HasCrashed = true;
        seeder.ImpactStatus.ObjectName = weaponName;
        controls.HandleCrash(seeder);
    }

    private static _3dObject CreateSeederCollisionObject()
    {
        return new _3dObject
        {
            ObjectId = 10,
            ObjectName = "Seeder",
            WorldPosition = new Vector3 { x = 1000f, y = 0f, z = 2000f },
            ObjectOffsets = new Vector3 { x = 0f, y = -200f, z = 600f },
            Rotation = new Vector3(),
            CrashBoxes =
            [
                [
                    new Vector3 { x = -10f, y = -10f, z = -10f },
                    new Vector3 { x = 10f, y = 10f, z = 10f }
                ]
            ],
            ImpactStatus = new ImpactStatus { ObjectHealth = EnemySetup.SeederHealth },
            ObjectParts =
            [
                new _3dObjectPart
                {
                    PartName = "Body",
                    IsVisible = true,
                    Triangles =
                    [
                        PointTriangle(0f, 0f, 0f)
                    ]
                }
            ]
        };
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
