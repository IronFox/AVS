using HarmonyLib;

// PURPOSE: Allow custom handling of the player's body during AvsVehicle piloting
// VALUE: Moderate-low. Convenient for developers. Maybe better to do it in a per-vehicle way

namespace AVS.Patches
{
    [HarmonyPatch(typeof(ArmsController))]
    public class ArmsControllerPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ArmsController.Update))]
        public static void ArmsControllerUpdatePostfix()
        {
            var mv = Player.main.GetAvsVehicle();
            if (mv != null)
            {
                mv.HandlePilotingAnimations();
            }
        }
    }
}
