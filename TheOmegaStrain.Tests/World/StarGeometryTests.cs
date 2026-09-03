using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Common.CommonGlobalState.States;
using TheOmegaStrain.Domain;
using TheOmegaStrain.Game.World.Objects;

namespace TheOmegaStrain.Tests.World;

[TestClass]
public class StarGeometryTests
{
    [TestInitialize]
    public void Setup()
    {
        GameState.SurfaceState = new SurfaceState
        {
            GlobalMapPosition = new Vector3 { x = 0f, y = 0f, z = 0f }
        };
        GameState.ObjectIdCounter = 0;
    }

    [TestMethod]
    public void CreateStar_ProducesGeometryLargerThanOnePixel()
    {
        // Regression guard: the original geometry was sub-pixel (0.4px arms) and only
        // remained visible because the WPF renderer stroked each triangle with a 1px pen.
        // Direct3D fills triangles without a stroke, so sub-pixel stars disappear.
        var star = Star.CreateStar(parentSurface: null!, randomOffset: new Vector3());

        Assert.IsTrue(star.ObjectParts.Count > 0, "Star must have geometry.");

        float maxExtent = 0f;
        foreach (var part in star.ObjectParts)
        {
            foreach (var tri in part.Triangles)
            {
                foreach (var v in new[] { tri.vert1, tri.vert2, tri.vert3 })
                {
                    // Orientation is baked in, so the arm span can land on any axis.
                    maxExtent = Math.Max(maxExtent, MathF.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z));
                }
            }
        }

        Assert.IsTrue(
            maxExtent >= 1.5f,
            $"Star arms must be at least 1.5px from centre to survive rasterization, was {maxExtent}px.");
    }

    [TestMethod]
    public void CreateStar_BakesOrientationIntoGeometryAndLeavesRotationZero()
    {
        // Stars bypass the per-frame deep copy, so a non-zero Rotation would be re-applied to
        // the same triangles every frame and accumulate. Orientation must live in the mesh.
        var a = Star.CreateStar(parentSurface: null!, randomOffset: new Vector3());
        var b = Star.CreateStar(parentSurface: null!, randomOffset: new Vector3());

        Assert.AreEqual(0f, a.Rotation.x);
        Assert.AreEqual(0f, a.Rotation.y);
        Assert.AreEqual(0f, a.Rotation.z);

        bool geometryDiffers = false;
        var triA = a.ObjectParts[0].Triangles;
        var triB = b.ObjectParts[0].Triangles;
        for (int i = 0; i < triA.Count && !geometryDiffers; i++)
        {
            if (Math.Abs(triA[i].vert1.x - triB[i].vert1.x) > 0.0001f ||
                Math.Abs(triA[i].vert1.y - triB[i].vert1.y) > 0.0001f ||
                Math.Abs(triA[i].vert1.z - triB[i].vert1.z) > 0.0001f)
            {
                geometryDiffers = true;
            }
        }

        Assert.IsTrue(geometryDiffers, "Each star should get its own baked random orientation.");
    }
}
