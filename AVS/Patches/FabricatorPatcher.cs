using AVS.BaseVehicle;
using AVS.Util;
using HarmonyLib;

// PURPOSE: Ensure onboard fabricators are correctly powered. Ensure the constructor cannot build two MVs at once.
// VALUE: High.

namespace AVS.Patches
{
    [HarmonyPatch(typeof(GhostCrafter))]
    public static class FabricatorPatcher
    {
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

    [HarmonyPatch(typeof(CrafterLogic))]
    public static class CrafterLogicPatcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CrafterLogic.ConsumeEnergy))]
        public static bool ConsumeEnergyPrefix(CrafterLogic __instance, ref bool __result, PowerRelay powerRelay, float amount)
        {
            if (!GameModeUtils.RequiresPower())
            {
                // If this is Creative or something, don't even bother
                return true;
            }
            if (powerRelay.powerPreview == null)
            {
                // if powerRelay.powerPreview was null, we must be talking about a ModVehicle
                // (it was never assigned because PowerRelay.Start is skipped for ModVehicles)
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
