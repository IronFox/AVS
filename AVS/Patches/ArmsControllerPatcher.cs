using HarmonyLib;

// PURPOSE: Allow custom handling of the player's body during AvsVehicle piloting
// VALUE: Moderate-low. Convenient for developers. Maybe better to do it in a per-vehicle way

namespace AVS.Patches
{
    /// <summary>
    /// A Harmony patching class for modifying the behavior of the <see cref="ArmsController"/> class during its update cycle.
    /// </summary>
    /// <remarks>
    /// This patch adds support for custom handling of player animations and interactions while piloting an AvsVehicle.
    /// It hooks into the <c>ArmsController.Update</c> method and enables the execution of AvsVehicle-specific piloting animations,
    /// based on the current vehicle being controlled by the player.
    /// </remarks>
    [HarmonyPatch(typeof(ArmsController))]
    public class ArmsControllerPatcher
    {
        /// <summary>
        /// Postfix method that modifies the behavior of the <c>ArmsController.Update</c> method.
        /// </summary>
        /// <remarks>
        /// This method is executed after the original <c>ArmsController.Update</c> method.
        /// It enables AvsVehicle-specific handling, ensuring that piloting animations are properly executed
        /// for the vehicle currently controlled by the player. If the player is not piloting an AvsVehicle,
        /// this method performs no additional actions.
        /// </remarks>
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
