using HarmonyLib;
using UnityEngine;
using AVS.Util;

// PURPOSE: Ensure sleeping in a bed in a submarine doesn't cause the vehicle to drift (issues caused by the animation)
// VALUE: high, for the sake of world consistency

namespace AVS.Patches;

/// <summary>
/// Patches the behavior of the <see cref="Bed"/> class to ensure that a submarine vehicle remains stationary while the player is sleeping in a bed.
/// </summary>
/// <remarks>
/// Addresses issues caused by bed usage animations that could otherwise inadvertently affect the position or orientation of the submarine.
/// </remarks>
[HarmonyPatch(typeof(Bed))]
public class BedPatcher
{
    private static Quaternion initialSubRotation = Quaternion.identity;
    private static Vector3 initialSubPosition = Vector3.zero;

    /// <summary>
    /// Postfix method that modifies the behavior of the <c>Bed.EnterInUseMode</c> method.
    /// </summary>
    /// <remarks>
    /// This method is executed after the original <c>Bed.EnterInUseMode</c> method.
    /// It enables the execution of AvsVehicle-specific handling, ensuring that piloting animations are properly executed
    /// for the vehicle currently controlled by the player. If the player is not piloting an AvsVehicle,
    /// this method performs no additional actions.
    /// </remarks>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Bed.EnterInUseMode))]
    public static void BedEnterInUseModePostfix(Bed __instance)
    {
        var sub = Player.main.GetVehicle() as VehicleTypes.Submarine;
        if (sub.IsNotNull() && __instance.inUseMode == Bed.InUseMode.Sleeping)
        {
            // freeze the sub
            initialSubRotation = sub.transform.rotation;
            initialSubPosition = sub.transform.position;
        }
    }

    /// <summary>
    /// Postfix method that modifies the behavior of the <c>Bed.Update</c> method.
    /// </summary>
    /// <remarks>
    /// Ensures that the submarine vehicle's position and rotation remain consistent while the player is in the sleeping animation mode.
    /// This prevents unwanted movement or orientation changes to the submarine while the <c>Bed</c> is in use.
    /// </remarks>
    /// <param name="__instance">The <c>Bed</c> instance being updated.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Bed.Update))]
    public static void UpdatePostfix(Bed __instance)
    {
        var sub = Player.main.GetVehicle() as VehicleTypes.Submarine;
        if (sub.IsNotNull() && __instance.inUseMode == Bed.InUseMode.Sleeping)
        {
            sub.transform.rotation = initialSubRotation;
            sub.transform.position = initialSubPosition;
        }
    }
}