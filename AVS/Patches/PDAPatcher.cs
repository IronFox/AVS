using HarmonyLib;

// PURPOSE: ensures QuickSlots display as expected when inside a AvsVehicle. Prevents Drones from accessing the Player's inventory.
// VALUE: High.

namespace AVS.Patches
{
    /// <summary>
    /// Provides patch functionality for the PDA class through Harmony.
    /// Ensures proper QuickSlots display when the player is inside an AvsVehicle,
    /// and prevents Drones from interacting with the Player's inventory.
    /// </summary>
    [HarmonyPatch(typeof(PDA))]
    public class PDAPatcher
    {
        /// <summary>
        /// A Harmony patch method executed after the PDA.Close method.
        /// Ensures that the appropriate QuickSlots are displayed when the player
        /// is inside an AvsVehicle but not piloting it.
        /// If the player is inside the AvsVehicle and not actively controlling it,
        /// the QuickSlots will reset to the default player inventory (e.g., tools, flashlight, etc.).
        /// </summary>
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
