using AVS.Util;
using HarmonyLib;

// PURPOSE: player is "normally grounded" while inside a Submarine
// VALUE: High.

namespace AVS.Patches;

/// <summary>
/// Harmony patch class for modifying the behavior of the <see cref="PlayerController"/> class.
/// </summary>
/// <remarks>
/// This patch ensures that the player always behaves as if "normally grounded" while inside an AVS vehicle of
/// type <see cref="VehicleTypes.Submarine"/>.
/// It prevents the player from performing swim-related actions when inside a submarine vehicle.
/// </remarks>
[HarmonyPatch(typeof(PlayerController))]
public class PlayerControllerPatcher
{
    /*
     * This patch ensures the Player behaves as expected inside a AvsVehicle.
     * That is, the player should always act as "normally grounded."
     * This patch prevents the player from doing any swim-related behaviors while inside a AvsVehicle
     */
    /// <summary>
    /// Harmony prefix patch method for modifying the behavior of the HandleUnderWaterState method in the PlayerController class.
    /// Ensures the player behaves as if grounded and disables swim-related actions when inside a submarine vehicle.
    /// </summary>
    /// <param name="__instance">The instance of the PlayerController being patched.</param>
    /// <returns>
    /// Returns false if the player is inside a submarine and not controlling it, preventing the original method from executing.
    /// Returns true otherwise, allowing the original method to execute.
    /// </returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(PlayerController.HandleUnderWaterState))]
    public static bool HandleUnderWaterStatePrefix(PlayerController __instance)
    {
        var av = Player.main.GetVehicle() as VehicleTypes.Submarine;
        if (av.IsNotNull() && !av.IsPlayerControlling())
        {
            __instance.inVehicle = true;
            __instance.underWater = false;
            __instance.groundController.SetEnabled(false);
            __instance.underWaterController.SetEnabled(false);
            __instance.activeController = __instance.groundController;
            __instance.desiredControllerHeight = __instance.standheight;
            __instance.activeController.SetControllerHeight(__instance.currentControllerHeight,
                __instance.cameraOffset);
            __instance.activeController.SetEnabled(true);
            return false;
        }

        return true;
    }
}