using AVS.BaseVehicle;
using AVS.Util;
using HarmonyLib;
using UnityEngine;

// PURPOSE: AvsVehicle that go through MoonGates should fall to the ground
// VALUE: High.

namespace AVS.Patches;

/// <summary>
/// A Harmony patching class for modifying the behavior of the
/// <see cref="PrecursorDoorMotorModeSetter"/> when a trigger event occurs.
/// </summary>
/// <remarks>
/// This patch checks if the entering object is a suitable <see cref="AvsVehicle"/> instance
/// and adjusts its "precursor out-of-water" state based on the motor mode
/// defined by the <see cref="PrecursorDoorMotorModeSetter"/> instance.
/// </remarks>
/// <seealso cref="PrecursorDoorMotorModeSetter"/>
/// <seealso cref="AvsVehicle"/>
[HarmonyPatch(typeof(PrecursorDoorMotorModeSetter))]
public class PrecursorDoorMotorModeSetterPatcher
{
    /// <summary>
    /// Postfix patch for the OnTriggerEnter method of the PrecursorDoorMotorModeSetter class.
    /// Adjusts the behavior of the triggered event to interact with AVS vehicles.
    /// </summary>
    /// <param name="__instance">The instance of the PrecursorDoorMotorModeSetter class.</param>
    /// <param name="col">The collider that triggered the event.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PrecursorDoorMotorModeSetter.OnTriggerEnter))]
    public static void PrecursorDoorMotorModeSetterOnTriggerEnterHarmonyPostfix(PrecursorDoorMotorModeSetter __instance,
        Collider col)
    {
        if (__instance.setToMotorModeOnEnter == PrecursorDoorMotorMode.None)
            return;
        if (col.gameObject.IsNull())
            return;
        if (col.gameObject.GetComponentInChildren<IgnoreTrigger>().IsNotNull())
            return;
        var gameObject = UWE.Utils.GetEntityRoot(col.gameObject);
        if (!gameObject)
            gameObject = col.gameObject!;
        var componentInHierarchy2 = UWE.Utils.GetComponentInHierarchy<AvsVehicle>(gameObject);
        if (componentInHierarchy2)
        {
            var precursorDoorMotorMode = __instance.setToMotorModeOnEnter;
            if (precursorDoorMotorMode == PrecursorDoorMotorMode.Auto)
            {
                componentInHierarchy2.precursorOutOfWater = false;
                return;
            }

            if (precursorDoorMotorMode != PrecursorDoorMotorMode.ForceWalk)
                return;
            componentInHierarchy2.precursorOutOfWater = true;
        }
    }
}