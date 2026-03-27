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
            ("Bullet", 10),
            ("Rocket", 31)
        };

        // Lazer crashbox extents (local coords, adjustable per axis)
        // X = lateral (left/right), Y = longitudinal (beam direction), Z = vertical (up/down)
        public const float LazerCrashBoxMinX = -50f;
        public const float LazerCrashBoxMaxX = 50f;
        public const float LazerCrashBoxMinY = -500f;  // extended forward along beam
        public const float LazerCrashBoxMaxY = -30f;   // slightly closer to muzzle
        public const float LazerCrashBoxMinZ = 0f;
        public const float LazerCrashBoxMaxZ = 90f;

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