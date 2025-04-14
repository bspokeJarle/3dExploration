using Domain;
using static Domain._3dSpecificsImplementations;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using System.Windows;
using GameAiAndControls.Physics;
using GameAiAndControls.Helpers;

public class ParticlesAI : IParticles
{
    private Random random = new();

    // --- Konfigurasjon ---
    private const int MaxParticlesBase = 15;
    private const int MaxThrustMultiplier = 5;
    private const int MaxDynamicParticles = 30;
    private const float MinLife = 2.5f;
    private const float MaxLife = 3.5f;
    private const float MinSize = 1f;
    private const float MaxSize = 4f;
    private const float SpreadIntensity = 4f;
    private const float AccelerationRandomFactor = 0.1f;
    private const float FadeFactor = 0.03f;
    private const float InitialThrottleFactor = 4f; // New thrust-like impulse
    public float ThrottleDurationFactor { get; set; } = 0.3f; // Proportion of life spent with thrust boost

    public List<IParticle> Particles { get; set; } = new();
    public IObjectMovement? ParentShip { get; set; }
    public bool Visible { get; set; }
    public bool EnableParticleLogging { get; set; } = true;
    private DateTime _lastUpdateTime = DateTime.UtcNow;
   

    public void MoveParticles()
    {
        var deadParticles = new List<IParticle>();
        DateTime now = DateTime.UtcNow;
        float deltaTime = (float)(now - _lastUpdateTime).TotalSeconds;
        if (deltaTime <= 0f || deltaTime > 1f) deltaTime = 1f / 60f;
        _lastUpdateTime = now;

        long currentTicks = now.Ticks;

        foreach (var particle in Particles)
        {
            if (particle.BirthTime.Ticks + particle.VariedStart > currentTicks) continue;

            long lifeTicks = (long)(particle.Life * 10_000_000);
            long deathTicks = particle.BirthTime.Ticks + lifeTicks + particle.VariedStart;
            long boostTicks = (long)(lifeTicks * ThrottleDurationFactor);

            if (deathTicks > currentTicks)
            {
                particle.Visible = true;

                float lifeProgress = (float)(currentTicks - particle.BirthTime.Ticks - particle.VariedStart) / lifeTicks;

                particle.Size = ApplyFade(particle.Size, lifeProgress);
                particle.Color = GetColorByLifeProgress(lifeProgress);

                if (particle.Physics != null)
                {
                    if (particle.ImpactStatus?.HasCrashed == true && particle.ImpactStatus.ImpactDirection != null)
                    {
                        if (Logger.EnableFileLogging && EnableParticleLogging)
                        {
                            Logger.Log($"[Particle Bounce Triggered]");
                            Logger.Log($"   Object Name        : {particle.ImpactStatus.ObjectName}");
                            Logger.Log($"   Position (Local)   : x={particle.Position?.x:F2}, y={particle.Position?.y:F2}, z={particle.Position?.z:F2}");
                            Logger.Log($"   Impact Direction   : {particle.ImpactStatus.ImpactDirection}");
                            Logger.Log($"   Velocity Before    : x={particle.Physics.Velocity.x:F2}, y={particle.Physics.Velocity.y:F2}, z={particle.Physics.Velocity.z:F2}");
                        }

                        particle.Physics.Bounce(new Vector3(0, 0, 0), particle.ImpactStatus.ImpactDirection);

                        if (Logger.EnableFileLogging && EnableParticleLogging)
                        {
                            Logger.Log("After Bounce Velocity: " + particle.Physics.Velocity);
                        }

                        particle.Life *= 0.8f;
                        particle.ImpactStatus.HasCrashed = false;
                    }


                    // Apply throttle boost for a percentage of total life duration
                    long ageTicks = currentTicks - particle.BirthTime.Ticks - particle.VariedStart;
                    if (ageTicks <= boostTicks)
                    {
                        particle.Physics.Velocity = PhysicsHelpers.Add(
                            particle.Physics.Velocity,
                            PhysicsHelpers.Multiply(particle.Physics.Velocity, InitialThrottleFactor * deltaTime)
                        );
                    }

                    // Apply tested and validated gravity-based physics
                    particle.Position = particle.Physics.ApplyGravityForce(particle.Position ?? new Vector3(), deltaTime);
                }
                else
                {
                    particle.Velocity.x += particle.Acceleration.x;
                    particle.Velocity.y += particle.Acceleration.y;
                    particle.Velocity.z += particle.Acceleration.z;

                    particle.Velocity.x *= 0.98f;
                    particle.Velocity.y *= 0.98f;
                    particle.Velocity.z *= 0.98f;

                    particle.Position.x -= particle.Velocity.x;
                    particle.Position.y -= particle.Velocity.y;
                    particle.Position.z -= particle.Velocity.z;
                }

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

            // Use a wider spread to avoid straight-line emission
            float spread = SpreadIntensity * (float)(random.NextDouble() + 0.5); // Makes it more dynamic (1.5x to 2.0x)
            float offsetX = (float)(random.NextDouble() - 0.5) * spread;
            float offsetY = (float)(random.NextDouble() - 0.5) * spread;
            float offsetZ = (float)(random.NextDouble() - 0.5) * spread;

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
                Physics = new Physics
                {
                    Velocity = new Vector3 { x = velocity.x, y = velocity.y, z = velocity.z },
                    Acceleration = new Vector3 { x = acceleration.x, y = acceleration.y, z = acceleration.z },
                    GravityStrength = 200f,
                },
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
