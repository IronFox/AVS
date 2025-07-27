using AVS.Crafting;
using AVS.UpgradeModules;
using HarmonyLib;

// PURPOSE: allow VF upgrades for the Prawn to be used as expected
// VALUE: High.

namespace AVS.Patches
{
    /* This set of patches is meant to only effect Exosuits.
     * For whatever reason, Exosuit does not implement
     * OnUpgradeModuleUse or
     * OnUpgradeModuleToggle
     * So we patch those in Vehicle here.
     * 
     * The purpose of these patches is to let our
     * AvsVehicleUpgrades be usable.
     */
    [HarmonyPatch(typeof(Vehicle))]
    public class VehicleExosuitPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Vehicle.OnUpgradeModuleToggle))]
        public static void VehicleOnUpgradeModuleTogglePostfix(Vehicle __instance, int slotID, bool active)
        {
            var exo = __instance as Exosuit;
            if (exo != null)
            {
                TechType techType = exo.modules.GetTechTypeInSlot(exo.slotIDs[slotID]);
                var param = new ToggleActionParams
                {
                    active = active,
                    vehicle = exo,
                    slotID = slotID,
                    techType = techType
                };
                UpgradeRegistrar.OnToggleActions.ForEach(x => x(param));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Vehicle.OnUpgradeModuleUse))]
        public static void VehicleOnUpgradeModuleUsePostfix(Vehicle __instance, TechType techType, int slotID)
        {
            var exo = __instance as Exosuit;
            if (exo != null)
            {
                var param = new SelectableActionParams
                (
                    vehicle: __instance,
                    slotID: slotID,
                    techType: techType
                );
                UpgradeRegistrar.OnSelectActions.ForEach(x => x(param));

                var param2 = new SelectableChargeableActionParams
                (
                    vehicle: __instance,
                    slotID: slotID,
                    techType: techType,
                    charge: param.Vehicle.quickSlotCharge[param.SlotID],
                    slotCharge: param.Vehicle.GetSlotCharge(param.SlotID)
                );
                UpgradeRegistrar.OnSelectChargeActions.ForEach(x => x(param2));
            }
        }
    }

    [HarmonyPatch(typeof(Exosuit))]
    public class ExosuitPatcher
    {
        public static void DoSlotDown(Exosuit exo, int slotID, bool isJank)
        {
            if (!exo.playerFullyEntered || exo.ignoreInput)
            {
                return;
            }
            if (slotID < 0)
            {
                // Usually this means slotID was -1,
                // which means there was no active slot.
                return;
            }
            if (exo.GetQuickSlotCooldown(slotID) != 1f)
            {
                // cooldown isn't finished!
                return;
            }
            int slotIDToTest = isJank ? slotID - 2 : slotID;
            if (!Player.main.GetQuickSlotKeyDown(slotIDToTest) && !Player.main.GetLeftHandDown())
            {
                // we didn't actually hit the slot button!
                // (or hit the left mouse button!)
                // this prevents activation on SlotNext and SlotPrev
                return;
            }
            QuickSlotType quickSlotType = exo.GetQuickSlotType(slotID, out TechType techType);
            if (quickSlotType == QuickSlotType.Selectable)
            {
                if (exo.ConsumeEnergy(techType))
                {
                    exo.OnUpgradeModuleUse(techType, slotID);
                }
            }
            else if (quickSlotType == QuickSlotType.SelectableChargeable)
            {
                exo.quickSlotCharge[slotID] = 0f;
            }
        }
        public static void DoSlotHeld(Exosuit exo, int slotID)
        {
            if (!exo.playerFullyEntered || exo.ignoreInput)
            {
                return;
            }
            QuickSlotType quickSlotType = exo.GetQuickSlotType(slotID, out TechType techType);
            if (quickSlotType == QuickSlotType.SelectableChargeable)
            {
                exo.ChargeModule(techType, slotID);
            }
        }
        public static void DoSlotUp(Exosuit exo, int slotID)
        {
            if (!exo.playerFullyEntered || exo.ignoreInput)
            {
                return;
            }
            QuickSlotType quickSlotType = exo.GetQuickSlotType(slotID, out TechType techType);
            if (quickSlotType == QuickSlotType.SelectableChargeable)
            {
                if (exo.ConsumeEnergy(techType))
                {
                    exo.OnUpgradeModuleUse(techType, slotID);
                }
                exo.quickSlotCharge[slotID] = 0f;
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(nameof(Exosuit.SlotKeyDown))]
        public static void ExosuitSlotKeyDownPostfix(Exosuit __instance, int slotID)
        {
            DoSlotDown(__instance, slotID, true);
        }
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Exosuit.SlotKeyHeld))]
        public static void ExosuitSlotKeyHeldPostfix(Exosuit __instance, int slotID)
        {
            DoSlotHeld(__instance, slotID);
        }
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Exosuit.SlotKeyUp))]
        public static void ExosuitSlotKeyUpPostfix(Exosuit __instance, int slotID)
        {
            DoSlotUp(__instance, slotID);
        }
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Exosuit.SlotLeftDown))]
        public static void ExosuitSlotLeftDownPostfix(Exosuit __instance)
        {
            if (!AvatarInputHandler.main.IsEnabled())
            {
                return;
            }
            int slotID = __instance.activeSlot;
            DoSlotDown(__instance, slotID, false);
        }
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Exosuit.SlotLeftHeld))]
        public static void ExosuitSlotLeftHeldPostfix(Exosuit __instance)
        {
            if (!AvatarInputHandler.main.IsEnabled())
            {
                return;
            }
            int slotID = __instance.activeSlot;
            DoSlotHeld(__instance, slotID);
        }
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Exosuit.SlotLeftUp))]
        public static void ExosuitSlotLeftUpPostfix(Exosuit __instance)
        {
            if (!AvatarInputHandler.main.IsEnabled())
            {
                return;
            }
            int slotID = __instance.activeSlot;
            DoSlotUp(__instance, slotID);
        }
    }
}
