using _3dTesting._3dWorld;
using _3dTesting._3dRotation;
using _3dTesting._Coordinates;
using _3dTesting.Rendering;
using _3DWorld.Scene;
using _3dRotations.World.Objects.EarthObject;
using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using Domain;
using System.Windows.Media;
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
            .Where(o => o.ObjectName != "Earth" && o.ObjectName != "Asteroid")
            .ToList();
        Assert.AreEqual(0, unexpected.Count,
            $"Outro should only contain Earth and Asteroid objects. Found: {string.Join(", ", unexpected.Select(o => o.ObjectName))}");
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
}
