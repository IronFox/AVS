using HarmonyLib;

// PURPOSE: Unlock all AvsVehicle encyclopedia entries on Creative mode start or console command "unlock"
// VALUE: High. Can't really figure out a better way to do this in general.

namespace AVS.Patches
{
    /// <summary>
    /// Provides functionality to unlock all AVS vehicle encyclopedia entries
    /// in Creative mode or via a specified console command.
    /// </summary>
    /// <remarks>
    /// This class includes a Harmony patch for the KnownTech.UnlockAll method to register
    /// AVS vehicle entries in the PDA Encyclopedia.
    /// </remarks>
    [HarmonyPatch(typeof(KnownTech))]
    public static class KnownTechPatcher
    {
        /// <summary>
        /// Harmony Postfix method to ensure that all AVS vehicle entries
        /// are unlocked in the in-game PDA encyclopedia when the
        /// KnownTech.UnlockAll method is called.
        /// </summary>
        /// <remarks>
        /// This method loops through all registered AVS vehicle types and
        /// registers their names in the PDAEncyclopedia without setting them
        /// to be discovered.
        /// </remarks>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(KnownTech.UnlockAll))]
        public static void KnownTechUnlockAllHarmonyPostfix()
        {
            AvsVehicleManager.VehicleTypes.ForEach(x => PDAEncyclopedia.Add(x.Name, false));
        }
    }
}
