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