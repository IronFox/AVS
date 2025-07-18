using HarmonyLib;

// PURPOSE: ensures QuickSlots display as expected when inside a AvsVehicle. Prevents Drones from accessing the Player's inventory.
// VALUE: High.

namespace AVS
{
    [HarmonyPatch(typeof(PDA))]
    public class PDAPatcher
    {
        /*
         * This patch ensures our QuickSlots display as expected when inside the AvsVehicle but not piloting it.
         * That is, when piloting the AvsVehicle, we should see the AvsVehicle's modules.
         * When merely standing in the AvsVehicle, we should see our own items: knife, flashlight, scanner, etc
         */
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PDA.Close))]
        public static void ClosePostfix()
        {
            var mv = Player.main.GetVehicle() as VehicleTypes.Submarine;
            if (mv != null && !mv.IsPlayerControlling())
            {
                uGUI.main.quickSlots.SetTarget(null);
            }
        }

    }
}
