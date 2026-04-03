using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonUtilities.CommonSetup
{
    //TODO: Expand this as needed, common enemy properties can go here
    public static class WeaponSetup
    {
        public static List<(string, int)> WeaponTypes = new List<(string, int)>
        {
            ("Lazer", 55),
            ("Bullet", 21),
            ("Rocket", 31)
        };

        // Lazer crashbox extents (local coords, adjustable per axis)
        // X = lateral (left/right), Y = longitudinal (beam direction), Z = vertical (up/down)
        public const float LazerCrashBoxMinX = -70f;
        public const float LazerCrashBoxMaxX = 70f;
        public const float LazerCrashBoxMinY = -600f;  // extended forward along beam
        public const float LazerCrashBoxMaxY = -30f;   // slightly closer to muzzle
        public const float LazerCrashBoxMinZ = -10f;
        public const float LazerCrashBoxMaxZ = 120f;

        // Bullet crashbox extents (local coords, adjustable per axis)
        // X = lateral (left/right), Y = longitudinal (bullet direction), Z = vertical (up/down)
        public const float BulletCrashBoxMinX = -8f;
        public const float BulletCrashBoxMaxX = 8f;
        public const float BulletCrashBoxMinY = -40f;   // forward along bullet
        public const float BulletCrashBoxMaxY = -4f;    // rear end near muzzle
        public const float BulletCrashBoxMinZ = 20f;
        public const float BulletCrashBoxMaxZ = 36f;

        // Lazer exit point fine-tuning (applied on top of the midpoint between start and guide)
        // X = lateral shift (left/right on screen)
        // Y = vertical shift on screen (+ moves down toward ship, - moves up away from ship)
        // Z = depth/perspective (changes apparent size, not position)
        public static float LazerExitOffsetX = 0f;
        public static float LazerExitOffsetY = 0f;
        public static float LazerExitOffsetZ = 0;

        // Bullet exit point fine-tuning (applied on top of the midpoint between start and guide)
        // X = lateral shift (left/right on screen)
        // Y = vertical shift on screen (+ moves down toward ship, - moves up away from ship)
        // Z = depth/perspective (changes apparent size, not position)
        public static float BulletExitOffsetX = 0f;
        public static float BulletExitOffsetY = 0f;
        public static float BulletExitOffsetZ = 0f;

        // -------------------------------------------------------
        // Aim assist per weapon
        // ConeDot  = cosine of the half-angle cone in which assist activates
        //            (higher = tighter cone = player must aim closer to target)
        // Strength = blend factor toward enemy (0 = no assist, 1 = full snap)
        // MaxRange = max distance (screen units) to consider enemies
        // -------------------------------------------------------
        public static float LazerAimAssistConeDot  = 0.96f;   // ~16° half-angle
        public static float LazerAimAssistStrength = 0.6f;
        public static float LazerAimAssistMaxRange = 2000f;

        public static float BulletAimAssistConeDot  = 0.85f;  // ~21° half-angle
        public static float BulletAimAssistStrength = 0.7f;
        public static float BulletAimAssistMaxRange = 2000f;

        public static float RocketAimAssistConeDot  = 0.85f;  // ~32° half-angle
        public static float RocketAimAssistStrength = 0.8f;
        public static float RocketAimAssistMaxRange = 8000f;

        public static bool IsWeaponTypeValid(string weaponName)
        {
            return WeaponTypes.Any(w => w.Item1 == weaponName);
        }
        public static int GetWeaponDamage(string weaponName)
        {
            var weapon = WeaponTypes.FirstOrDefault(w => w.Item1 == weaponName);
            return weapon != default ? weapon.Item2 : 0;
        }
    }
}