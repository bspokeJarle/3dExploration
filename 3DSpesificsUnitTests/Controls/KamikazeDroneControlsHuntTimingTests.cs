using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using Domain;
using GameAiAndControls.Controls.KamikazeDroneControls;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.Controls;

[TestClass]
public class KamikazeDroneControlsHuntTimingTests
{
    [TestInitialize]
    public void Setup()
    {
        GameState.GamePlayState = new GamePlayState
        {
            PowerUpsCollected = 1
        };
        GameState.SurfaceState = new SurfaceState
        {
            GlobalMapPosition = new Vector3 { x = 0, y = 0, z = 0 }
        };
        GameState.ShipState = new ShipState
        {
            ShipCrashCenterWorldPosition = new Vector3 { x = 1000, y = 0, z = 1000 }
        };
    }

    [TestMethod]
    public void MoveObject_BeforeStartHuntDate_WithOnlyDrones_StartsHuntImmediately()
    {
        var control = new KamikazeDroneControls
        {
            StartHuntDateTime = DateTime.Now.AddMinutes(5)
        };

        var drone = CreateDrone(100, 0, 0);
        var otherDrone = CreateDrone(200, 2000, 2000);

        GameState.SurfaceState.AiObjects.Add(drone);
        GameState.SurfaceState.AiObjects.Add(otherDrone);

        control.MoveObject(drone, null, null);
        var before = (Vector3)drone.WorldPosition;

        Thread.Sleep(20);

        control.MoveObject(drone, null, null);
        var after = (Vector3)drone.WorldPosition;

        Assert.IsTrue(after.x != before.x || after.y != before.y || after.z != before.z,
            "Drone should start hunting immediately when no non-drone enemies are alive.");
    }

    [TestMethod]
    public void MoveObject_BeforeStartHuntDate_WithLiveSeeder_StillWaitsForDelay()
    {
        GameState.ShipState!.ShipCrashCenterWorldPosition = new Vector3 { x = 50000, y = 0, z = 50000 };

        var control = new KamikazeDroneControls
        {
            StartHuntDateTime = DateTime.Now.AddMinutes(5)
        };

        var drone = CreateDrone(300, 0, 0);
        var liveSeeder = new _3dObject
        {
            ObjectId = 301,
            ObjectName = "Seeder",
            IsActive = true,
            WorldPosition = new Vector3 { x = 5000, y = 0, z = 5000 },
            ObjectOffsets = new Vector3 { x = 0, y = 0, z = 0 },
            Rotation = new Vector3(),
            CrashBoxes = new List<List<IVector3>> { new() { new Vector3(), new Vector3 { x = 1, y = 1, z = 1 } } }
        };

        GameState.SurfaceState.AiObjects.Add(drone);
        GameState.SurfaceState.AiObjects.Add(liveSeeder);

        control.MoveObject(drone, null, null);
        var before = (Vector3)drone.WorldPosition;

        Thread.Sleep(20);

        control.MoveObject(drone, null, null);
        var after = (Vector3)drone.WorldPosition;

        Assert.AreEqual(before.x, after.x, 0.0001, "Drone should still wait for hunt delay while live non-drone enemies remain.");
        Assert.AreEqual(before.y, after.y, 0.0001, "Drone should still wait for hunt delay while live non-drone enemies remain.");
        Assert.AreEqual(before.z, after.z, 0.0001, "Drone should still wait for hunt delay while live non-drone enemies remain.");
    }

    [TestMethod]
    public void MoveObject_BeforeStartHuntDate_WithLiveSpaceSwan_StartsHuntImmediately()
    {
        var control = new KamikazeDroneControls
        {
            StartHuntDateTime = DateTime.Now.AddMinutes(5)
        };

        var drone = CreateDrone(400, 0, 0);
        var swan = new _3dObject
        {
            ObjectId = 401,
            ObjectName = "SpaceSwan",
            IsActive = true,
            WorldPosition = new Vector3 { x = 5000, y = 0, z = 5000 },
            ObjectOffsets = new Vector3 { x = 0, y = 0, z = 0 },
            Rotation = new Vector3(),
            CrashBoxes = new List<List<IVector3>> { new() { new Vector3(), new Vector3 { x = 1, y = 1, z = 1 } } }
        };

        GameState.SurfaceState.AiObjects.Add(drone);
        GameState.SurfaceState.AiObjects.Add(swan);

        control.MoveObject(drone, null, null);
        var before = (Vector3)drone.WorldPosition;

        Thread.Sleep(20);

        control.MoveObject(drone, null, null);
        var after = (Vector3)drone.WorldPosition;

        Assert.IsTrue(after.x != before.x || after.y != before.y || after.z != before.z,
            "Drone should start hunting immediately even if non-essential enemies like swans exist.");
    }

    [TestMethod]
    public void MoveObject_BeforeStartHuntDate_WithLiveZeppelinBomber_StartsHuntImmediately()
    {
        var control = new KamikazeDroneControls
        {
            StartHuntDateTime = DateTime.Now.AddMinutes(5)
        };

        var drone = CreateDrone(500, 0, 0);
        var zeppelin = new _3dObject
        {
            ObjectId = 501,
            ObjectName = "ZeppelinBomber",
            IsActive = true,
            WorldPosition = new Vector3 { x = 5000, y = 0, z = 5000 },
            ObjectOffsets = new Vector3 { x = 0, y = 0, z = 0 },
            Rotation = new Vector3(),
            CrashBoxes = new List<List<IVector3>> { new() { new Vector3(), new Vector3 { x = 1, y = 1, z = 1 } } }
        };

        GameState.SurfaceState.AiObjects.Add(drone);
        GameState.SurfaceState.AiObjects.Add(zeppelin);

        control.MoveObject(drone, null, null);
        var before = (Vector3)drone.WorldPosition;

        Thread.Sleep(20);

        control.MoveObject(drone, null, null);
        var after = (Vector3)drone.WorldPosition;

        Assert.IsTrue(after.x != before.x || after.y != before.y || after.z != before.z,
            "Drone should start hunting immediately even if non-essential enemies like zeppelins exist.");
    }

    private static _3dObject CreateDrone(int id, float x, float z)
    {
        return new _3dObject
        {
            ObjectId = id,
            ObjectName = "KamikazeDrone",
            IsActive = true,
            WorldPosition = new Vector3 { x = x, y = 0, z = z },
            ObjectOffsets = new Vector3 { x = 0, y = 0, z = 0 },
            Rotation = new Vector3(),
            CrashBoxes = new List<List<IVector3>> { new() { new Vector3(), new Vector3 { x = 1, y = 1, z = 1 } } }
        };
    }
}
