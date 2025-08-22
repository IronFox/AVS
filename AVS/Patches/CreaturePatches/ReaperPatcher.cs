using AVS.BaseVehicle;
using AVS.Configuration;
using AVS.Util;
using HarmonyLib;
using UnityEngine;

namespace AVS.Patches.CreaturePatches;

/// <summary>
/// A Harmony patch class that modifies the behavior of the <see cref="ReaperMeleeAttack"/> class
/// to allow Reaper Leviathans to interact with instances of <see cref="AvsVehicle"/>.
/// </summary>
/// <remarks>
/// This patch implements changes to enable Reaper Leviathans to grab an <see cref="AvsVehicle"/>
/// in a manner similar to their interaction with Seamoths. Additionally, it adjusts damage dealt
/// by the Reaper Leviathan's bite dynamically based on the <see cref="VehicleConfiguration"/> of
/// the targeted <see cref="AvsVehicle"/>.
/// </remarks>
[HarmonyPatch(typeof(ReaperMeleeAttack))]
internal class ReaperMeleeAttackPatcher
{
    /// <summary>
    /// Executes additional logic after the ReaperMeleeAttack.OnTouch method. Handles interactions with AVS vehicles specifically
    /// allowing Reaper Leviathans to grab AVS vehicles similar to how they grab Seamoths.
    /// </summary>
    /// <param name="__instance">The instance of the ReaperMeleeAttack class executing the OnTouch method.</param>
    /// <param name="collider">The collider of the object that the Reaper Leviathan interacts with.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ReaperMeleeAttack.OnTouch))]
    public static void OnTouchPostfix(ReaperMeleeAttack __instance, Collider collider)
    {
        // This postfix basically executes the OnTouch function again but only for the GrabAvsVehicle case.
        if (collider.gameObject.GetComponent<Player>().IsNotNull())
        {
            var maybeMV = collider.gameObject.GetComponent<Player>().GetAvsVehicle();
            if (maybeMV.IsNotNull())
                // Don't let the reaper grab the player from inside the AvsVehicle
                return;
        }

        var mv = collider.gameObject.GetComponentInParent<AvsVehicle>();
        if (mv.IsNotNull())
            if (__instance.liveMixin.IsAlive() && Time.time > __instance.timeLastBite + __instance.biteInterval)
            {
                var component = __instance.GetComponent<Creature>();
                if (component.Aggression.Value >= 0.5f)
                {
                    var component2 = __instance.GetComponent<ReaperLeviathan>();
                    if (!component2.IsHoldingVehicle() && !__instance.playerDeathCinematic.IsCinematicModeActive())
                    {
                        if (component2.GetCanGrabVehicle() && mv.Config.CanLeviathanGrab)
                            component2.GrabVehicle(mv, ReaperLeviathan.VehicleType.Seamoth);
                        __instance.OnTouch(collider);
                        component.Aggression.Value -= 0.25f;
                    }
                }
            }
    }

    /// <summary>
    /// Modifies the bite damage dealt by Reaper Leviathans when targeting AVS vehicles, applying custom damage values
    /// specific to the targeted vehicle's configuration.
    /// </summary>
    /// <param name="__instance">The instance of the ReaperMeleeAttack performing the bite attack.</param>
    /// <param name="__result">The original damage dealt by the Reaper Leviathan, modified to reflect the custom configuration.</param>
    /// <param name="target">The target GameObject, potentially an AVS vehicle, that the Reaper Leviathan is attacking.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ReaperMeleeAttack.GetBiteDamage))]
    public static void GetBiteDamagePostfix(ReaperMeleeAttack __instance, ref float __result, GameObject target)
    {
        var mv = target.GetComponent<AvsVehicle>();
        if (mv.IsNull()) return;

        __result = mv.Config.ReaperBiteDamage;
    }
}

/// <summary>
/// A Harmony patch class that modifies the behavior of the <see cref="ReaperLeviathan"/> class
/// to ensure proper interaction and positioning of an attached <see cref="AvsVehicle"/> when grabbed.
/// </summary>
/// <remarks>
/// This patch implements a post-fix to update the position of the <see cref="AvsVehicle"/> being
/// held by the <see cref="ReaperLeviathan"/>. If a valid grab point is defined within the vehicle,
/// the patch ensures that the vehicle's position is adjusted to align with its intended grab point,
/// maintaining consistent positioning during the interaction.
/// </remarks>
[HarmonyPatch(typeof(ReaperLeviathan))]
internal class ReaperPatcher
{
    /// <summary>
    /// Ensures the proper positioning of an attached <see cref="AvsVehicle"/> when held by the <see cref="ReaperLeviathan"/>.
    /// This method adjusts the vehicle's position to align with its defined grab point or its default position.
    /// </summary>
    /// <param name="__instance">The instance of the <see cref="ReaperLeviathan"/> currently executing the update method.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ReaperLeviathan.Update))]
    public static void UpdatePostfix(ReaperLeviathan __instance)
    {
        if (__instance.holdingVehicle is AvsVehicle v)
        {
            var gb = v.Com.LeviathanGrabPoint.Or(v.gameObject);
            if (gb.IsNotNull())
            {
                var diff = gb.transform.position - v.transform.position;
                v.transform.position -= diff;
            }
        }
    }
}