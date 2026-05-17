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

        Assert.IsTrue(earth.ObjectParts.SelectMany(p => p.Triangles).All(t => t.noHidden != true),
            "Outro Earth must use backface culling; rendering every hidden sphere face is too expensive.");
    }

    [TestMethod]
    public void OutroScene_EarthGlobePalette_ComesFromGlbVertexColors()
    {
        var world = CreateWorldAtOutro();
        var earth = world.WorldInhabitants.First(o => o.ObjectName == "Earth");
        var colors = earth.ObjectParts
            .SelectMany(p => p.Triangles)
            .Select(t => ParseColor(t.Color))
            .ToList();

        Assert.IsTrue(EarthModelData.UniqueColorCount >= 2, "The GLB source should provide at least ocean and land vertex colors.");
        Assert.IsTrue(colors.Distinct().Count() >= 2, "Earth should not render as one flat color.");
        Assert.IsTrue(colors.Any(c => c.B > c.G + 40 && c.B > c.R + 80), "Earth should include strong blue ocean colors from the GLB.");
        Assert.IsTrue(colors.Any(c => c.G > c.R + 40 && c.G > c.B + 10), "Earth should include green land colors from the GLB.");
    }

    [TestMethod]
    public void OutroScene_EarthOnlyUsesSingleGlbPart()
    {
        var world = CreateWorldAtOutro();
        var earth = world.WorldInhabitants.First(o => o.ObjectName == "Earth");

        Assert.AreEqual(1, earth.ObjectParts.Count, "Earth should not keep the failed procedural land/outline/detail parts.");
        Assert.AreEqual("EarthGlobe", earth.ObjectParts[0].PartName);
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

        Assert.IsTrue(projected.Count > triangleCount * 0.25f,
            $"Earth should still have enough visible front-side triangles. Projected {projected.Count} of {triangleCount}.");
        Assert.IsTrue(projected.Count < triangleCount * 0.75f,
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
        var nonEarth = world.WorldInhabitants.Where(o => o.ObjectName != "Earth").ToList();
        Assert.AreEqual(0, nonEarth.Count,
            $"Outro should only contain the Earth object. Found: {string.Join(", ", nonEarth.Select(o => o.ObjectName))}");
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
}
