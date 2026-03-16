using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonUtilities.CommonSetup
{
    //TODO: Expand this as needed, common enemy properties can go here
    public static class EnemySetup
    {
        public const int KamikazeDroneHealth = 55;
        public const int KamikazeDroneCollisionDamage = 50;

        public static List<string> EnemyTypes = new List<string>
        {
            "Seeder",
            "KamikazeDrone",
            "Bomber",
            "AttackShip",
            "Endboss1",
            "Endboss2"
        };
        public static bool IsEnemyTypeValid(string objectName)
        {
            return EnemyTypes.Contains(objectName);
        }
    }
}
    