using AVS.BaseVehicle;
using HarmonyLib;
using UnityEngine;

// PURPOSE: ensures the CrabSquid's EMP disables AvsVehicles gracefully
// VALUE: high

namespace AVS.Patches.CreaturePatches
{
    /// <summary>
    /// A Harmony patch class designed to modify the behavior of the EnergyMixin class,
    /// specifically to ensure compatibility with vehicles implementing the AvsVehicle class when affected by the CrabSquid's EMP ability.
    /// </summary>
    [HarmonyPatch(typeof(EnergyMixin))]
    class CrabSquidEnergyMixinPatcher
    {
        /// <summary>
        /// Handles the behavior for setting the electronicsDisabled property in the EnergyMixin class.
        /// Ensures compatibility with vehicles implementing the AvsVehicle class when affected by disruptive effects like the CrabSquid's EMP.
        /// </summary>
        /// <param name="__instance">The instance of the EnergyMixin class being modified.</param>
        /// <param name="value">A boolean value indicating whether the electronics should be disabled.</param>
        /// <returns>Returns false if the value change is handled internally within the method, otherwise true to allow the game to handle the update.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(EnergyMixin.electronicsDisabled), MethodType.Setter)]
        public static bool electronicsDisabled(EnergyMixin __instance, bool value)
        {
            if (__instance.gameObject.GetComponentInParent<AvsVehicle>() != null)
            {
                if (value == __instance._electronicsDisabled)
                {
                    return false;
                }
                __instance._electronicsDisabled = value;
                __instance.NotifyPowered(!__instance._electronicsDisabled);
                __instance.PlayPowerSound(!__instance._electronicsDisabled);
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// A Harmony patch class designed to modify the behavior of the EMPBlast class,
    /// ensuring the CrabSquid's EMP effect interacts gracefully with vehicles that implement the AvsVehicle class.
    /// </summary>
    [HarmonyPatch(typeof(EMPBlast))]
    class CrabSquidPatcher
    {
        /// <summary>
        /// A postfix method for the OnTouch method of the EMPBlast class.
        /// Ensures the EMP effect properly interacts with vehicles implementing the AvsVehicle class by disabling their electronics and applying visual effects.
        /// </summary>
        /// <param name="__instance">The instance of the EMPBlast class.</param>
        /// <param name="collider">The collider of the GameObject that was touched by the EMP effect.</param>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(EMPBlast.OnTouch))]
        public static void OnTouchPostfix(EMPBlast __instance, Collider collider)
        {
            AvsVehicle maybeMV = collider.gameObject.GetComponentInParent<AvsVehicle>();
            if (maybeMV != null)
            {
                maybeMV.GetComponent<EnergyInterface>().DisableElectronicsForTime(__instance.disableElectronicsTime);
                __instance.ApplyAndForgetOverlayFX(maybeMV.gameObject); // TODO: not sure if this is the right GameObject to target
                Object.Destroy(__instance.gameObject); // MUST destroy self or else FPS tanks for reasons unknown
                return;
            }
        }
    }
}
