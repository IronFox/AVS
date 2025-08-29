using AVS.BaseVehicle;
using AVS.Util;
using HarmonyLib;
using UnityEngine;

namespace AVS.Patches.CreaturePatches;

/// <summary>
/// The <c>GhostPatcher</c> class is a Harmony patch used to modify the damage dealt by Ghost Leviathan melee attacks
/// to vehicles defined in the AVS.BaseVehicle namespace. It adjusts the bite damage value dynamically depending on
/// whether the attacker is an adult or juvenile Ghost Leviathan, and the type of the target vehicle.
/// </summary>
/// <remarks>
/// This patch ensures that the damage inflicted by Ghost Leviathans is configured to specific values set in
/// the <c>VehicleConfiguration</c> class:
/// - Seamoth and Prawn: 85
/// - Cyclops: 250
/// </remarks>
[HarmonyPatch(typeof(GhostLeviathanMeleeAttack))]
internal class GhostPatcher
{
    /// <summary>
    /// Adjusts the bite damage dealt by Ghost Leviathans when attacking AVS vehicles.
    /// </summary>
    /// <param name="__instance">The instance of the Ghost Leviathan Melee Attack being patched.</param>
    /// <param name="__result">The resulting bite damage value, modified based on target vehicle type.</param>
    /// <param name="target">The target of the Ghost Leviathan's melee attack.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(GhostLeviathanMeleeAttack.GetBiteDamage))]
    public static void GetBiteDamagePostfix(GhostLeviathanMeleeAttack __instance, ref float __result, GameObject target)
    {
        var av = target.GetComponent<AvsVehicle>();
        if (av.IsNull())
            return;

        var techType = CraftData.GetTechType(__instance.gameObject);
        if (techType == TechType.GhostLeviathan)
            __result = av.Config.GhostAdultBiteDamage;
        else if (techType == TechType.GhostLeviathanJuvenile)
            __result = av.Config.GhostJuvenileBiteDamage;
        else
        {
            using var log = av.NewAvsLog();
            log.Error("Unrecognized ghost leviathan");
        }
    }
}