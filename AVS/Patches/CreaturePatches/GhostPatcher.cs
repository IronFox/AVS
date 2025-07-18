using AVS.BaseVehicle;
using HarmonyLib;
using UnityEngine;

// PURPOSE: configure how much damage ghost leviathans can do
// VALUE: Moderate. Convenient for developers.

namespace AVS.Patches.LeviathanPatches
{
    [HarmonyPatch(typeof(GhostLeviathanMeleeAttack))]
    class GhostPatcher
    {
        /*
         * This patch changes how much damage Ghosts will do to AvsVehicles.
         * Ghosts will do:
         * 85 to Seamoth/Prawn
         * 250 to Cyclops
         */
        [HarmonyPostfix]
        [HarmonyPatch(nameof(GhostLeviathanMeleeAttack.GetBiteDamage))]
        public static void GetBiteDamagePostfix(GhostLeviathanMeleeAttack __instance, ref float __result, GameObject target)
        {
            AvsVehicle mv = target.GetComponent<AvsVehicle>();
            if (mv == null) return;

            TechType techType = CraftData.GetTechType(__instance.gameObject);
            if (techType == TechType.GhostLeviathan)
            {
                __result = mv.Config.GhostAdultBiteDamage;
            }
            else if (techType == TechType.GhostLeviathanJuvenile)
            {
                __result = mv.Config.GhostJuvenileBiteDamage;
            }
            else
            {
                Logger.Error("ERROR: Unrecognized ghost leviathan");
            }
        }
    }
}
