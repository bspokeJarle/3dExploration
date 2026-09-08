using TheOmegaStrain.Common.CommonSetup;
using TheOmegaStrain.Domain;
using TheOmegaStrain.Gameplay.Helpers;

namespace TheOmegaStrain.Tests.Controls;

[TestClass]
public class MovementHelpersPursuitTests
{
    [TestMethod]
    public void GetVelocityTowardsTarget_NormalizesDirectionAndAppliesSpeed()
    {
        var velocity = MovementHelpers.GetVelocityTowardsTarget(
            new Vector3 { x = 0f, y = 0f, z = 0f },
            new Vector3 { x = 3f, y = 0f, z = 4f },
            100f);

        Assert.AreEqual(60f, velocity.x, 0.001f);
        Assert.AreEqual(0f, velocity.y, 0.001f);
        Assert.AreEqual(80f, velocity.z, 0.001f);
        Assert.AreEqual(100f, MovementHelpers.GetLength(velocity), 0.001f);
    }

    [TestMethod]
    public void GetPursuitStep_WhenMoveReachesTarget_MarksOvershootAndKeepsRequestedMoveDistance()
    {
        var step = MovementHelpers.GetPursuitStep(
            new Vector3 { x = 0f, y = 0f, z = 0f },
            new Vector3 { x = 10f, y = 0f, z = 0f },
            new Vector3 { x = 30f, y = 0f, z = 0f },
            1f);

        Assert.IsTrue(step.HasMovement);
        Assert.IsTrue(step.ShouldStartOvershoot);
        Assert.AreEqual(10f, step.DistanceToTarget, 0.001f);
        Assert.AreEqual(30f, step.MoveDistance, 0.001f);
        Assert.AreEqual(1f, step.MovementDirection.x, 0.001f);
    }

    [TestMethod]
    public void GetPursuitStep_WithForcedDirection_ContinuesPastTargetWithoutRestartingOvershoot()
    {
        var step = MovementHelpers.GetPursuitStep(
            new Vector3 { x = 0f, y = 0f, z = 0f },
            new Vector3 { x = 0f, y = 0f, z = 0f },
            new Vector3 { x = 0f, y = 0f, z = 90f },
            0.5f,
            new Vector3 { x = 0f, y = 0f, z = 12f });

        Assert.IsTrue(step.HasMovement);
        Assert.IsFalse(step.ShouldStartOvershoot);
        Assert.AreEqual(45f, step.MoveDistance, 0.001f);
        Assert.AreEqual(0f, step.MovementDirection.x, 0.001f);
        Assert.AreEqual(1f, step.MovementDirection.z, 0.001f);
    }

    [TestMethod]
    public void ShouldRefreshPursuitVelocity_WhenVelocityPointsAwayFromTarget_ReturnsTrue()
    {
        var refresh = MovementHelpers.ShouldRefreshPursuitVelocity(
            DateTime.Now,
            DateTime.Now,
            10f,
            new Vector3 { x = -1f, y = 0f, z = 0f },
            new Vector3 { x = 100f, y = 0f, z = 0f },
            isOvershooting: false);

        Assert.IsTrue(refresh);
    }

    [TestMethod]
    public void MoveRotationTowards_UsesDeltaTimeLimitedRotation()
    {
        var rotation = MovementHelpers.MoveRotationTowards(
            currentX: WorldViewSetup.SurfaceFacingObjectPitchDegrees,
            currentY: 0f,
            currentZ: 0f,
            targetX: WorldViewSetup.SurfaceFacingObjectPitchDegrees,
            targetY: 0f,
            targetZ: 90f,
            degreesPerSecond: 180f,
            deltaSeconds: 0.25);

        Assert.AreEqual(WorldViewSetup.SurfaceFacingObjectPitchDegrees, rotation.X, 0.001f);
        Assert.AreEqual(0f, rotation.Y, 0.001f);
        Assert.AreEqual(45f, rotation.Z, 0.001f);
    }
}
