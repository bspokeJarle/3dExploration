using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Controls;
using GameAiAndControls.Controls.KamikazeDroneControls;
using _3dTesting.Helpers;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.Controls;

[TestClass]
public class ExplosionParticleEffectsTests
{
    [TestInitialize]
    public void Setup()
    {
        GameState.GamePlayState = new GamePlayState();
        GameState.ShipState = new ShipState();
        GameState.SurfaceState = new SurfaceState
        {
            AiObjects = new List<_3dObject>(),
            GlobalMapPosition = new Vector3 { x = 0f, y = 0f, z = 0f }
        };
    }

    [TestMethod]
    public void CrashDetection_DecoyDoesNotCrashWithShip()
    {
        var ship = CreateCollisionObject("Ship", 7201);
        var decoy = CreateCollisionObject("DroneDecoy", 7202);

        CrashDetection.HandleCrashboxes(new List<_3dObject> { ship, decoy }, isPaused: false);

        Assert.IsFalse(ship.ImpactStatus!.HasCrashed, "Decoy should not collide with the player ship.");
        Assert.IsFalse(decoy.ImpactStatus!.HasCrashed, "Decoy should not be destroyed by touching the player ship.");
    }

    [TestMethod]
    public void DecoyExplosion_DirectHitExplodesWithoutWaitingForTimeout()
    {
        var decoy = CreateExplodingDecoy();
        var controls = new DecoyBeaconControls();

        controls.MoveObject(decoy, null, null);

        Assert.IsNotNull(decoy.Particles, "Decoy should keep its particle system during the explosion.");
        Assert.IsTrue(decoy.Particles.Particles.Count > 0, "Direct decoy hits should release explosion particles immediately.");
        Assert.AreEqual(0, decoy.CrashBoxes.Count, "Exploding decoy should clear crashboxes immediately so it cannot keep attracting collisions.");
        Assert.IsFalse(decoy.ImpactStatus!.HasCrashed, "Direct-hit explosion should consume the crash state immediately.");
    }

    [TestMethod]
    public void KamikazeDroneTargetsDecoyWorldPositionWithoutRenderOffsetZ()
    {
        GameState.GamePlayState.PowerUpsCollected = 1;

        var decoy = CreateNavigationDecoy();
        GameState.SurfaceState.AiObjects.Add(decoy);

        var drone = CreateHuntingDrone();
        var controls = new KamikazeDroneControls
        {
            StartHuntDateTime = DateTime.Now.AddSeconds(-1)
        };

        controls.MoveObject(drone, null, null);
        SetPrivateDateTime(controls, "LastMovementDateTime", DateTime.Now.AddSeconds(-1));

        controls.MoveObject(drone, null, null);

        Assert.AreEqual(
            drone.WorldPosition!.x,
            drone.WorldPosition.z,
            2f,
            "Decoy render Z offset should not pull the drone toward a different navigation Z than the decoy world position.");
    }

    [TestMethod]
    public void KamikazeDroneExplosion_ReleasesVisibleExplosionParticles()
    {
        var drone = CreateExplodingDrone();
        var controls = new KamikazeDroneControls
        {
            StartHuntDateTime = DateTime.Now.AddSeconds(-1)
        };

        controls.MoveObject(drone, null, null);

        Assert.IsNotNull(drone.Particles, "Exploding AI objects should keep their particle system during the explosion.");
        Assert.IsTrue(drone.Particles.Particles.Count > 0, "Explosion should release the particle stream.");
        Assert.IsTrue(drone.Particles.Particles.Any(particle => particle.Visible), "Explosion particles should be advanced immediately so they render on the explosion frame.");
    }

    private static void SetPrivateDateTime(object instance, string fieldName, DateTime value)
    {
        var field = instance.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(field, $"Expected private field {fieldName} to exist.");
        field.SetValue(instance, value);
    }

    private static _3dObject CreateExplodingDecoy()
    {
        return new _3dObject
        {
            ObjectId = 7101,
            ObjectName = "DroneDecoy",
            IsOnScreen = true,
            ParentSurface = null,
            WorldPosition = new Vector3 { x = 100f, y = 0f, z = 100f },
            ObjectOffsets = new Vector3 { x = -200f, y = 0f, z = 800f },
            Rotation = new Vector3 { x = 0f, y = 0f, z = 0f },
            Particles = new ParticlesAI(),
            CrashBoxes = CreateCrashBoxes(),
            ImpactStatus = new ImpactStatus
            {
                HasCrashed = true,
                ObjectName = "KamikazeDrone",
                ObjectHealth = 1
            },
            ObjectParts = CreateSimpleObjectParts()
        };
    }

    private static _3dObject CreateCollisionObject(string objectName, int objectId)
    {
        return new _3dObject
        {
            ObjectId = objectId,
            ObjectName = objectName,
            IsOnScreen = true,
            WorldPosition = new Vector3 { x = 0f, y = 0f, z = 0f },
            ObjectOffsets = new Vector3 { x = 0f, y = 0f, z = 0f },
            Rotation = new Vector3 { x = 0f, y = 0f, z = 0f },
            CrashBoxes = CreateCrashBoxes(),
            ImpactStatus = new ImpactStatus { ObjectName = objectName, ObjectHealth = 100 },
            ObjectParts = CreateSimpleObjectParts()
        };
    }

    private static _3dObject CreateNavigationDecoy()
    {
        return new _3dObject
        {
            ObjectId = 7102,
            ObjectName = "DroneDecoy",
            IsOnScreen = true,
            ParentSurface = null,
            WorldPosition = new Vector3 { x = 300f, y = 0f, z = 300f },
            ObjectOffsets = new Vector3 { x = 0f, y = 0f, z = 1000f },
            Rotation = new Vector3 { x = 0f, y = 0f, z = 0f },
            Particles = new ParticlesAI(),
            CrashBoxes = CreateCrashBoxes(),
            ImpactStatus = new ImpactStatus(),
            ObjectParts = CreateSimpleObjectParts()
        };
    }

    private static _3dObject CreateHuntingDrone()
    {
        return new _3dObject
        {
            ObjectId = 7103,
            ObjectName = "KamikazeDrone",
            IsOnScreen = true,
            ParentSurface = null,
            WorldPosition = new Vector3 { x = 0f, y = 0f, z = 0f },
            ObjectOffsets = new Vector3 { x = 0f, y = 0f, z = 0f },
            Rotation = new Vector3 { x = 0f, y = 0f, z = 0f },
            Particles = new ParticlesAI(),
            CrashBoxes = CreateCrashBoxes(),
            ImpactStatus = new ImpactStatus { ObjectHealth = EnemySetup.KamikazeDroneHealth },
            ObjectParts = CreateSimpleObjectParts()
        };
    }

    private static _3dObject CreateExplodingDrone()
    {
        return new _3dObject
        {
            ObjectId = 7001,
            ObjectName = "KamikazeDrone",
            IsOnScreen = true,
            ParentSurface = null,
            WorldPosition = new Vector3 { x = 100f, y = 0f, z = 100f },
            ObjectOffsets = new Vector3 { x = 0f, y = 0f, z = 0f },
            Rotation = new Vector3 { x = 0f, y = 0f, z = 0f },
            Particles = new ParticlesAI(),
            CrashBoxes = new List<List<IVector3>>(),
            ImpactStatus = new ImpactStatus
            {
                HasCrashed = true,
                ObjectName = "Ship",
                ObjectHealth = 1
            },
            ObjectParts = CreateSimpleObjectParts()
        };
    }

    private static List<List<IVector3>> CreateCrashBoxes()
    {
        return
        [
            [
                new Vector3 { x = -5f, y = -5f, z = -5f },
                new Vector3 { x = 5f, y = 5f, z = 5f }
            ]
        ];
    }

    private static List<I3dObjectPart> CreateSimpleObjectParts()
    {
        return
        [
            new _3dObjectPart
            {
                PartName = "Body",
                IsVisible = true,
                Triangles =
                [
                    new TriangleMeshWithColor
                    {
                        Color = "ffffff",
                        vert1 = new Vector3 { x = -10f, y = 0f, z = 0f },
                        vert2 = new Vector3 { x = 10f, y = 0f, z = 0f },
                        vert3 = new Vector3 { x = 0f, y = 20f, z = 0f }
                    }
                ]
            }
        ];
    }
}
