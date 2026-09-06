using TheOmegaStrain.Game.World.Objects;
using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Common.CommonGlobalState.States;
using TheOmegaStrain.Domain;
using TheOmegaStrain.Runtime.Rendering;

namespace TheOmegaStrain.Tests.Rendering;

[TestClass]
public class ObjectShadowManagerTests
{
    [TestInitialize]
    public void Setup()
    {
        GameState.SurfaceState = new SurfaceState
        {
            GlobalMapPosition = new Vector3(),
            SurfaceViewportObject = new OmegaObject3D
            {
                ObjectId = 1,
                ObjectName = "Surface",
                ObjectOffsets = new Vector3 { x = 0f, y = 500f, z = 0f },
                WorldPosition = new Vector3(),
                Rotation = new Vector3()
            }
        };
        GameState.ObjectIdCounter = 10;
    }

    [TestMethod]
    public void FreeFlyingShadow_InterpolatesGroundYInsideSurfaceTriangle()
    {
        float oldStaticOffsetY = ObjectShadowManager.StaticOffsetY;
        try
        {
            ObjectShadowManager.StaticOffsetY = -40f;

            var surface = new Surface
            {
                RotatedSurfaceTriangles = new List<ITriangleMeshWithColorAndTexture>
                {
                    new TriangleMeshWithColor
                    {
                        vert1 = new Vector3 { x = 0f, y = 0f, z = 0f },
                        vert2 = new Vector3 { x = 100f, y = 100f, z = 0f },
                        vert3 = new Vector3 { x = 0f, y = 0f, z = 100f }
                    }
                }
            };

            var flyingObject = CreateFreeFlyingShadowCaster(surface, x: 25f, y: 400f, z: 25f);
            var shadows = new List<OmegaObject3D>();

            new ObjectShadowManager().HandleObjectShadow(flyingObject, shadows);

            Assert.AreEqual(1, shadows.Count);
            var shadowVertex = shadows[0].ObjectParts[0].Triangles[0].vert1;

            Assert.AreEqual(
                25f + ObjectShadowManager.StaticOffsetY,
                shadowVertex.y,
                0.001f,
                "Free-flying shadows should use barycentric surface Y under the object, not the nearest tile center.");
        }
        finally
        {
            ObjectShadowManager.StaticOffsetY = oldStaticOffsetY;
        }
    }

    [TestMethod]
    public void FreeFlyingShadow_IsSkippedWhenObjectIsOutsideSurfaceBounds()
    {
        var surface = new Surface
        {
            RotatedSurfaceTriangles = new List<ITriangleMeshWithColorAndTexture>
            {
                new TriangleMeshWithColor
                {
                    vert1 = new Vector3 { x = 0f, y = 0f, z = 0f },
                    vert2 = new Vector3 { x = 100f, y = 100f, z = 0f },
                    vert3 = new Vector3 { x = 0f, y = 0f, z = 100f }
                }
            }
        };

        // Mirrors a mother ship during descent: spawned far behind the tile grid
        // (DescentSpawnOffsetZ = -1500), so no ground exists under it.
        var flyingObject = CreateFreeFlyingShadowCaster(surface, x: 25f, y: 400f, z: 1500f);
        var shadows = new List<OmegaObject3D>();

        new ObjectShadowManager().HandleObjectShadow(flyingObject, shadows);

        Assert.AreEqual(
            0,
            shadows.Count,
            "Objects outside the tile grid must not snap their shadow to the nearest edge tile.");
    }

    [TestMethod]
    public void FreeFlyingShadow_IsClampedToHorizonWhenOffsetPushesItAboveTerrain()
    {
        float oldStaticOffsetY = ObjectShadowManager.StaticOffsetY;
        try
        {
            ObjectShadowManager.StaticOffsetY = 0f;

            // Horizon row (smallest Y) of this grid is y = 0.
            var surface = new Surface
            {
                RotatedSurfaceTriangles = new List<ITriangleMeshWithColorAndTexture>
                {
                    new TriangleMeshWithColor
                    {
                        vert1 = new Vector3 { x = 0f, y = 0f, z = 0f },
                        vert2 = new Vector3 { x = 100f, y = 100f, z = 0f },
                        vert3 = new Vector3 { x = 0f, y = 0f, z = 100f }
                    }
                }
            };

            var flyingObject = CreateFreeFlyingShadowCaster(surface, x: 25f, y: 400f, z: 25f);

            // A large negative offset would drive the anchor far above the
            // horizon, making the shadow float in the sky behind the terrain.
            flyingObject.ShadowOffset = new Vector3 { x = 0f, y = -900f, z = 0f };

            var shadows = new List<OmegaObject3D>();

            new ObjectShadowManager().HandleObjectShadow(flyingObject, shadows);

            Assert.AreEqual(1, shadows.Count);
            var shadowVertex = shadows[0].ObjectParts[0].Triangles[0].vert1;

            Assert.AreEqual(
                ObjectShadowManager.ShadowHorizonMargin,
                shadowVertex.y,
                0.001f,
                "Shadow anchors must be clamped to the terrain horizon instead of floating above the surface.");
        }
        finally
        {
            ObjectShadowManager.StaticOffsetY = oldStaticOffsetY;
        }
    }

    private static OmegaObject3D CreateFreeFlyingShadowCaster(Surface surface, float x, float y, float z)
    {
        return new OmegaObject3D
        {
            ObjectId = 2,
            ObjectName = "KamikazeDrone",
            HasShadow = true,
            ParentSurface = surface,
            ObjectOffsets = new Vector3 { x = x, y = y, z = z },
            WorldPosition = new Vector3(),
            Rotation = new Vector3(),
            ObjectParts = new List<I3dObjectPart>
            {
                new OmegaObjectPart3D
                {
                    PartName = "Shadow",
                    IsVisible = false,
                    Triangles = new List<ITriangleMeshWithColorAndTexture>
                    {
                        new TriangleMeshWithColor
                        {
                            Color = "000000",
                            noHidden = true,
                            vert1 = new Vector3(),
                            vert2 = new Vector3(),
                            vert3 = new Vector3()
                        }
                    }
                }
            }
        };
    }
}
