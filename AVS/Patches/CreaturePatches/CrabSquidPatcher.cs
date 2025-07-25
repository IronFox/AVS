﻿using AVS.BaseVehicle;
using HarmonyLib;
using UnityEngine;

// PURPOSE: ensures the CrabSquid's EMP disables AvsVehicles gracefully
// VALUE: high

namespace AVS.Patches.CreaturePatches
{
    [HarmonyPatch(typeof(EnergyMixin))]
    class CrabSquidEnergyMixinPatcher
    {
        /*
         * This patch ensures a null dereference is handled gracefully.
         * The problem is that the game doesn't know how to handle vehicles with more than one energy mixin
         */
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
    [HarmonyPatch(typeof(EMPBlast))]
    class CrabSquidPatcher
    {
        /*
         * This patch ensures the CrabSquid's EMP disables AvsVehicles
         */
        [HarmonyPostfix]
        [HarmonyPatch(nameof(EMPBlast.OnTouch))]
        public static void OnTouchPostfix(EMPBlast __instance, Collider collider)
        {
            AvsVehicle maybeMV = collider.gameObject.GetComponentInParent<AvsVehicle>();
            if (maybeMV != null)
            {
                maybeMV.GetComponent<EnergyInterface>().DisableElectronicsForTime(__instance.disableElectronicsTime);
                __instance.ApplyAndForgetOverlayFX(maybeMV.gameObject); // TODO: not sure if this is the right GameObject to target
                GameObject.Destroy(__instance.gameObject); // MUST destroy self or else FPS tanks for reasons unknown
                return;
            }
        }
    }
}
