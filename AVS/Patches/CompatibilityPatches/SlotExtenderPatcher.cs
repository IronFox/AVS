using AVS.VehicleBuilding;
using HarmonyLib;

// PURPOSE: Ensure AVS is compatible with Slot Extender (both can be used to full effect)
// VALUE: Very high. Excellent mod!

namespace AVS.Patches.CompatibilityPatches
{
    class SlotExtenderPatcher
    {
        /*
         * This patch is specifically for the Slot Extender mod.
         * It ensures that our AvsVehicle upgrades UI is displayed correctly.
         */
        [HarmonyPrefix]
        public static bool PrePrefix(object __instance)
        {
            if (ModuleBuilder.slotExtenderIsPatched)
            {
                return true;
            }
            else if (ModuleBuilder.slotExtenderHasGreenLight)
            {
                ModuleBuilder.slotExtenderIsPatched = true;
                return true;
            }
            return false;
        }

        [HarmonyPrefix]
        public static bool PrePostfix(object __instance)
        {
            if (ModuleBuilder.slotExtenderIsPatched)
            {
                return true;
            }
            else if (ModuleBuilder.slotExtenderHasGreenLight)
            {
                ModuleBuilder.slotExtenderIsPatched = true;
                return true;
            }
            return false;
        }
    }
}
