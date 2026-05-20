using _3dTesting._3dWorld;
using _3dTesting._3dRotation;
using _3dTesting._Coordinates;
using _3dTesting.Rendering;
using _3DWorld.Scene;
using _3dRotations.World.Objects;
using _3dRotations.World.Objects.EarthObject;
using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Controls;
using System.Windows.Media;
using NumericsVector3 = System.Numerics.Vector3;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.SceneManagement;

[TestClass]
public class OutroSceneTests
{
    private const int ExpectedOutroStarCount = 250;
    private const float ExpectedStarFieldRadius = 560f;

    [TestInitialize]
    public void Setup()
    {
        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState();
        GameState.ScreenOverlayState = new ScreenOverlayState();
        GameState.WorldFade = new WorldFadeState();
        GameState.ObjectIdCounter = 0;
    }

    private static _3dWorld CreateWorldAtOutro()
    {
        GameState.GamePlayState.SceneIndex = 9;
        var handler = new SceneHandler();
        var world = new _3dWorld();
        world.SceneHandler = handler;
        world.WorldInhabitants.Clear();
        handler.SetupActiveScene(world);
        return world;
    }

    [TestMethod]
    public void OutroScene_IsActiveAtIndexNine()
    {
        GameState.GamePlayState.SceneIndex = 9;
        var handler = new SceneHandler();
        var scene = handler.GetActiveScene();
        Assert.AreEqual(SceneTypes.Outro, scene.SceneType, "Scene at index 9 should be Outro.");
    }

    [TestMethod]
    public void OutroScene_HasEarthObject()
    {
        var world = CreateWorldAtOutro();
        var earth = world.WorldInhabitants.FirstOrDefault(o => o.ObjectName == "Earth");
        Assert.IsNotNull(earth, "Outro should contain an Earth object.");
    }

    [TestMethod]
    public void OutroScene_EarthIsActive()
    {
        var world = CreateWorldAtOutro();
        var earth = world.WorldInhabitants.First(o => o.ObjectName == "Earth");
        Assert.IsTrue(earth.IsActive, "Earth should be active in the Outro.");
    }

    [TestMethod]
    public void OutroScene_EarthHasMovementController()
    {
        var world = CreateWorldAtOutro();
        var earth = world.WorldInhabitants.First(o => o.ObjectName == "Earth");
        Assert.IsNotNull(earth.Movement, "Earth should have a movement controller assigned.");
    }

    [TestMethod]
    public void OutroScene_EarthHasCrashBoxes()
    {
        var world = CreateWorldAtOutro();
        var earth = world.WorldInhabitants.First(o => o.ObjectName == "Earth");
        Assert.IsTrue(earth.CrashBoxes?.Count > 0, "Earth should have crash boxes.");
    }

    [TestMethod]
    public void OutroScene_EarthGlobePart_IsVisible()
    {
        var world = CreateWorldAtOutro();
        var earth = world.WorldInhabitants.First(o => o.ObjectName == "Earth");
        var globe = earth.ObjectParts.FirstOrDefault(p => p.PartName == "EarthGlobe");

        Assert.IsNotNull(globe, "Earth should contain an EarthGlobe part.");
        Assert.IsTrue(globe.IsVisible, "EarthGlobe must be visible or the renderer skips all Earth triangles.");
    }

    [TestMethod]
    public void OutroScene_EarthGlobeTriangleCount_UsesCompleteGlbModel()
    {
        var world = CreateWorldAtOutro();
        var earth = world.WorldInhabitants.First(o => o.ObjectName == "Earth");
        var globe = earth.ObjectParts.FirstOrDefault(p => p.PartName == "EarthGlobe");

        Assert.IsNotNull(globe, "Earth should contain the imported GLB model as EarthGlobe.");
        Assert.AreEqual(EarthModelData.TriangleCount, globe.Triangles.Count,
            "Earth should use the complete baked low_poly_earth.glb triangle set.");
        Assert.AreEqual(1280, EarthModelData.TriangleCount, "The imported source model should keep all GLB triangles.");
        Assert.AreEqual(642, EarthModelData.SourceVertexCount, "The imported source model should keep the GLB vertex count metadata.");
    }

    [TestMethod]
    public void OutroScene_EarthGlobeTriangles_UseBackfaceCulling()
    {
        var world = CreateWorldAtOutro();
        var earth = world.WorldInhabitants.First(o => o.ObjectName == "Earth");
        var globe = earth.ObjectParts.FirstOrDefault(p => p.PartName == "EarthGlobe");

        Assert.IsNotNull(globe, "Earth should contain an EarthGlobe part.");
        Assert.IsTrue(globe.Triangles.All(t => t.noHidden != true),
            "Outro Earth must use backface culling; rendering every hidden sphere face is too expensive.");
    }

    [TestMethod]
    public void OutroScene_EarthGlobePalette_UsesDarkOceanGreenTerrainAndGrayMountains()
    {
        var world = CreateWorldAtOutro();
        var earth = world.WorldInhabitants.First(o => o.ObjectName == "Earth");
        var colors = earth.ObjectParts
            .Where(p => p.PartName == "EarthGlobe")
            .SelectMany(p => p.Triangles)
            .Select(t => ParseColor(t.Color))
            .ToList();

        var blueColors = colors.Where(IsBlueDominant).Distinct().ToList();
        var greenTerrainColors = colors.Where(IsLandColor).Distinct().ToList();
        var mountainColors = colors.Where(IsMountainGray).Distinct().OrderBy(c => c.R).ToList();

        Assert.AreEqual(1, blueColors.Count, "Only the ocean should use blue in the Earth model.");
        Assert.IsTrue(IsDarkOceanColor(blueColors[0]), "The single ocean model color should be dark blue.");
        Assert.IsTrue(greenTerrainColors.Count >= 2,
            $"Mid terrain should include green height variation. Green color count was {greenTerrainColors.Count}.");
        Assert.IsTrue(mountainColors.Count >= 2,
            $"High terrain should include multiple gray levels. Gray color count was {mountainColors.Count}.");
        Assert.IsTrue(mountainColors.Last().R > mountainColors.First().R,
            "Higher mountain levels should become lighter gray.");
        Assert.IsTrue(colors.Count(IsLandColor) > EarthModelData.TriangleCount * 0.15f,
            "Earth should contain broad readable land areas, not just scattered green specks.");
    }

    [TestMethod]
    public void OutroScene_StylizedEarthColor_UsesHeightBandsForTerrain()
    {
        var centralEurope = ParseColor(EarthObject.GetStylizedEarthColor(CreateSpherePoint(52f, 15f)));
        var scandinavia = ParseColor(EarthObject.GetStylizedEarthColor(CreateSpherePoint(67f, 21f)));
        var northAtlantic = ParseColor(EarthObject.GetStylizedEarthColor(CreateSpherePoint(40f, -35f)));
        var midTerrain = ParseColor(EarthObject.GetStylizedEarthColor(CreateSpherePoint(0f, -120f, 186f)));
        var lowMountain = ParseColor(EarthObject.GetStylizedEarthColor(CreateSpherePoint(0f, -120f, 188.5f)));
        var highMountain = ParseColor(EarthObject.GetStylizedEarthColor(CreateSpherePoint(0f, -120f, 190.2f)));

        Assert.IsTrue(centralEurope.G > centralEurope.R + 25 && centralEurope.G > centralEurope.B + 10,
            "Central Europe should read as land.");
        Assert.IsTrue(scandinavia.G > scandinavia.R + 20 && scandinavia.G > scandinavia.B,
            "Scandinavia/Nordic region should read as land.");
        Assert.IsTrue(IsDarkOceanColor(northAtlantic),
            "Open Atlantic should remain the fixed dark-blue ocean color.");
        Assert.IsTrue(IsLandColor(midTerrain),
            "Middle height terrain should be green, even outside the broad land masks.");
        Assert.IsTrue(IsMountainGray(lowMountain),
            "High terrain should become gray instead of blue.");
        Assert.IsTrue(IsMountainGray(highMountain) && highMountain.R > lowMountain.R,
            "The highest terrain should be lighter gray than lower mountains.");
    }

    [TestMethod]
    public void OutroScene_EarthContainsGlobeAndStarParts()
    {
        var world = CreateWorldAtOutro();
        var earth = world.WorldInhabitants.First(o => o.ObjectName == "Earth");
        var globeParts = earth.ObjectParts.Where(p => p.PartName == "EarthGlobe").ToList();
        var starParts = earth.ObjectParts.Where(IsOutroStarPart).ToList();
        var unexpectedParts = earth.ObjectParts
            .Where(p => p.PartName != "EarthGlobe"
                     && !IsOutroStarPart(p)
                     && !IsMiniaturePart(p))
            .ToList();

        var miniatureParts = earth.ObjectParts.Where(IsMiniaturePart).ToList();

        Assert.AreEqual(1, globeParts.Count, "Earth should contain exactly one imported GLB globe part.");
        Assert.AreEqual(ExpectedOutroStarCount, starParts.Count, "Outro Earth should contain the requested star count.");
        Assert.IsTrue(miniatureParts.Count > 0, "Earth should contain surface miniatures (trees, houses, igloos).");
        Assert.AreEqual(0, unexpectedParts.Count,
            $"Earth contains unexpected parts. Found: {string.Join(", ", unexpectedParts.Select(p => p.PartName))}");
    }

    [TestMethod]
    public void OutroScene_EarthStarField_HasRequestedRadius()
    {
        var world = CreateWorldAtOutro();
        var earth = world.WorldInhabitants.First(o => o.ObjectName == "Earth");
        var starCenters = earth.ObjectParts.Where(IsOutroStarPart).Select(GetPartCenter).ToList();

        Assert.AreEqual(ExpectedOutroStarCount, starCenters.Count);

        float averageRadius = starCenters.Average(v => MathF.Sqrt((v.x * v.x) + (v.y * v.y) + (v.z * v.z)));
        Assert.IsTrue(averageRadius >= ExpectedStarFieldRadius - 3f && averageRadius <= ExpectedStarFieldRadius + 3f,
            $"Star field should be roughly doubled to radius {ExpectedStarFieldRadius}. Average radius was {averageRadius:0.0}.");
    }

    [TestMethod]
    public void OutroScene_EarthStars_HaveVariedLocalRotations()
    {
        var world = CreateWorldAtOutro();
        var earth = world.WorldInhabitants.First(o => o.ObjectName == "Earth");

        var distinctAngles = earth.ObjectParts
            .Where(IsOutroStarPart)
            .Select(GetFirstTriangleBaseAngleBucket)
            .Distinct()
            .Count();

        Assert.IsTrue(distinctAngles > 50,
            $"Stars should have varied local rotations. Distinct angle buckets: {distinctAngles}.");
    }

    [TestMethod]
    public void OutroScene_EarthGlobeVisibleTriangles_AreCulledToRoughlyFrontHalf()
    {
        var world = CreateWorldAtOutro();
        var earth = (_3dObject)world.WorldInhabitants.First(o => o.ObjectName == "Earth");
        ApplyLiveMeshRotation(earth);

        int triangleCount = earth.ObjectParts.Sum(p => p.Triangles.Count);
        var converter = new _3dTo2d();
        var projected = converter.ConvertTo2dFromObjects(new List<_3dObject> { earth }, currentFrame: 1);

        // Stars carry noHidden=true and always project; globe and miniatures use backface culling.
        int starTriangleCount = earth.ObjectParts.Where(IsOutroStarPart).Sum(p => p.Triangles.Count);
        int cullableCount = triangleCount - starTriangleCount;

        Assert.IsTrue(projected.Count > starTriangleCount + cullableCount * 0.25f,
            $"Earth should still have enough visible front-side triangles. Projected {projected.Count} of {triangleCount}.");
        Assert.IsTrue(projected.Count < starTriangleCount + cullableCount * 0.75f,
            $"Earth should cull hidden back-side triangles. Projected {projected.Count} of {triangleCount}.");
    }

    [TestMethod]
    public void OutroScene_EarthOffsets_AreOnScreen()
    {
        var world = CreateWorldAtOutro();
        var earth = world.WorldInhabitants.First(o => o.ObjectName == "Earth");
        var offsets = earth.ObjectOffsets;

        Assert.IsNotNull(offsets, "Earth should have ObjectOffsets.");
        // Z offset should be positive (zoomed in, same layer as ship)
        Assert.IsTrue(offsets.z > 0, $"Earth Z offset should be positive (zoom), got {offsets.z}.");
        // X offset 0 = horizontally centered
        Assert.AreEqual(0f, offsets.x, $"Earth should be horizontally centered (x=0), got {offsets.x}.");
        Assert.AreEqual(0f, offsets.y, $"Earth should be vertically centered (y=0), got {offsets.y}.");
    }

    [TestMethod]
    public void OutroScene_EarthWorldPosition_IsAlwaysVisibleOrigin()
    {
        var world = CreateWorldAtOutro();
        var earth = (_3dObject)world.WorldInhabitants.First(o => o.ObjectName == "Earth");

        Assert.IsNotNull(earth.WorldPosition, "Earth must have a WorldPosition so the render loop can test visibility.");
        Assert.AreEqual(0f, earth.WorldPosition.x);
        Assert.AreEqual(0f, earth.WorldPosition.y);
        Assert.AreEqual(0f, earth.WorldPosition.z);
        Assert.IsTrue(earth.CheckInhabitantVisibility(), "Earth should render like Intro objects without needing a Surface.");
    }

    [TestMethod]
    public void OutroScene_HasShipWithOutroControls()
    {
        var world = CreateWorldAtOutro();
        var ship = world.WorldInhabitants.FirstOrDefault(o => o.ObjectName == "Ship");

        Assert.IsNotNull(ship, "Outro should contain a Ship object.");
        Assert.IsInstanceOfType(ship.Movement, typeof(OutroShipControls),
            "Outro ship should use the cinematic controller, not player input controls.");
        Assert.IsInstanceOfType(ship.Particles, typeof(ParticlesAI),
            "Outro ship should keep the normal particle system for engine flames.");
        Assert.IsFalse(ship.HasShadow, "Space outro ship should not cast a terrain shadow.");
        Assert.IsTrue(ship.ObjectParts.Any(p => p.PartName == "JetMotor"), "Ship should keep the lower engine emitter.");
        Assert.IsTrue(ship.ObjectParts.Any(p => p.PartName == "RearEngine"), "Ship should keep the rear engine emitter.");
    }

    [TestMethod]
    public void OutroShipControls_FirstMove_StartsShipFromRightSide()
    {
        var world = CreateWorldAtOutro();
        var ship = world.WorldInhabitants.First(o => o.ObjectName == "Ship");

        ship.Movement!.MoveObject(ship, null, null);

        Assert.IsTrue(ship.ObjectOffsets!.x > ScreenSetup.screenSizeX * 0.35f,
            $"Outro ship should enter from the right side. X offset was {ship.ObjectOffsets.x:0.0}.");
        Assert.IsTrue(ship.ObjectOffsets.x < ScreenSetup.screenSizeX * 0.5f,
            $"Outro ship should start like the Intro object: right side but already renderable. X offset was {ship.ObjectOffsets.x:0.0}.");
        Assert.IsTrue(ship.ObjectOffsets.z < 520f,
            $"Outro ship should start in front of Earth (depth < Earth's 520). Z offset was {ship.ObjectOffsets.z:0.0}.");
        Assert.AreEqual(70f, ship.Rotation!.x, "Outro ship should keep the same camera tilt as the rest of the scene.");
    }

    [TestMethod]
    public void OutroShipControls_FirstMove_ProjectsVisibleShipTriangles()
    {
        var world = CreateWorldAtOutro();
        var ship = (_3dObject)world.WorldInhabitants.First(o => o.ObjectName == "Ship");

        ship.Movement!.MoveObject(ship, null, null);
        ApplyLiveMeshRotation(ship);

        var converter = new _3dTo2d();
        var projected = converter.ConvertTo2dFromObjects(new List<_3dObject> { ship }, currentFrame: 1);

        Assert.IsTrue(projected.Count > 0, "Outro ship should be visible on the first rendered movement frame.");
        Assert.IsTrue(projected.Any(t => t.PartName == "UpperPart" || t.PartName == "LowerPart" || t.PartName == "RearPart"),
            "Projected triangles should include the ship hull, not only helper effects.");
    }

    [TestMethod]
    public void OutroShipControls_ApproachPhase_FliesFromRightToCenterBeforeDiving()
    {
        var now = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);
        var controls = new OutroShipControls(() => now);
        var ship = Ship.CreateShip(null, controls);
        ship.ObjectName = "Ship";
        ship.WorldPosition = new Vector3 { x = 0f, y = 0f, z = 0f };

        controls.MoveObject(ship, null, null);
        float startX = ship.ObjectOffsets!.x;
        float startY = ship.ObjectOffsets.y;
        float startZ = ship.ObjectOffsets.z;

        now = now.AddSeconds(2.5);
        controls.MoveObject(ship, null, null);

        Assert.IsTrue(ship.ObjectOffsets!.x > 0f && ship.ObjectOffsets.x < startX,
            $"Approach phase should move laterally from the right toward center. Start x={startX:0.0}, approach x={ship.ObjectOffsets.x:0.0}.");
        Assert.IsTrue(ship.ObjectOffsets.y > startY && ship.ObjectOffsets.y < 0f,
            $"Approach phase should move vertically toward center without overshooting. Start y={startY:0.0}, approach y={ship.ObjectOffsets.y:0.0}.");
        Assert.AreEqual(startZ, ship.ObjectOffsets.z, 1f,
            "Approach phase should not dive inward; depth must stay stable until the ship reaches center.");

        now = now.AddSeconds(0.7);
        controls.MoveObject(ship, null, null);

        Assert.AreEqual(0f, ship.ObjectOffsets!.x, 5f,
            "Ship should reach screen center before the inward dive becomes visible.");
        Assert.AreEqual(0f, ship.ObjectOffsets.y, 5f,
            "Ship should reach Earth center vertically before the inward dive becomes visible.");
        Assert.IsTrue(ship.ObjectOffsets.z > startZ,
            "Dive phase should start only after the approach phase reaches center.");
    }

    [TestMethod]
    public void OutroShipControls_FliesTowardEarthByIncreasingDepth()
    {
        var now = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);
        var controls = new OutroShipControls(() => now);
        var ship = Ship.CreateShip(null, controls);
        ship.ObjectName = "Ship";
        ship.WorldPosition = new Vector3 { x = 0f, y = 0f, z = 0f };

        controls.MoveObject(ship, null, null);
        float startZ = ship.ObjectOffsets!.z;

        now = now.AddSeconds(5.0);
        controls.MoveObject(ship, null, null);

        Assert.IsTrue(ship.ObjectOffsets!.z > startZ + 700f,
            $"Ship should move toward Earth by increasing z offset. Start z={startZ:0.0}, final z={ship.ObjectOffsets.z:0.0}.");
        Assert.AreEqual(1000f, ship.ObjectOffsets.z, 1f,
            $"Ship should finish at the requested perspective depth. Final z={ship.ObjectOffsets.z:0.0}.");
        Assert.IsTrue(ship.ZSortBias > 0f,
            "Ship should stay rendered in front of Earth via sort bias, not by limiting the perspective depth.");
        Assert.AreEqual(0f, ship.ObjectOffsets.x, 3f, "Ship should fly into the center of Earth on screen.");
        Assert.AreEqual(0f, ship.ObjectOffsets.y, 3f, "Ship should fly into the center of Earth on screen.");
    }

    [TestMethod]
    public void OutroShipControls_TargetDepthShrinksProjectedShip()
    {
        var startBounds = ProjectShipHullBoundsAfter(seconds: 0.01);
        var targetBounds = ProjectShipHullBoundsAfter(seconds: 5.0);

        Assert.IsTrue(targetBounds.Width < startBounds.Width * 0.50f,
            $"Ship should visibly shrink as z offset increases. Start width={startBounds.Width}px, target width={targetBounds.Width}px.");
        Assert.IsTrue(targetBounds.Height < startBounds.Height * 0.50f,
            $"Ship should visibly shrink as z offset increases. Start height={startBounds.Height}px, target height={targetBounds.Height}px.");
    }

    [TestMethod]
    public void OutroShipControls_ApproachPhase_DoesNotShrinkShip()
    {
        var startBounds = GetShipHullLocalBoundsAfter(seconds: 0.01);
        var approachBounds = GetShipHullLocalBoundsAfter(seconds: 2.5);

        Assert.AreEqual(startBounds.Width, approachBounds.Width, 0.001f,
            $"Ship model should not be scaled while it is only flying in from the right. Start width={startBounds.Width}, approach width={approachBounds.Width}.");
        Assert.AreEqual(startBounds.Height, approachBounds.Height, 0.001f,
            $"Ship model should not be scaled before the inward dive. Start height={startBounds.Height}, approach height={approachBounds.Height}.");
    }

    [TestMethod]
    public void OutroShipControls_TurnsTowardEarthDuringDiveWithoutPitchingWrongAxis()
    {
        var now = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);
        var controls = new OutroShipControls(() => now);
        var ship = Ship.CreateShip(null, controls);
        ship.ObjectName = "Ship";
        ship.WorldPosition = new Vector3 { x = 0f, y = 0f, z = 0f };

        controls.MoveObject(ship, null, null);
        float startXRotation = ship.Rotation!.x;
        float startYRotation = ship.Rotation.y;
        float startZRotation = ship.Rotation.z;

        now = now.AddSeconds(2.9);
        controls.MoveObject(ship, null, null);
        float approachXRotation = ship.Rotation!.x;
        float approachZRotation = ship.Rotation.z;

        now = now.AddSeconds(2.1);
        controls.MoveObject(ship, null, null);

        Assert.AreEqual(70f, startXRotation, 0.001f,
            "Ship should enter with the normal scene pitch.");
        Assert.IsTrue(startZRotation < -80f && startZRotation > -120f,
            $"Ship should point across the screen from the right side toward Earth center. Start Z rotation was {startZRotation:0.0}.");
        Assert.AreEqual(70f, approachXRotation, 0.001f,
            "Ship should keep the normal scene pitch while approaching Earth center.");
        Assert.AreEqual(startZRotation, approachZRotation, 0.001f,
            "Ship should not yaw away from its approach heading while flying from the right.");
        Assert.AreEqual(startXRotation, ship.Rotation!.x, 0.001f,
            "The inward dive should not use the X/camera-tilt axis.");
        Assert.IsTrue(ship.Rotation.z > startZRotation,
            $"The inward dive should turn the ship's screen-plane heading toward Earth. Start Z={startZRotation:0.0}, final Z={ship.Rotation.z:0.0}.");
        Assert.AreEqual(0f, ship.Rotation.z, 0.001f,
            "The inward dive should finish nose-first on the Earth heading, not tail-first.");
        Assert.IsTrue(ship.Rotation.y > startYRotation,
            $"Ship may relax its bank during the dive, but should not spin. Start Y={startYRotation:0.0}, final Y={ship.Rotation.y:0.0}.");
    }

    [TestMethod]
    public void OutroShipControls_RequestsFullFadeWhenShipReachesEarth()
    {
        var now = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);
        var controls = new OutroShipControls(() => now);
        var ship = Ship.CreateShip(null, controls);
        ship.ObjectName = "Ship";
        ship.WorldPosition = new Vector3 { x = 0f, y = 0f, z = 0f };

        controls.MoveObject(ship, null, null);
        Assert.AreEqual(WorldFadePhase.Idle, GameState.WorldFade.Phase);

        now = now.AddSeconds(5.0);
        controls.MoveObject(ship, null, null);

        Assert.AreEqual(WorldFadePhase.FadeOutRequested, GameState.WorldFade.Phase,
            "Outro ship should use the shared full-screen fade when it reaches Earth.");
        Assert.AreEqual("OutroShipReachedEarth", GameState.WorldFade.Reason);
    }

    [TestMethod]
    public void OutroShipControls_Phase1_ShipSortsAboveAllEarthTriangles()
    {
        // Reproduce exactly what the renderer does:
        //   1. Project Earth + ship to 2D together (same list).
        //   2. Sort ascending by CalculatedZ (lower = drawn first = behind).
        //   3. Every ship hull triangle must have a higher CalculatedZ than
        //      every Earth globe triangle so that the ship is always painted last (on top).
        var world = CreateWorldAtOutro();
        var earth = (_3dObject)world.WorldInhabitants.First(o => o.ObjectName == "Earth");
        var ship  = (_3dObject)world.WorldInhabitants.First(o => o.ObjectName == "Ship");

        // Run one movement tick � this sets ZSortBias and positions ship in Phase 1
        ship.Movement!.MoveObject(ship, null, null);

        ApplyLiveMeshRotation(earth);
        ApplyLiveMeshRotation(ship);

        var converter = new _3dTo2d();
        // Project both objects together, exactly as the renderer does
        var allTriangles = converter.ConvertTo2dFromObjects(new List<_3dObject> { earth, ship }, currentFrame: 1);

        // Renderer sort: ascending CalculatedZ ? highest = drawn last = on top
        allTriangles.Sort((a, b) => a.CalculatedZ.CompareTo(b.CalculatedZ));

        var earthGlobeTriangles = allTriangles.Where(t => t.PartName == "EarthGlobe").ToList();
        var shipHullTriangles   = allTriangles.Where(t => t.PartName == "UpperPart" || t.PartName == "LowerPart" || t.PartName == "RearPart").ToList();

        Assert.IsTrue(earthGlobeTriangles.Count > 0, "Earth globe must project triangles.");
        Assert.IsTrue(shipHullTriangles.Count > 0,   "Ship hull must project triangles.");

        float maxEarthZ = earthGlobeTriangles.Max(t => t.CalculatedZ);
        float minShipZ  = shipHullTriangles.Min(t => t.CalculatedZ);
        float maxShipZ  = shipHullTriangles.Max(t => t.CalculatedZ);

        Assert.IsTrue(minShipZ > maxEarthZ,
            $"ALL ship hull triangles must sort above ALL Earth globe triangles so the ship renders on top. " +
            $"Ship CalculatedZ range: {minShipZ:0.0}�{maxShipZ:0.0}, Earth globe max: {maxEarthZ:0.0}.");
    }

    [TestMethod]
    public void OutroShipControls_ReleasesParticlesFromBothEngines()
    {
        var world = CreateWorldAtOutro();
        var ship = world.WorldInhabitants.First(o => o.ObjectName == "Ship");
        var controls = (OutroShipControls)ship.Movement!;

        controls.SetParticleGuideCoordinates(
            ship.ObjectParts.First(p => p.PartName == "JetMotor").Triangles.First(),
            ship.ObjectParts.First(p => p.PartName == "JetMotorDirectionGuide").Triangles.First());
        controls.SetRearEngineGuideCoordinates(
            ship.ObjectParts.First(p => p.PartName == "RearEngine").Triangles.First(),
            ship.ObjectParts.First(p => p.PartName == "RearEngineDirectionGuide").Triangles.First());

        controls.ReleaseParticles(ship);

        var particlePositions = ship.Particles!.Particles
            .Select(p => (Vector3)p.Position)
            .ToList();

        Assert.IsTrue(particlePositions.Count >= 40,
            $"Both engines should emit in the same frame. Particle count was {particlePositions.Count}.");
        Assert.IsTrue(particlePositions.Any(p => p.y < 30f),
            "Lower jet motor should contribute particles near the belly engine.");
        Assert.IsTrue(particlePositions.Any(p => p.y > 45f),
            "Rear engine should contribute particles from the tail engine.");
    }

    [TestMethod]
    public void OutroShipControls_ThrottlesEngineParticleEmission()
    {
        var now = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);
        var controls = new OutroShipControls(() => now);
        var ship = Ship.CreateShip(null, controls);

        controls.SetParticleGuideCoordinates(
            ship.ObjectParts.First(p => p.PartName == "JetMotor").Triangles.First(),
            ship.ObjectParts.First(p => p.PartName == "JetMotorDirectionGuide").Triangles.First());
        controls.SetRearEngineGuideCoordinates(
            ship.ObjectParts.First(p => p.PartName == "RearEngine").Triangles.First(),
            ship.ObjectParts.First(p => p.PartName == "RearEngineDirectionGuide").Triangles.First());

        controls.ReleaseParticles(ship);
        int firstCount = ship.Particles!.Particles.Count;

        controls.ReleaseParticles(ship);
        int immediateCount = ship.Particles.Particles.Count;

        now = now.AddSeconds(2.1);
        controls.ReleaseParticles(ship);
        int delayedCount = ship.Particles.Particles.Count;

        Assert.IsTrue(firstCount > 0, "First engine emission should create particles.");
        Assert.AreEqual(firstCount, immediateCount,
            "Engine particles should not be emitted again before the two second interval.");
        Assert.IsTrue(delayedCount > immediateCount,
            "Engine particles should emit again after the two second interval.");
    }

    [TestMethod]
    public void OutroShipControls_PlaysRocketLoop()
    {
        var world = CreateWorldAtOutro();
        var ship = world.WorldInhabitants.First(o => o.ObjectName == "Ship");
        var audio = new CapturingAudioPlayer();
        var registry = new FakeSoundRegistry();

        ship.Movement!.MoveObject(ship, audio, registry);

        Assert.AreEqual("rocket_main", audio.LastSoundId, "Outro ship should start the normal rocket sound.");
        Assert.AreEqual(AudioPlayMode.SegmentedLoop, audio.LastMode, "Rocket sound should run as a loop during the fly-in.");
    }

    [TestMethod]
    public void OutroScene_EarthProjectsVisibleTriangles()
    {
        var world = CreateWorldAtOutro();
        var earth = (_3dObject)world.WorldInhabitants.First(o => o.ObjectName == "Earth");
        ApplyLiveMeshRotation(earth);
        int triangleCount = earth.ObjectParts.Sum(p => p.Triangles.Count);
        var converter = new _3dTo2d();

        var projected = converter.ConvertTo2dFromObjects(new List<_3dObject> { earth }, currentFrame: 1);
        var bounds = GetProjectedBounds(projected);
        var brightestShade = projected.Max(CalculateRenderShadeFactor);

        Assert.IsTrue(projected.Count > 0, "Earth should project at least one visible triangle on the Outro screen.");
        Assert.IsTrue(projected.Count < triangleCount,
            $"Earth should cull hidden faces before render. Projected {projected.Count} of {triangleCount} triangles.");
        Assert.IsTrue(bounds.Width >= 260,
            $"Earth should be large enough to see. Projected width was {bounds.Width}px.");
        Assert.IsTrue(bounds.Height >= 180,
            $"Earth should be large enough to see. Projected height was {bounds.Height}px.");
        Assert.IsTrue(brightestShade >= 0.15f,
            $"Earth should have lit triangles against the black background. Brightest shade was {brightestShade:0.00}.");
    }

    [TestMethod]
    public void OutroScene_EarthSurvivesLiveRotationAndRenderFiltering()
    {
        var world = CreateWorldAtOutro();
        var earth = (_3dObject)world.WorldInhabitants.First(o => o.ObjectName == "Earth");
        ApplyLiveMeshRotation(earth);

        var converter = new _3dTo2d();
        var projected = converter.ConvertTo2dFromObjects(new List<_3dObject> { earth }, currentFrame: 1);
        int renderable = WorldRenderer.ProcessTrianglesForRender(
            projected,
            new Dictionary<(float, string), Color>(),
            new Dictionary<Color, SolidColorBrush>(),
            new Dictionary<Color, Pen>());

        Assert.IsTrue(projected.Count > 0, "Earth should still project triangles after live-loop mesh rotation.");
        Assert.IsTrue(renderable > 0, "Earth should survive renderer depth/color filtering after projection.");
    }

    [TestMethod]
    public void OutroScene_EarthRotation_HasCorrectXAxis()
    {
        var world = CreateWorldAtOutro();
        var earth = world.WorldInhabitants.First(o => o.ObjectName == "Earth");
        Assert.AreEqual(70f, earth.Rotation.x, "Earth X rotation should be 70 (camera tilt).");
    }

    [TestMethod]
    public void OutroScene_HasSceneMusic()
    {
        GameState.GamePlayState.SceneIndex = 9;
        var handler = new SceneHandler();
        var scene = handler.GetActiveScene();
        Assert.IsFalse(string.IsNullOrEmpty(scene.SceneMusic), "Outro scene should have a SceneMusic id.");
        Assert.AreEqual("music_outro", scene.SceneMusic, "Outro should use music_outro.");
    }

    [TestMethod]
    public void OutroScene_HasNoDirector()
    {
        GameState.GamePlayState.SceneIndex = 9;
        var handler = new SceneHandler();
        Assert.IsNull(handler.GetActiveScene().Director, "Outro should not have a director.");
    }

    [TestMethod]
    public void OutroScene_OverlayType_IsOutro()
    {
        var world = CreateWorldAtOutro();
        Assert.AreEqual(ScreenOverlayType.Outro, GameState.ScreenOverlayState.Type,
            "Outro should set overlay type to Outro.");
    }

    [TestMethod]
    public void OutroScene_OverlayIsHidden()
    {
        var world = CreateWorldAtOutro();
        Assert.IsFalse(GameState.ScreenOverlayState.ShowOverlay,
            "Outro overlay should be hidden on setup.");
    }

    [TestMethod]
    public void OutroScene_OnlyContainsEarth()
    {
        var world = CreateWorldAtOutro();
        var unexpected = world.WorldInhabitants
            .Where(o => o.ObjectName != "Earth" && o.ObjectName != "Asteroid" && o.ObjectName != "Ship")
            .ToList();
        Assert.AreEqual(0, unexpected.Count,
            $"Outro should only contain Earth, Ship, and Asteroid objects. Found: {string.Join(", ", unexpected.Select(o => o.ObjectName))}");
    }

    private static void ApplyLiveMeshRotation(_3dObject obj)
    {
        var rotate = new _3dRotationCommon();
        var rotation = (Vector3)obj.Rotation;

        foreach (var part in obj.ObjectParts)
        {
            var rotated = rotate.RotateMesh(part.Triangles, rotation.z, 'Z');
            rotated = rotate.RotateMesh(rotated, rotation.y, 'Y');
            rotated = rotate.RotateMesh(rotated, rotation.x, 'X');
            part.Triangles = rotated;
        }
    }

    private static (int Width, int Height) GetProjectedBounds(List<_2dTriangleMesh> triangles)
    {
        Assert.IsTrue(triangles.Count > 0, "Cannot measure bounds without projected triangles.");

        int minX = triangles.Min(t => Math.Min(t.X1, Math.Min(t.X2, t.X3)));
        int maxX = triangles.Max(t => Math.Max(t.X1, Math.Max(t.X2, t.X3)));
        int minY = triangles.Min(t => Math.Min(t.Y1, Math.Min(t.Y2, t.Y3)));
        int maxY = triangles.Max(t => Math.Max(t.Y1, Math.Max(t.Y2, t.Y3)));

        return (maxX - minX, maxY - minY);
    }

    private static (int Width, int Height) ProjectShipHullBoundsAfter(double seconds)
    {
        var now = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);
        var controls = new OutroShipControls(() => now);
        var ship = Ship.CreateShip(null, controls);
        ship.ObjectName = "Ship";
        ship.WorldPosition = new Vector3 { x = 0f, y = 0f, z = 0f };

        controls.MoveObject(ship, null, null);
        now = now.AddSeconds(seconds);
        controls.MoveObject(ship, null, null);
        ApplyLiveMeshRotation(ship);

        var converter = new _3dTo2d();
        var projected = converter.ConvertTo2dFromObjects(new List<_3dObject> { ship }, currentFrame: 1)
            .Where(t => t.PartName == "UpperPart" || t.PartName == "LowerPart" || t.PartName == "RearPart" || t.PartName == "Winglets")
            .ToList();

        return GetProjectedBounds(projected);
    }

    private static (float Width, float Height) GetShipHullLocalBoundsAfter(double seconds)
    {
        var now = new DateTime(2026, 5, 20, 12, 0, 0, DateTimeKind.Utc);
        var controls = new OutroShipControls(() => now);
        var ship = Ship.CreateShip(null, controls);
        ship.ObjectName = "Ship";
        ship.WorldPosition = new Vector3 { x = 0f, y = 0f, z = 0f };

        controls.MoveObject(ship, null, null);
        now = now.AddSeconds(seconds);
        controls.MoveObject(ship, null, null);

        var vertices = ship.ObjectParts
            .Where(part => part.PartName == "UpperPart" || part.PartName == "LowerPart" || part.PartName == "RearPart" || part.PartName == "Winglets")
            .SelectMany(part => part.Triangles)
            .SelectMany(t => new[] { (Vector3)t.vert1, (Vector3)t.vert2, (Vector3)t.vert3 })
            .ToList();

        Assert.IsTrue(vertices.Count > 0, "Cannot measure ship hull bounds without vertices.");
        return (
            vertices.Max(v => v.x) - vertices.Min(v => v.x),
            vertices.Max(v => v.y) - vertices.Min(v => v.y));
    }

    private static float CalculateRenderShadeFactor(_2dTriangleMesh triangle)
    {
        float depthFactor01 = WorldRenderer.GetDepthFactor01(triangle.CalculatedZ);
        float angleFactor01 = Math.Clamp((triangle.TriangleAngle + 1f) * 0.5f, 0f, 1f);

        return Math.Clamp(depthFactor01 * angleFactor01, 0f, 1f);
    }

    private static Color ParseColor(string? value)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(value), "Triangle color should be set.");
        return (Color)ColorConverter.ConvertFromString(value)!;
    }

    private static bool IsDarkOceanColor(Color color)
    {
        return color.B > color.G + 35 && color.B > color.R + 45 && color.R < 20 && color.G < 50;
    }

    private static bool IsBlueDominant(Color color)
    {
        return color.B > color.G && color.B > color.R;
    }

    private static bool IsLandColor(Color color)
    {
        return color.G > color.R + 25 && color.G > color.B + 10;
    }

    private static bool IsMountainGray(Color color)
    {
        int max = Math.Max(color.R, Math.Max(color.G, color.B));
        int min = Math.Min(color.R, Math.Min(color.G, color.B));
        return max - min <= 3 && color.R >= 100;
    }

    private static Vector3 CreateSpherePoint(float latitude, float longitude, float radius = 180f)
    {
        float lat = latitude * MathF.PI / 180f;
        float lon = longitude * MathF.PI / 180f;
        float cosLat = MathF.Cos(lat);

        return new Vector3
        {
            x = radius * cosLat * MathF.Cos(lon),
            y = radius * MathF.Sin(lat),
            z = radius * cosLat * MathF.Sin(lon)
        };
    }

    private static bool IsOutroStarPart(I3dObjectPart part)
    {
        return part.PartName != null && part.PartName.StartsWith("Star_", StringComparison.Ordinal);
    }

    private static bool IsMiniaturePart(I3dObjectPart part)
    {
        return part.PartName != null
            && (part.PartName.StartsWith("MiniTree_", StringComparison.Ordinal)
             || part.PartName.StartsWith("MiniHouse_", StringComparison.Ordinal)
             || part.PartName.StartsWith("MiniIgloo_", StringComparison.Ordinal));
    }

    private static Vector3 GetPartCenter(I3dObjectPart part)
    {
        var vertices = part.Triangles
            .SelectMany(t => new[] { (Vector3)t.vert1, (Vector3)t.vert2, (Vector3)t.vert3 })
            .ToList();

        return new Vector3
        {
            x = vertices.Average(v => v.x),
            y = vertices.Average(v => v.y),
            z = vertices.Average(v => v.z)
        };
    }

    private static int GetFirstTriangleBaseAngleBucket(I3dObjectPart part)
    {
        var tri = part.Triangles.First();
        var v1 = (Vector3)tri.vert1;
        var v2 = (Vector3)tri.vert2;
        float angle = MathF.Atan2(v2.y - v1.y, v2.x - v1.x) * 180f / MathF.PI;
        if (angle < 0f) angle += 360f;
        return (int)MathF.Round(angle);
    }

    private sealed class FakeSoundRegistry : ISoundRegistry
    {
        private readonly SoundDefinition _rocket = new()
        {
            Id = "rocket_main",
            Settings = new SoundSettings { Volume = 1f },
            Speed = new SoundSpeed { Base = 1f, Min = 0.8f, Max = 1.4f }
        };

        public SoundDefinition Get(string id) => id == _rocket.Id
            ? _rocket
            : throw new InvalidOperationException($"Unexpected sound id {id}.");

        public bool TryGet(string id, out SoundDefinition definition)
        {
            if (id == _rocket.Id)
            {
                definition = _rocket;
                return true;
            }

            definition = new SoundDefinition();
            return false;
        }
    }

    private sealed class CapturingAudioPlayer : IAudioPlayer
    {
        public string? LastSoundId { get; private set; }
        public AudioPlayMode? LastMode { get; private set; }

        public IAudioInstance Play(SoundDefinition definition, AudioPlayMode mode, AudioPlayOptions? options = null)
        {
            LastSoundId = definition.Id;
            LastMode = mode;
            return new CapturingAudioInstance(definition.Id, mode == AudioPlayMode.SegmentedLoop);
        }

        public void PlayOneShot(SoundDefinition definition, AudioPlayOptions? options = null) => Play(definition, AudioPlayMode.OneShot, options);
        public void Stop(IAudioInstance instance, bool playEndSegment) => instance.Stop(playEndSegment);
        public void StopAll() { }
        public void PlayMusic(SoundDefinition definition, float? volumeOverride = null) { }
        public void StopMusic() { }
        public void Update(double deltaTimeSeconds) { }
    }

    private sealed class CapturingAudioInstance : IAudioInstance
    {
        public CapturingAudioInstance(string soundId, bool isLooping)
        {
            SoundId = soundId;
            IsLooping = isLooping;
            IsPlaying = true;
        }

        public Guid Id { get; } = Guid.NewGuid();
        public string SoundId { get; }
        public bool IsPlaying { get; private set; }
        public bool IsLooping { get; }

        public void SetVolume(float volume) { }
        public void SetSpeed(float speed) { }
        public void SetWorldPosition(NumericsVector3 position) { }
        public void Stop(bool playEndSegment) => IsPlaying = false;
    }
}
