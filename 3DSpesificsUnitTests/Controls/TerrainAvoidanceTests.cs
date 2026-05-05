using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Controls.MotherShipSmallControls;
using GameAiAndControls.Helpers;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.Controls;

[TestClass]
public class TerrainAvoidanceTests
{
    [TestInitialize]
    public void Setup()
    {
        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState
        {
            GlobalMapPosition = new Vector3 { x = 0, y = 0, z = 0 },
            AiObjects = new List<_3dObject>()
        };
    }

    [TestMethod]
    public void TryStartTerrainRecovery_WhenAiObjectHitsSurface_ClearsCrashAndMovesUp()
    {
        var aiObject = CreateAiObject(1001, "KamikazeDrone", "Surface");
        GameState.SurfaceState.AiObjects.Add(aiObject);

        var started = TerrainAvoidanceHelpers.TryStartTerrainRecovery(aiObject);
        var originalY = aiObject.ObjectOffsets!.y;
        var originalX = aiObject.WorldPosition!.x;

        var applied = TerrainAvoidanceHelpers.ApplyTerrainRecovery(aiObject, 0.1f);

        Assert.IsTrue(started, "Terrain contact should start recovery for AIObjects.");
        Assert.IsTrue(applied, "Started recovery should apply movement.");
        Assert.IsFalse(aiObject.ImpactStatus!.HasCrashed, "Terrain recovery should consume the crash signal.");
        Assert.AreEqual(string.Empty, aiObject.ImpactStatus.ObjectName);
        Assert.IsTrue(aiObject.ObjectOffsets.y < originalY, "Recovery should lift the object away from the surface.");
        Assert.AreNotEqual(originalX, aiObject.WorldPosition.x, "Recovery should also steer horizontally away from terrain.");
    }

    [TestMethod]
    public void TryStartTerrainRecovery_WhenAiObjectHitsTower_UsesTowerAsTerrainObstacle()
    {
        var aiObject = CreateAiObject(1002, "ZeppelinBomber", "Tower");
        GameState.SurfaceState.AiObjects.Add(aiObject);

        var started = TerrainAvoidanceHelpers.TryStartTerrainRecovery(aiObject);

        Assert.IsTrue(started, "Towers should be treated as passive terrain obstacles for AI avoidance.");
        Assert.IsFalse(aiObject.ImpactStatus!.HasCrashed);
    }

    [TestMethod]
    public void TryStartTerrainRecovery_WhenObjectIsNotInAiObjects_DoesNotReact()
    {
        var nonAiObject = CreateAiObject(1003, "KamikazeDrone", "Surface");

        var started = TerrainAvoidanceHelpers.TryStartTerrainRecovery(nonAiObject);

        Assert.IsFalse(started, "Only objects registered in SurfaceState.AiObjects should react.");
        Assert.IsTrue(nonAiObject.ImpactStatus!.HasCrashed);
        Assert.AreEqual("Surface", nonAiObject.ImpactStatus.ObjectName);
    }

    [TestMethod]
    public void TryStartTerrainRecovery_WhenBomberBombHitsSurface_DoesNotReact()
    {
        var bomb = CreateAiObject(1004, "BomberBomb", "Surface");
        GameState.SurfaceState.AiObjects.Add(bomb);

        var started = TerrainAvoidanceHelpers.TryStartTerrainRecovery(bomb);

        Assert.IsFalse(started, "BomberBomb should keep its surface crash behavior.");
        Assert.IsTrue(bomb.ImpactStatus!.HasCrashed);
        Assert.AreEqual("Surface", bomb.ImpactStatus.ObjectName);
    }

    [TestMethod]
    public void MotherShipSmall_MoveObject_WhenSurfaceContactOccurs_DoesNotTakeDamage()
    {
        var motherShip = CreateAiObject(1005, "MotherShipSmall", "Surface");
        motherShip.ImpactStatus!.ObjectHealth = EnemySetup.MotherShipSmallHealth;
        motherShip.ObjectOffsets = new Vector3 { x = 0, y = 75f, z = 400f };
        GameState.SurfaceState.AiObjects.Add(motherShip);

        var controls = new MotherShipSmallControls();

        controls.MoveObject(motherShip, null, null);

        Assert.IsFalse(motherShip.ImpactStatus.HasCrashed, "MotherShip should consume terrain contact as avoidance, not damage.");
        Assert.AreEqual(EnemySetup.MotherShipSmallHealth, motherShip.ImpactStatus.ObjectHealth);
        Assert.IsTrue(motherShip.ObjectOffsets!.y < 75f, "MotherShip recovery should lift it away from the terrain.");
    }

    private static _3dObject CreateAiObject(int objectId, string objectName, string contactName)
    {
        return new _3dObject
        {
            ObjectId = objectId,
            ObjectName = objectName,
            IsActive = true,
            WorldPosition = new Vector3 { x = 1000f + objectId, y = 0f, z = 2000f },
            ObjectOffsets = new Vector3 { x = 0f, y = 100f, z = 400f },
            Rotation = new Vector3(),
            CrashBoxes = new List<List<IVector3>>
            {
                new()
                {
                    new Vector3 { x = -10f, y = -10f, z = -10f },
                    new Vector3 { x = 10f, y = 10f, z = 10f }
                }
            },
            ImpactStatus = new ImpactStatus
            {
                HasCrashed = true,
                ObjectName = contactName,
                ObjectHealth = 100
            },
            ObjectParts = new List<I3dObjectPart>
            {
                new _3dObjectPart
                {
                    PartName = "Body",
                    IsVisible = true,
                    Triangles = new List<ITriangleMeshWithColor>
                    {
                        new TriangleMeshWithColor
                        {
                            Color = "FFFFFF",
                            vert1 = new Vector3 { x = 0f, y = 0f, z = 0f },
                            vert2 = new Vector3 { x = 1f, y = 0f, z = 0f },
                            vert3 = new Vector3 { x = 0f, y = 1f, z = 0f }
                        }
                    }
                }
            }
        };
    }
}
