using AVS.BaseVehicle;
using AVS.Util;
using HarmonyLib;

// PURPOSE: Ensure onboard fabricators are correctly powered. Ensure the constructor cannot build two MVs at once.
// VALUE: High.

namespace AVS.Patches
{
    /// <summary>
    /// Provides patching functionality for fabricator and constructor-related behaviors in the AVS.BaseVehicle namespace.
    /// This class ensures onboard fabricators are correctly powered and prevents the construction of multiple vehicles concurrently.
    /// </summary>
    /// <remarks>
    /// Uses the Harmony library to apply prefix patches for specific methods in target classes such as GhostCrafter, CrafterLogic, and ConstructorInput.
    /// The primary functionality focuses on evaluating power status and preventing undesirable construction behaviors.
    /// </remarks>
    [HarmonyPatch(typeof(GhostCrafter))]
    public static class FabricatorPatcher
    {
        /// <summary>
        /// Determines whether the GhostCrafter has enough power to perform its operation.
        /// It evaluates the power status of the associated vehicle and sets the result accordingly.
        /// </summary>
        /// <param name="__instance">The instance of the GhostCrafter being evaluated.</param>
        /// <param name="__result">A reference to the boolean result indicating power sufficiency.</param>
        /// <returns>
        /// Returns false to prevent the original method from being executed, or true if the operation is unaffected.
        /// </returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GhostCrafter.HasEnoughPower))]
        public static bool HasEnoughPowerPrefix(GhostCrafter __instance, ref bool __result)
        {
            AvsVehicle mv = __instance.GetComponentInParent<AvsVehicle>();
            if (mv is null || !GameModeUtils.RequiresPower())
            {
                return true;
            }
            PowerManager.PowerStatus goodPS = new PowerManager.PowerStatus
            {
                hasFuel = true,
                isPowered = true
            };
            __result = mv.PowerManager.EvaluatePowerStatus() == goodPS;
            return false;
        }
    }

    /// <summary>
    /// Provides patching functionality for crafting logic, specifically addressing energy consumption behaviors in the AVS framework.
    /// This class enhances the logic for determining power usage within fabricators aboard vehicles, ensuring proper energy validation
    /// and power systems integration.
    /// </summary>
    /// <remarks>
    /// Implements prefix patches using the Harmony library to intervene in the CrafterLogic.ConsumeEnergy method. The patch accounts for
    /// unique scenarios where power relays may not properly function, such as within certain custom vehicle contexts provided by the AVS framework.
    /// Enables compatibility with game modes that do not require power consumption, such as Creative.
    /// </remarks>
    [HarmonyPatch(typeof(CrafterLogic))]
    public static class CrafterLogicPatcher
    {
        /// <summary>
        /// Modifies the behavior of the CrafterLogic.ConsumeEnergy method to handle unique energy consumption logic for custom vehicle scenarios.
        /// This method ensures proper energy validation and power relay handling when interacting with AVS vehicles.
        /// </summary>
        /// <param name="__instance">The instance of CrafterLogic associated with the energy consumption operation.</param>
        /// <param name="__result">A reference to a boolean result indicating whether the operation was successfully processed.</param>
        /// <param name="powerRelay">The power relay involved in the energy consumption context.</param>
        /// <param name="amount">The amount of energy required for the operation.</param>
        /// <returns>
        /// Returns true to allow the original operation to proceed, or false to override the original behavior
        /// and apply custom energy consumption logic.
        /// </returns>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CrafterLogic.ConsumeEnergy))]
        public static bool ConsumeEnergyPrefix(CrafterLogic __instance, ref bool __result, PowerRelay powerRelay,
            float amount)
        {
            if (!GameModeUtils.RequiresPower())
            {
                // If this is Creative or something, don't even bother
                return true;
            }
            if (powerRelay.powerPreview == null)
            {
                // if powerRelay.powerPreview was null, we must be talking about a AvsVehicle
                // (it was never assigned because PowerRelay.Start is skipped for AvsVehicles)
                // so let's check for one
                AvsVehicle? mv = null;
                foreach (AvsVehicle tempMV in AvsVehicleManager.VehiclesInPlay)
                {
                    if (tempMV.IsBoarded)
                    {
                        mv = tempMV;
                        break;
                    }
                }
                if (mv == null)
                {
                    Logger.Error($"ConsumeEnergyPrefix ERROR: PowerRelay was null, but we weren't in an {nameof(AvsVehicle)}.");
                    return true;
                }
                else
                {
                    // we found the AvsVehicle from whose fabricator we're trying to drain power
                    float WantToSpend = 5f;
                    float SpendTolerance = 4.99f;
                    float energySpent = mv.PowerManager.TrySpendEnergy(WantToSpend);
                    __result = SpendTolerance <= energySpent;
                    return false;
                }
            }
            // we should never make it here... so let the base game throw an error :shrug:
            return true;
        }
    }

    /// <summary>
    /// Ensures that the ConstructorInput class properly handles vehicle construction interactions.
    /// This patch prevents multiple vehicle construction events from being executed simultaneously.
    /// </summary>
    /// <remarks>
    /// Applies a Harmony prefix patch to the OnHandClick method of the ConstructorInput class.
    /// The functionality evaluates whether the current build target is a valid AvsVehicle component
    /// and interrupts the construction process if the condition is met, ensuring proper construction behavior.
    /// </remarks>
    [HarmonyPatch(typeof(ConstructorInput))]
    public static class ConstructorInputFabricatorPatcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ConstructorInput.OnHandClick))]
        public static bool OnHandClickPrefix(ConstructorInput __instance, GUIHand hand)
        {
            if (__instance.constructor.buildTarget.SafeGetComponent<AvsVehicle>() != null)
            {
                return false;
            }
            return true;
        }
    }
}
