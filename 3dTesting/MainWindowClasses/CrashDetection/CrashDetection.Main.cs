using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.Helpers
{
    public static partial class CrashDetection
    {
        public static DateTime staticOjectLastCheck { get; set; } = new DateTime();
        private static DateTime _lastStaticCheck = DateTime.MinValue;
        private static bool _skipParticles = false;

        public static void HandleCrashboxes(List<_3dObject> activeWorld, bool isPaused)
        {
            numFrame++;
            ResetFrameCachesIfNeeded();

            int count = activeWorld.Count;
            bool shouldCheckStaticObjects = (DateTime.Now - _lastStaticCheck).TotalMilliseconds > 100;
            _skipParticles = !_skipParticles;

            GameState.ShipState.BestCandidateStates.Clear();

            for (int i = 0; i < count; i++)
            {
                var inhabitant = activeWorld[i];
                if (inhabitant == null || inhabitant.CrashBoxes == null || inhabitant.CrashBoxes.Count == 0) continue;
                if (inhabitant.ImpactStatus?.HasExploded == true) continue;

                for (int j = i + 1; j < count; j++)
                {
                    var otherInhabitant = activeWorld[j];
                    if (otherInhabitant == null || otherInhabitant.CrashBoxes == null || otherInhabitant.CrashBoxes.Count == 0) continue;
                    if (otherInhabitant.ImpactStatus?.HasExploded == true) continue;

                    var flagsA = GetTypeFlagsCached(inhabitant);
                    var flagsB = GetTypeFlagsCached(otherInhabitant);

                    bool isInhabitantStatic = flagsA.IsStatic;
                    bool isOtherStatic = flagsB.IsStatic;
                    bool isInhabitantParticle = flagsA.IsParticle;
                    bool isOtherParticle = flagsB.IsParticle;
                    bool isInhabitantLazer = flagsA.IsLazer;
                    bool isOtherLazer = flagsB.IsLazer;
                    bool isInhabitantSeeder = flagsA.IsSeeder;
                    bool isOtherSeeder = flagsB.IsSeeder;
                    bool isSeeder = isInhabitantSeeder || isOtherSeeder;
                    bool isParticle = isInhabitantParticle || isOtherParticle;
                    bool isLazer = isInhabitantLazer || isOtherLazer;
                    bool isWeapon = flagsA.IsWeapon || flagsB.IsWeapon;
                    bool isWeaponShipPair =
                        (flagsA.IsWeapon && flagsB.IsShip) ||
                        (flagsB.IsWeapon && flagsA.IsShip);
                    bool isBothParticles = isInhabitantParticle && isOtherParticle;
                    bool isShip = flagsA.IsShip || flagsB.IsShip;
                    bool isSurface = flagsA.IsSurface || flagsB.IsSurface;
                    bool isKamikazeDroneSurfacePair =
                        (flagsA.Name == "KamikazeDrone" && flagsB.IsSurface) ||
                        (flagsB.Name == "KamikazeDrone" && flagsA.IsSurface);
                    bool isSeederSurfacePair =
                        (flagsA.Name == "Seeder" && flagsB.IsSurface) ||
                        (flagsB.Name == "Seeder" && flagsA.IsSurface);
                    bool isDecoySurfacePair =
                        (flagsA.Name == "DroneDecoy" && flagsB.IsSurface) ||
                        (flagsB.Name == "DroneDecoy" && flagsA.IsSurface);
                    bool isDecoyShipPair =
                        (flagsA.Name == "DroneDecoy" && flagsB.IsShip) ||
                        (flagsB.Name == "DroneDecoy" && flagsA.IsShip);
                    bool isPowerUp = flagsA.Name == "PowerUp" || flagsB.Name == "PowerUp";
                    bool isPowerUpShipPair =
                        (flagsA.Name == "PowerUp" && flagsB.IsShip) ||
                        (flagsB.Name == "PowerUp" && flagsA.IsShip);
                    bool isEnemySurfacePair = (flagsA.IsEnemy && flagsB.IsSurface) || (flagsB.IsEnemy && flagsA.IsSurface);
                    bool isBomberBombSurfacePair =
                        (flagsA.Name == "BomberBomb" && flagsB.IsSurface) ||
                        (flagsB.Name == "BomberBomb" && flagsA.IsSurface);
                    bool isBothEnemies = flagsA.IsEnemy && flagsB.IsEnemy;

                    if (string.IsNullOrEmpty(flagsA.Name) || string.IsNullOrEmpty(flagsB.Name)) continue;
                    if (flagsA.Name == flagsB.Name) continue;
                    if (isInhabitantStatic && isOtherStatic) continue;
                    if ((isInhabitantStatic || isOtherStatic) && !shouldCheckStaticObjects) continue;
                    if (isParticle && isShip) continue;
                    if (isBothParticles) continue;
                    if (isParticle && _skipParticles) continue;
                    if (isEnemySurfacePair && !isBomberBombSurfacePair) continue;
                    if (isBothEnemies) continue;
                    if (isDecoySurfacePair) continue;
                    if (isDecoyShipPair) continue;
                    if (isPowerUp && !isPowerUpShipPair) continue;
                    if (isWeaponShipPair) continue;
                    if (isLazer && isParticle || isSeeder && isParticle) continue;

                    if (isInhabitantStatic || isOtherStatic) _lastStaticCheck = DateTime.Now;

                    if (isPaused && !LogOnlyCollisions)
                    {
                        if (CheckLogFilter(inhabitant, otherInhabitant))
                        {
                            LogSnapShots(inhabitant, otherInhabitant);
                        }
                    }

                    var centerA = GetCenterCached(inhabitant);
                    var centerB = GetCenterCached(otherInhabitant);

                    double distance = _3dObjectHelpers.GetDistance(centerA, centerB);

                    if (CommonUtilities.CommonSetup.EnemySetup.IsEnemyTypeValid(inhabitant.ObjectName) && distance < MaxCrashDistance * 2)
                    {
                        CommonUtilities.CommonGlobalState.GameState.ShipState.BestCandidateStates.Add(new BestCandidateState
                        {
                            BestEnemyCandidate = new EnemyCandidateInfo
                            {
                                EnemyObject = inhabitant,
                                EnemyCenterPosition = centerA
                            },
                            TimeStampUtc = DateTime.UtcNow
                        });

                    }

                    LogNonCollision(inhabitant, otherInhabitant,
                        $"[DISTANCE CHECK] [FRAME:{numFrame}] {inhabitant.ObjectName} vs {otherInhabitant.ObjectName} = {distance:F2}");

                    if (isLazer || isOtherLazer)
                    {
                        if (!LogOnlyCollisions && ShouldLogPair(inhabitant, otherInhabitant))
                        {
                            var inhabitantCrashText = string.Join(" | ",
                                inhabitant.CrashBoxes.Select((box, idx) =>
                                    $"Box{idx}: " + string.Join(", ", box.Select(v => $"({v.x:F2},{v.y:F2},{v.z:F2})"))));

                            var otherCrashText = string.Join(" | ",
                                otherInhabitant.CrashBoxes.Select((box, idx) =>
                                    $"Box{idx}: " + string.Join(", ", box.Select(v => $"({v.x:F2},{v.y:F2},{v.z:F2})"))));

                            Logger.Log($"[CHECKLAZER] {inhabitant.ObjectName} CrashBox: {inhabitantCrashText} and {otherInhabitant.ObjectName} LocalCrash: {otherCrashText}");
                        }
                    }

                    var effectiveMaxCrashDistance = isLazer ? MaxCrashDistance * 2 : MaxCrashDistance;
                    // Ship is in screen-space; enemies are in world-offset space.
                    // The center-distance check is meaningless for Ship↔Enemy pairs,
                    // so skip the early-out and let box-vs-box handle it.
                    // Same for BomberBomb↔Surface: the Surface center is far from
                    // any individual bomb, so let box-vs-box decide.
                    bool isShipEnemyPair = isShip && !isSurface && !isParticle && !isLazer;
                    if (!isShipEnemyPair && !isBomberBombSurfacePair && distance > effectiveMaxCrashDistance)
                    {
                        SkippedByDistance++;
                        continue;
                    }

                    if (isParticle)
                    {
                        if (HandleParticleCollision(inhabitant, otherInhabitant))
                            continue;
                    }
                    else
                    {
                        if (HandleGeneralCollision(inhabitant, otherInhabitant))
                            continue;
                    }
                }
            }

            HandleDecoyBlastDamage(activeWorld);
            HandleBomberBombBlastDamage(activeWorld);

            if (ShouldLogAny && !LogOnlyCollisions)
            {
                Logger.Log($"[CACHE] Hits: {CacheHits}, Misses: {CacheMisses}, Efficiency: {(CacheHits + CacheMisses == 0 ? 0 : (int)(100.0 * CacheHits / (CacheHits + CacheMisses)))}%");
                Logger.Log($"[DISTANCE SKIP] Skipped {SkippedByDistance} pairs due to distance > {MaxCrashDistance}");
            }
        }
    }
}
