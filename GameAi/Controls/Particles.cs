using Domain;
using static Domain._3dSpecificsImplementations;
using System.Collections.Generic;
using System;

public class ParticlesAI : IParticles
{
    private Random random = new();

    // --- Konfigurasjon ---
    private const int MaxParticlesBase = 30;
    private const int MaxThrustMultiplier = 5;
    private const int MaxDynamicParticles = 60;
    private const float MinLife = 2f;
    private const float MaxLife = 5f;
    private const float MinSize = 1f;
    private const float MaxSize = 4f;
    private const float SpreadIntensity = 3f;        // Mer spredning her!
    private const float DragFactor = 0.98f;
    private const float BounceLoss = 0.6f;
    private const float LifetimeLossOnBounce = 0.7f;
    private const float AccelerationRandomFactor = 0.1f;
    private const float FadeFactor = 0.01f;

    public List<IParticle> Particles { get; set; } = new();
    public IObjectMovement? ParentShip { get; set; }
    public bool Visible { get; set; }

    public void MoveParticles()
    {
        var deadParticles = new List<IParticle>();
        var currentDateTime = DateTime.UtcNow;

        foreach (var particle in Particles)
        {
            if (particle.BirthTime.Ticks + particle.VariedStart > currentDateTime.Ticks) continue;

            var deathTimeTicks = particle.BirthTime.Ticks + Convert.ToInt64(particle.Life * 10_000_000) + particle.VariedStart;
            if (deathTimeTicks > currentDateTime.Ticks)
            {
                particle.Visible = true;

                float lifeProgress = (float)(currentDateTime.Ticks - particle.BirthTime.Ticks - particle.VariedStart) / (particle.Life * 10_000_000);

                // Fade size
                particle.Size *= 1.0f - FadeFactor * lifeProgress;

                // Fargeovergang
                if (lifeProgress < 0.5f) particle.Color = "ffff00";       // Gul
                else if (lifeProgress < 0.8f) particle.Color = "ff6600";  // Oransje
                else particle.Color = "ff0000";                           // Rød

                // Friksjon / drag
                particle.Velocity.x *= DragFactor;
                particle.Velocity.y *= DragFactor;
                particle.Velocity.z *= DragFactor;

                // Akselerasjon
                particle.Velocity.x += particle.Acceleration.x;
                particle.Velocity.y += particle.Acceleration.y;
                particle.Velocity.z += particle.Acceleration.z;

                // Bevegelse
                particle.Position.x -= particle.Velocity.x;
                particle.Position.y -= particle.Velocity.y;
                particle.Position.z -= particle.Velocity.z;

                // Kollisjon mot bakken
                if (particle.Position.y <= 0)
                {
                    particle.Position.y = 0.1f;
                    particle.Velocity.y *= -BounceLoss;
                    particle.Life *= LifetimeLossOnBounce;
                }

                // Rotasjon
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

        for (int i = 0; i < particleCount; i++)
        {
            if (Particles.Count > dynamicMaxParticles) break;

            var startX = (StartPosition.vert1.x + StartPosition.vert2.x + StartPosition.vert3.x) / 3;
            var startY = (StartPosition.vert1.y + StartPosition.vert2.y + StartPosition.vert3.y) / 3;
            var startZ = (StartPosition.vert1.z + StartPosition.vert2.z + StartPosition.vert3.z) / 3;

            var guideX = (Trajectory.vert1.x + Trajectory.vert2.x + Trajectory.vert3.x) / 3;
            var guideY = (Trajectory.vert1.y + Trajectory.vert2.y + Trajectory.vert3.y) / 3;
            var guideZ = (Trajectory.vert1.z + Trajectory.vert2.z + Trajectory.vert3.z) / 3;

            float life = (float)(random.NextDouble() * (MaxLife - MinLife) + MinLife);
            float size = (float)(random.NextDouble() * (MaxSize - MinSize) + MinSize);

            var offsetX = (float)(random.NextDouble() - 0.5) * SpreadIntensity;
            var offsetY = (float)(random.NextDouble() - 0.5) * SpreadIntensity;
            var offsetZ = (float)(random.NextDouble() - 0.5) * SpreadIntensity;

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

            var particle = new Particle
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
                Rotation = new Vector3 { x = random.NextInt64(0, 360), y = random.NextInt64(0, 360), z = random.NextInt64(0, 360) },
                RotationSpeed = new Vector3 { x = random.NextInt64(-5, 5), y = random.NextInt64(-5, 5), z = random.NextInt64(-5, 5) },
                Color = "ffff00",
                Visible = false
            };

            Particles.Add(particle);
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
}