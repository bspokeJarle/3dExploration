using Domain;
using static Domain._3dSpecificsImplementations;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using System.Windows;
using GameAiAndControls.Physics;

public class ParticlesAI : IParticles
{
    private Random random = new();

    // --- Konfigurasjon ---
    private const int MaxParticlesBase = 15;
    private const int MaxThrustMultiplier = 5;
    private const int MaxDynamicParticles = 30;
    private const float MinLife = 2f;
    private const float MaxLife = 3.5f;
    private const float MinSize = 1f;
    private const float MaxSize = 4f;
    private const float SpreadIntensity = 3f;
    private const float DragFactor = 0.98f;
    private const float BounceLoss = 0.1f; // redusert fra 0.9f for mindre dramatiske sprett
    private const float LifetimeLossOnBounce = 0.8f;
    private const float AccelerationRandomFactor = 0.1f;
    private const float FadeFactor = 0.01f;

    public List<IParticle> Particles { get; set; } = new();
    public IObjectMovement? ParentShip { get; set; }
    public bool Visible { get; set; }
    public bool EnableParticleLogging { get; set; } = false;

    public void MoveParticles()
    {
        var deadParticles = new List<IParticle>();
        long currentTicks = DateTime.UtcNow.Ticks;

        foreach (var particle in Particles)
        {
            if (particle.BirthTime.Ticks + particle.VariedStart > currentTicks) continue;

            long lifeTicks = (long)(particle.Life * 10_000_000);
            long deathTicks = particle.BirthTime.Ticks + lifeTicks + particle.VariedStart;

            if (deathTicks > currentTicks)
            {
                particle.Visible = true;

                float lifeProgress = (float)(currentTicks - particle.BirthTime.Ticks - particle.VariedStart) / lifeTicks;

                particle.Size = ApplyFade(particle.Size, lifeProgress);
                particle.Color = GetColorByLifeProgress(lifeProgress);

                particle.Velocity.x *= DragFactor;
                particle.Velocity.y *= DragFactor;
                particle.Velocity.z *= DragFactor;

                particle.Velocity.x += particle.Acceleration.x;
                particle.Velocity.y += particle.Acceleration.y;
                particle.Velocity.z += particle.Acceleration.z;

                if (particle.ImpactStatus?.HasCrashed == true)
                {
                    string before = $"Before bounce: Vx={particle.Velocity.x:F2}, Vy={particle.Velocity.y:F2}, Vz={particle.Velocity.z:F2}";

                    switch (particle.ImpactStatus.ImpactDirection)
                    {
                        case ImpactDirection.Top:
                            if (particle.Velocity.y < 0f)
                                particle.Velocity.y = Math.Abs(particle.Velocity.y) * BounceLoss;
                            break;
                        case ImpactDirection.Bottom:
                            if (particle.Velocity.y > 0f)
                                particle.Velocity.y = -Math.Abs(particle.Velocity.y) * BounceLoss;
                            break;
                        case ImpactDirection.Left:
                        case ImpactDirection.Right:
                            particle.Velocity.x *= -BounceLoss;
                            break;
                        case ImpactDirection.Center:
                            particle.Velocity.z *= -BounceLoss;
                            break;
                    }

                    particle.Life *= LifetimeLossOnBounce;
                    particle.ImpactStatus.HasCrashed = false;

                    if (Logger.EnableFileLogging && EnableParticleLogging)
                    {
                        Logger.Log("[Particle Bounce] " + before);
                        Logger.Log($"After bounce: Vx={particle.Velocity.x:F2}, Vy={particle.Velocity.y:F2}, Vz={particle.Velocity.z:F2}");
                    }
                }

                particle.Position.x -= particle.Velocity.x;
                particle.Position.y -= particle.Velocity.y;
                particle.Position.z -= particle.Velocity.z;

                if (particle.Rotation != null && particle.RotationSpeed != null)
                {
                    particle.Rotation.x += particle.RotationSpeed.x;
                    particle.Rotation.y += particle.RotationSpeed.y;
                    particle.Rotation.z += particle.RotationSpeed.z;
                }
            }
            else
            {
                deadParticles.Add(particle);
            }
        }

        foreach (var deadParticle in deadParticles)
        {
            Particles.Remove(deadParticle);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ApplyFade(float size, float progress) => size * (1.0f - FadeFactor * progress);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetColorByLifeProgress(float progress)
    {
        if (progress < 0.5f) return "ffff00";
        if (progress < 0.8f) return "ff6600";
        return "ff0000";
    }

    public void ReleaseParticles(ITriangleMeshWithColor Trajectory, ITriangleMeshWithColor StartPosition, IVector3 WorldPosition, IObjectMovement ParShip, int Thrust)
    {
        if (Thrust == 0)
        {
            Particles.Clear();
            Particles.TrimExcess();
            return;
        }

        int dynamicMaxParticles = Math.Min(MaxDynamicParticles, Thrust * 2 + MaxParticlesBase);
        if (Particles.Count > dynamicMaxParticles) return;
        if (StartPosition == null || Trajectory == null) return;

        ParentShip = ParShip;
        int particleCount = Thrust * MaxThrustMultiplier;

        float startX = (StartPosition.vert1.x + StartPosition.vert2.x + StartPosition.vert3.x) / 3;
        float startY = (StartPosition.vert1.y + StartPosition.vert2.y + StartPosition.vert3.y) / 3;
        float startZ = (StartPosition.vert1.z + StartPosition.vert2.z + StartPosition.vert3.z) / 3;

        float guideX = (Trajectory.vert1.x + Trajectory.vert2.x + Trajectory.vert3.x) / 3;
        float guideY = (Trajectory.vert1.y + Trajectory.vert2.y + Trajectory.vert3.y) / 3;
        float guideZ = (Trajectory.vert1.z + Trajectory.vert2.z + Trajectory.vert3.z) / 3;

        for (int i = 0; i < particleCount && Particles.Count < dynamicMaxParticles; i++)
        {
            float life = (float)(random.NextDouble() * (MaxLife - MinLife) + MinLife);
            float size = (float)(random.NextDouble() * (MaxSize - MinSize) + MinSize);

            float offsetX = (float)(random.NextDouble() - 0.5) * SpreadIntensity;
            float offsetY = (float)(random.NextDouble() - 0.5) * SpreadIntensity;
            float offsetZ = (float)(random.NextDouble() - 0.5) * SpreadIntensity;

            var velocity = new Vector3
            {
                x = (startX - guideX) / life + offsetX,
                y = (startY - guideY) / life + offsetY,
                z = (startZ - guideZ) / life + offsetZ
            };

            var acceleration = new Vector3
            {
                x = (float)(random.NextDouble() - 0.5) * AccelerationRandomFactor,
                y = (float)(random.NextDouble() - 0.5) * AccelerationRandomFactor,
                z = (float)(random.NextDouble() - 0.5) * AccelerationRandomFactor
            };

            var rotation = new Vector3
            {
                x = random.NextInt64(0, 360),
                y = random.NextInt64(0, 360),
                z = random.NextInt64(0, 360)
            };

            var rotationSpeed = new Vector3
            {
                x = random.NextInt64(-5, 5),
                y = random.NextInt64(-5, 5),
                z = random.NextInt64(-5, 5)
            };

            Particles.Add(new Particle
            {
                Life = life,
                Size = size,
                Velocity = velocity,
                Acceleration = acceleration,
                VariedStart = random.NextInt64(0, 5_000_000),
                ParticleTriangle = new TriangleMeshWithColor
                {
                    Color = "eeffee",
                    vert1 = new Vector3 { x = -size / 2, y = -size / 2, z = 0 },
                    vert2 = new Vector3 { x = size / 2, y = -size / 2, z = 0 },
                    vert3 = new Vector3 { x = 0, y = size / 2, z = 0 },
                    noHidden = true
                },
                Position = new Vector3 { x = startX, y = startY, z = startZ },
                WorldPosition = new Vector3 { x = WorldPosition.x, y = WorldPosition.y, z = WorldPosition.z },
                BirthTime = DateTime.UtcNow,
                noHidden = true,
                IsRotated = false,
                Rotation = rotation,
                RotationSpeed = rotationSpeed,
                Color = "ffff00",
                Visible = false,
                ImpactStatus = new ImpactStatus { HasCrashed = false }
            });
        }
    }
}

public class Particle : IParticle
{
    public DateTime BirthTime { get; set; } = DateTime.Now;
    public ITriangleMeshWithColor ParticleTriangle { get; set; }
    public IVector3 Velocity { get; set; }
    public IVector3 Acceleration { get; set; }
    public float Life { get; set; }
    public float Size { get; set; }
    public string Color { get; set; }
    public bool noHidden { get; set; }
    public long VariedStart { get; set; }
    public bool IsRotated { get; set; }
    public IVector3? Rotation { get; set; }
    public IVector3? Position { get; set; }
    public IVector3 WorldPosition { get; set; }
    public IVector3 GlobalMapPosition { get; set; }
    public IVector3? RotationSpeed { get; set; }
    public bool? NoShading { get; set; }
    public bool Visible { get; set; }
    public IImpactStatus? ImpactStatus { get; set; }
    public IPhysics? Physics { get; set; } = new Physics();
}
