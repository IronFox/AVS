using AVS.BaseVehicle;
using HarmonyLib;
using UnityEngine;

// PURPOSE: neuter many SubRoot functions
// VALUE: High. It's valuable to have a SubRoot, but I don't want to sort these out or make them work, to be perfectly honest.

namespace AVS.Patches
{
    /// <summary>
    /// The SubRootPatcher class is a Harmony patching class for the SubRoot type.
    /// It provides prefixed methods that intercept specific methods in the SubRoot class to allow
    /// custom behavior prior to the execution of the original methods.
    /// </summary>
    [HarmonyPatch(typeof(SubRoot))]
    class SubRootPatcher
    {
        /// <summary>
        /// Determines whether the update process for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being updated.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing the update; otherwise, true to allow the update to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.Update))]
        public static bool UpdatePrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the leak amount calculation for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being evaluated.</param>
        /// <param name="__result">The resulting leak amount. If overridden, this value will be set to 0.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, halting the calculation; otherwise, true to allow the calculation to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.GetLeakAmount))]
        public static bool GetLeakAmountPrefix(SubRoot __instance, ref float __result)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                __result = 0;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the console command for flooding the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being affected by the console command.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing the command from taking effect; otherwise, true to allow the command to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.OnConsoleCommand_flood))]
        public static bool OnConsoleCommand_floodPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the execution of the 'OnConsoleCommand_crush' method for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being evaluated.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing the execution; otherwise, true to allow the execution to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.OnConsoleCommand_crush))]
        public static bool OnConsoleCommand_crushPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Intercepts the execution of the OnConsoleCommand_damagesub method for the specified SubRoot instance.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot on which the command is executed.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing the damage command; otherwise, true to allow the command execution.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.OnConsoleCommand_damagesub))]
        public static bool OnConsoleCommand_damagesubPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines if the SubRoot instance should provide an OxygenManager component.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being queried for an OxygenManager component.</param>
        /// <param name="__result">The resulting OxygenManager component, or null if the component is not available.</param>
        /// <returns>Returns false if the SubRoot instance contains a component of type AvsVehicle, overriding the default behavior; otherwise, true.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.GetOxygenManager))]
        public static bool GetOxygenManagerPrefix(SubRoot __instance, ref OxygenManager? __result)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                __result = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the OnKill process for the specified SubRoot instance should execute.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being evaluated.</param>
        /// <returns>Returns false if the SubRoot instance contains a component of type AvsVehicle, preventing the OnKill action; otherwise, true to allow the process to execute.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.OnKill))]
        public static bool OnKillPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the GetModulesRoot operation should proceed for the given SubRoot instance and sets the result accordingly.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being evaluated.</param>
        /// <param name="__result">The resulting Transform reference for the modules root, or null if the operation is prevented.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing the operation; otherwise, true to allow normal execution.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.GetModulesRoot))]
        public static bool GetModulesRootPrefix(SubRoot __instance, ref Transform? __result)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                __result = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the calculation of the world center of mass for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot whose world center of mass is being queried.</param>
        /// <param name="__result">The resulting world center of mass value if the process is bypassed.</param>
        /// <returns>Returns false and sets the world center of mass to zero if the SubRoot instance contains a component of type AvsVehicle; otherwise, true to proceed with the calculation.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.GetWorldCenterOfMass))]
        public static bool GetWorldCenterOfMassPrefix(SubRoot __instance, ref Vector3 __result)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                __result = Vector3.zero;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the collision handling for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot involved in the collision.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing collision handling; otherwise, true to allow it to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.OnCollisionEnter))]
        public static bool OnCollisionEnterPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Prevents the CrushDamageRandomPart functionality if the SubRoot instance has a component of type AvsVehicle.
        /// </summary>
        /// <param name="__instance">The SubRoot instance being evaluated.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, disabling crush damage. Otherwise, true to enable the crush damage functionality.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.CrushDamageRandomPart))]
        public static bool CrushDamageRandomPartPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Intercepts the UpdateDamageSettings method for the specified SubRoot instance.
        /// </summary>
        /// <param name="__instance">The SubRoot instance whose damage settings are being updated.</param>
        /// <returns>Returns false if the SubRoot instance contains a component of type AvsVehicle, preventing the update; otherwise, returns true to allow the update to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.UpdateDamageSettings))]
        public static bool UpdateDamageSettingsPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Handles the prefix for the ForceLightingState method on the SubRoot instance.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot on which lighting state is being forced.</param>
        /// <returns>Returns false if the SubRoot instance contains a component of type AvsVehicle, preventing execution of the original method; otherwise, true.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.ForceLightingState))]
        public static bool ForceLightingStatePrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the lighting update process for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being updated.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing the lighting update; otherwise, true to allow the update to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.UpdateLighting))]
        public static bool UpdateLightingPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the GetTemperature process for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being evaluated.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing temperature processing; otherwise, true to allow the process to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.GetTemperature))]
        public static bool GetTemperaturePrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the UpdateThermalReactorCharge process for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot for which the UpdateThermalReactorCharge is being processed.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing the thermal reactor charge update; otherwise, true to allow the process to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.UpdateThermalReactorCharge))]
        public static bool UpdateThermalReactorChargePrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the FixedUpdate process for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being processed during FixedUpdate.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing the FixedUpdate process; otherwise, true to allow the process to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.FixedUpdate))]
        public static bool FixedUpdatePrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the Cyclops upgrades process for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being modified for Cyclops upgrades.</param>
        /// <returns>Returns false if the SubRoot instance contains a component of type AvsVehicle, preventing the upgrades; otherwise, true to allow the upgrades to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.SetCyclopsUpgrades))]
        public static bool SetCyclopsUpgradesPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the SubRoot instance is allowed to proceed with the SetExtraDepth process.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being checked.</param>
        /// <returns>Returns false if the SubRoot instance contains an AvsVehicle component, halting the process; otherwise, true to allow the process to continue.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.SetExtraDepth))]
        public static bool SetExtraDepthPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the power rating update process for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being updated.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing the update; otherwise, true to allow the update to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.UpdatePowerRating))]
        public static bool UpdatePowerRatingPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines the power rating of the specified SubRoot instance.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being evaluated.</param>
        /// <param name="__result">The resulting power rating of the SubRoot instance. Defaults to 1 if overridden.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, providing a fixed power rating; otherwise, true to proceed with the default evaluation.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.GetPowerRating))]
        public static bool GetPowerRatingPrefix(SubRoot __instance, ref float __result)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                __result = 1;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the module change process for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot experiencing module changes.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing further module changes; otherwise, true to allow the process to continue.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.OnSubModulesChanged))]
        public static bool OnSubModulesChangedPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the update process for the submodules of the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being processed.</param>
        /// <returns>Returns false if the SubRoot instance contains a component of type AvsVehicle, preventing the submodules update; otherwise, true to allow the update to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.UpdateSubModules))]
        public static bool UpdateSubModulesPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the OnPlayerEntered process for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being processed when a player enters.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing further processing; otherwise, true to allow the process to continue.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.OnPlayerEntered))]
        public static bool OnPlayerEnteredPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                //Logger.DebugLog("skipping enter");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the OnPlayerExited process for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being acted on during the OnPlayerExited process.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing further processing; otherwise, true to allow the process to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.OnPlayerExited))]
        public static bool OnPlayerExitedPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                //Logger.DebugLog("skipping exit");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines the appropriate name prefix for the specified SubRoot instance.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot whose name prefix is being determined.</param>
        /// <param name="__result">The resulting name prefix for the SubRoot instance.</param>
        /// <returns>Returns false if the SubRoot instance contains a component of type AvsVehicle, setting the name prefix to "AvsVehicle" and preventing further name resolution; otherwise, true to allow normal name resolution.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.GetSubName))]
        public static bool GetSubNamePrefix(SubRoot __instance, ref string __result)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                __result = "AvsVehicle";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Handles the serialization process for the specified SubRoot instance.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being serialized.</param>
        /// <returns>Returns false if the SubRoot instance contains a component of type AvsVehicle, preventing serialization; otherwise, true to allow serialization to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.OnProtoSerialize))]
        public static bool OnProtoSerializerefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the deserialization process for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot undergoing the deserialization process.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing deserialization; otherwise, true to allow deserialization to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.OnProtoDeserialize))]
        public static bool OnProtoDeserializePrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the shield activation process for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being shielded.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing the shield activation; otherwise, true to allow the activation to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.StartSubShielded))]
        public static bool StartSubShieldedPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the EndSubShielded process for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being processed.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing the process; otherwise, true to allow the process to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.EndSubShielded))]
        public static bool EndSubShieldedPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the Cyclops power-down process should proceed for the specified SubRoot instance.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being checked for power-down compatibility.</param>
        /// <returns>Returns false to prevent the power-down process if the SubRoot instance has a component of type AvsVehicle; otherwise, true to allow the process to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.PowerDownCyclops))]
        public static bool PowerDownCyclopsPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the destruction process of the Cyclops SubRoot should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being evaluated for destruction.</param>
        /// <returns>Returns false if the SubRoot instance contains a component of type AvsVehicle, preventing destruction; otherwise, true to allow the destruction process to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.DestroyCyclopsSubRoot))]
        public static bool DestroyCyclopsSubRootPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the Awake process for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being initialized.</param>
        /// <returns>Returns false if the SubRoot instance contains a component of type AvsVehicle, preventing the Awake process; otherwise, true to allow the process to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.Awake))]
        public static bool AwakePrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the Start process for the specified SubRoot instance should proceed.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being initialized.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing the Start process; otherwise, true to allow the Start process to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.Start))]
        public static bool StartPrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the damage response logic for the specified SubRoot instance should execute.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot receiving damage.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, preventing the damage logic from executing; otherwise, true to allow the damage response to proceed.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.OnTakeDamage))]
        public static bool OnTakeDamagePrefix(SubRoot __instance)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the specified SubRoot instance is leaking.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot to check for leaks.</param>
        /// <param name="__result">A reference to the result indicating if the SubRoot is leaking. Will be set to false if the SubRoot instance has a component of type AvsVehicle.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle, bypassing the leak check; otherwise, true to proceed with the default leak determination logic.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.IsLeaking))]
        public static bool IsLeakingPrefix(SubRoot __instance, ref bool __result)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                __result = false;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether the specified SubRoot instance is underwater.
        /// </summary>
        /// <param name="__instance">The instance of the SubRoot being checked for underwater status.</param>
        /// <param name="__result">A reference to the boolean result indicating whether the SubRoot is underwater.</param>
        /// <returns>Returns false if the SubRoot instance has a component of type AvsVehicle and its underwater check is handled by that component; otherwise, true to proceed with the default check.</returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SubRoot.IsUnderwater))]
        public static bool IsUnderwaterPrefix(SubRoot __instance, ref bool __result)
        {
            if (__instance.GetComponent<AvsVehicle>())
            {
                __result = __instance.GetComponent<AvsVehicle>().GetIsUnderwater();
                return false;
            }
            return true;
        }
    }
}
