using HarmonyLib;

// PURPOSE: Unlock all AvsVehicle encyclopedia entries on Creative mode start or console command "unlock"
// VALUE: High. Can't really figure out a better way to do this in general.

namespace AVS.Patches
{
    [HarmonyPatch(typeof(KnownTech))]
    public static class KnownTechPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(KnownTech.UnlockAll))]
        public static void KnownTechUnlockAllHarmonyPostfix()
        {
            AvsVehicleManager.VehicleTypes.ForEach(x => PDAEncyclopedia.Add(x.name, false));
        }
    }
}
