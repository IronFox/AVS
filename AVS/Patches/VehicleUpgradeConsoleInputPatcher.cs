using HarmonyLib;
using System.Linq;
using UnityEngine;

// PURPOSE: Prevent Drones from accessing upgrades. Display upgrade module models when appropriate. Display custom upgrade-background images.
// VALUE: High. Drones would have odd behavior otherwise, and the other functions are important developer utilities.

namespace AVS.Patches
{
    [HarmonyPatch(typeof(VehicleUpgradeConsoleInput))]
    class VehicleUpgradeConsoleInputPatcher
    {


        [HarmonyPostfix]
        [HarmonyPatch(nameof(VehicleUpgradeConsoleInput.UpdateVisuals))]
        public static void UpdateVisualsPostfix(VehicleUpgradeConsoleInput __instance)
        {
            if (__instance.GetComponentInParent<ModVehicle>() != null && __instance.GetComponentInChildren<UpgradeProxy>() != null && __instance.GetComponentInChildren<UpgradeProxy>().slots != null)
            {
                __instance.slots = __instance.GetComponentInChildren<UpgradeProxy>().slots.ToArray();
                for (int i = 0; i < __instance.slots.Length; i++)
                {
                    VehicleUpgradeConsoleInput.Slot slot = __instance.slots[i];
                    GameObject model = slot.model;
                    if (model != null)
                    {
                        bool active = __instance.equipment != null && __instance.equipment.GetTechTypeInSlot(slot.id) > TechType.None;
                        model.SetActive(active);
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(VehicleUpgradeConsoleInput.OnHandClick))]
        public static void VehicleUpgradeConsoleInputOnHandClickHarmonyPostfix(VehicleUpgradeConsoleInput __instance)
        {
            foreach (var mv in VehicleManager.VehiclesInPlay.Where(x => x != null))
            {
                if (mv.upgradesInput == __instance)
                {
                    ModuleBuilder.main.BackgroundSprite = mv.ModuleBackgroundImage;
                    break;
                }
            }
        }
    }
}
