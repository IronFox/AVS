using AVS.BaseVehicle;
using HarmonyLib;
using System.Linq;
using AVS.Util;
using AVS.VehicleBuilding;
using UnityEngine;

// PURPOSE: Prevent Drones from accessing upgrades. Display upgrade module models when appropriate. Display custom upgrade-background images.
// VALUE: High. Drones would have odd behavior otherwise, and the other functions are important developer utilities.

namespace AVS.Patches;

/// <summary>
/// Provides functionality to patch the behavior of the VehicleUpgradeConsoleInput
/// in order to prevent drones from accessing upgrades, ensure proper display
/// of upgrade module models, and utilize custom upgrade background images.
/// </summary>
/// <remarks>
/// This patch is critical for maintaining appropriate behavior and providing
/// essential developer utilities within the upgrade console system.
/// </remarks>
[HarmonyPatch(typeof(VehicleUpgradeConsoleInput))]
internal class VehicleUpgradeConsoleInputPatcher
{
    /// <summary>
    /// Postfix method called when the UpdateVisuals method of VehicleUpgradeConsoleInput is invoked.
    /// Updates the slot visuals for the vehicle upgrade console by verifying the active equipment
    /// against available slots and activating or deactivating the corresponding models.
    /// </summary>
    /// <param name="__instance">The instance of VehicleUpgradeConsoleInput for which UpdateVisuals was called.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(VehicleUpgradeConsoleInput.UpdateVisuals))]
    public static void UpdateVisualsPostfix(VehicleUpgradeConsoleInput __instance)
    {
        var mv = __instance.GetComponentInParent<AvsVehicle>();
        if (mv != null && __instance.GetComponentInChildren<UpgradeProxy>() != null &&
            __instance.GetComponentInChildren<UpgradeProxy>().slots != null)
        {
            var log = mv.Log.Tag(nameof(UpdateVisualsPostfix));
            var proxy = __instance.GetComponentInChildren<UpgradeProxy>();
            if (proxy == null || proxy.slots == null)
            {
                log.Error("proxy or proxy.slots is null");
                return;
            }

            __instance.slots = proxy.slots.ToArray();
            for (var i = 0; i < __instance.slots.Length; i++)
            {
                var slot = __instance.slots[i];
                var model = slot.model;
                log.Write($"Slot {i} : {slot.id} : {model.NiceName()}");
                if (model != null)
                {
                    var active = __instance.equipment != null &&
                                 __instance.equipment.GetTechTypeInSlot(slot.id) > TechType.None;
                    model.SetActive(active);
                    log.Write(
                        $"Active: {active}, equipment={__instance.equipment != null}");
                }
            }
        }
    }

    /// <summary>
    /// Postfix method that triggers when the VehicleUpgradeConsoleInput's OnHandClick method is invoked.
    /// It identifies any active vehicles associated with the provided VehicleUpgradeConsoleInput instance
    /// and signals the opening of the module using ModuleBuilder.
    /// </summary>
    /// <param name="__instance">The instance of VehicleUpgradeConsoleInput that triggered the OnHandClick event.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(VehicleUpgradeConsoleInput.OnHandClick))]
    public static void VehicleUpgradeConsoleInputOnHandClickHarmonyPostfix(VehicleUpgradeConsoleInput __instance)
    {
        foreach (var mv in AvsVehicleManager.VehiclesInPlay.Where(x => x != null))
            if (mv.upgradesInput == __instance)
            {
                ModuleBuilder.Main.SignalOpened(__instance, mv);

                break;
            }
    }
}