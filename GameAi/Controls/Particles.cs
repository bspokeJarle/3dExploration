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
    private readonly Random random = new();
    private readonly List<IParticle> _deadParticles = new();

    // --- Konfigurasjon ---
    private const int MaxParticlesBase = 12;
    private const int MaxThrustMultiplier = 5;
    private const int MaxDynamicParticles = 25;
    private const float MinLife = 2.5f;
    private const float MaxLife = 3.5f;
    private const float MinSize = 1f;
    private const float MaxSize = 4f;
    private const float SpreadIntensity = 3f;
    private const float AccelerationRandomFactor = 0.1f;
    private const float FadeFactor = 0.03f;
    private const float InitialThrottleFactor = 5f;

    public float ThrottleDurationFactor { get; set; } = 0.3f;
    public List<IParticle> Particles { get; set; } = new();
    public IObjectMovement? ParentShip { get; set; }
    public bool Visible { get; set; }
    public bool EnableParticleLogging { get; set; } = false;

    private DateTime _lastUpdateTime = DateTime.UtcNow;

    public void MoveParticles()
    {
        _deadParticles.Clear();
        DateTime now = DateTime.UtcNow;
        long currentTicks = now.Ticks;

        float deltaTime = (float)(now - _lastUpdateTime).TotalSeconds;
        if (deltaTime <= 0f || deltaTime > 1f) deltaTime = 1f / 60f;
        _lastUpdateTime = now;

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
                            Logger.Log("[Particle Bounce Triggered]");
                            Logger.Log($"   Object Name        : {particle.ImpactStatus.ObjectName}");
                            Logger.Log($"   Position (Local)   : x={particle.Position?.x:F2}, y={particle.Position?.y:F2}, z={particle.Position?.z:F2}");
                            Logger.Log($"   Impact Direction   : {particle.ImpactStatus.ImpactDirection}");
                            Logger.Log($"   Velocity Before    : x={particle.Physics.Velocity.x:F2}, y={particle.Physics.Velocity.y:F2}, z={particle.Physics.Velocity.z:F2}");
                        }

                        particle.Physics.Bounce(new Vector3(0, 0, 0), particle.ImpactStatus.ImpactDirection);
                        particle.Life *= 0.8f;
                        particle.ImpactStatus.HasCrashed = false;

                        if (Logger.EnableFileLogging && EnableParticleLogging)
                        {
                            Logger.Log("After Bounce Velocity: " + particle.Physics.Velocity);
                        }
                    }

                    long ageTicks = currentTicks - particle.BirthTime.Ticks - particle.VariedStart;
                    if (ageTicks <= boostTicks)
                    {
                        particle.Physics.Velocity = PhysicsHelpers.Add(
                            particle.Physics.Velocity,
                            PhysicsHelpers.Multiply(particle.Physics.Velocity, InitialThrottleFactor * deltaTime)
                        );
                    }

                    particle.Position = particle.Physics.ApplyGravityForce(particle.Position ?? new Vector3(), deltaTime);
                }
                else
                {
                    particle.Velocity.x += particle.Acceleration.x;
                    particle.Velocity.y += particle.Acceleration.y;
                    particle.Velocity.z += particle.Acceleration.z;

                    particle.Velocity.x *= 0.95f;
                    particle.Velocity.y *= 0.95f;
                    particle.Velocity.z *= 0.95f;

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
                _deadParticles.Add(particle);
            }
        }

        foreach (var deadParticle in _deadParticles)
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

    public void ReleaseParticles(ITriangleMeshWithColor trajectory, ITriangleMeshWithColor startPosition, IVector3 worldPosition, IObjectMovement parentShip, int thrust)
    {
        if (thrust == 0)
        {
            Particles.Clear();
            Particles.TrimExcess();
            return;
        }

        int dynamicMaxParticles = Math.Min(MaxDynamicParticles, thrust * 2 + MaxParticlesBase);
        if (Particles.Count >= dynamicMaxParticles) return;
        if (startPosition == null || trajectory == null) return;

        ParentShip = parentShip;
        int particleCount = Math.Min(thrust * MaxThrustMultiplier, dynamicMaxParticles - Particles.Count);

        var startPos = new Vector3(
            (startPosition.vert1.x + startPosition.vert2.x + startPosition.vert3.x) / 3,
            (startPosition.vert1.y + startPosition.vert2.y + startPosition.vert3.y) / 3,
            (startPosition.vert1.z + startPosition.vert2.z + startPosition.vert3.z) / 3
        );

        var guidePos = new Vector3(
            (trajectory.vert1.x + trajectory.vert2.x + trajectory.vert3.x) / 3,
            (trajectory.vert1.y + trajectory.vert2.y + trajectory.vert3.y) / 3,
            (trajectory.vert1.z + trajectory.vert2.z + trajectory.vert3.z) / 3
        );

        for (int i = 0; i < particleCount; i++)
        {
            float life = (float)(random.NextDouble() * (MaxLife - MinLife) + MinLife);
            float size = (float)(random.NextDouble() * (MaxSize - MinSize) + MinSize);
            float spread = SpreadIntensity * (float)(random.NextDouble() + 0.5);

            float offsetX = (float)(random.NextDouble() - 0.5) * spread;
            float offsetY = (float)(random.NextDouble() - 0.5) * spread;
            float offsetZ = (float)(random.NextDouble() - 0.5) * spread;

            var velocity = new Vector3
            {
                x = (startPos.x - guidePos.x) / life + offsetX,
                y = (startPos.y - guidePos.y) / life + offsetY,
                z = (startPos.z - guidePos.z) / life + offsetZ
            };

            var acceleration = new Vector3
            {
                x = (float)(random.NextDouble() - 0.5) * AccelerationRandomFactor,
                y = (float)(random.NextDouble() - 0.5) * AccelerationRandomFactor,
                z = (float)(random.NextDouble() - 0.5) * AccelerationRandomFactor
            };

            Particles.Add(new Particle
            {
                Life = life,
                Size = size,
                Velocity = velocity,
                Acceleration = acceleration,
                VariedStart = random.NextInt64(0, 500_000),
                ParticleTriangle = new TriangleMeshWithColor
                {
                    Color = "eeffee",
                    vert1 = new Vector3 { x = -size / 2, y = -size / 2, z = 0 },
                    vert2 = new Vector3 { x = size / 2, y = -size / 2, z = 0 },
                    vert3 = new Vector3 { x = 0, y = size / 2, z = 0 },
                    noHidden = true
                },
                Position = new Vector3 { x = startPos.x, y = startPos.y, z = startPos.z },
                WorldPosition = new Vector3 { x = worldPosition.x, y = worldPosition.y, z = worldPosition.z },
                BirthTime = DateTime.UtcNow,
                noHidden = true,
                IsRotated = false,
                Rotation = new Vector3(random.NextInt64(0, 360), random.NextInt64(0, 360), random.NextInt64(0, 360)),
                RotationSpeed = new Vector3(random.NextInt64(-5, 5), random.NextInt64(-5, 5), random.NextInt64(-5, 5)),
                Color = "ffff00",
                Visible = false,
                Physics = new Physics
                {
                    Velocity = new Vector3 { x = velocity.x, y = velocity.y, z = velocity.z },
                    Acceleration = new Vector3 { x = acceleration.x, y = acceleration.y, z = acceleration.z },
                    Mass = 1f,
                    GravityStrength = 50f,
                    Friction = 0.02f,
                    EnergyLossFactor = 0.28f,
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
