using AVS.BaseVehicle;
using AVS.Util;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using AVS.Log;


// PURPOSE: Allow battery charges (and Power Relay in general) to work in expected ways on AvsVehicle
// VALUE: High.

namespace AVS.Patches;
/* This set of patches allows battery chargers to work inside Submarines.
 * Submarines have a PowerRelay, but they have SubRoot.powerRelay = null.
 * The PowerRelay acts as an interface to EnergyInterface.
 * These patches ensure the PowerRelay does not Start.
 *
 * Why doesn't AvsVehicle set its powerRelay?
 * We don't set powerRelay because we need CurrentSub.
 * And CurrentSub calls OnPlayerEntered.
 * And OnPlayerEntered plays a voice notification we don't set up,
 * but only when powerRelay is not null.
 * So this avoids an error appearing
 */

/// <summary>
/// The PowerRelayPatcher class provides Harmony patches to modify the behavior of the PowerRelay
/// component within the AVS game context. These patches ensure that certain functionalities
/// of the PowerRelay operate correctly when interacting with the AvsVehicle class, particularly
/// in submarines and other vehicles where a null SubRoot.powerRelay could cause issues.
/// </summary>
/// <remarks>
/// This class addresses issues such as missing power relays in vehicles, voice notification errors,
/// handling game object interactions, and energy interface integration. It disables certain default
/// behaviors of the PowerRelay and implements replacements compatible with AvsVehicle.
/// </remarks>
/// <example>
/// This class is automatically applied through Harmony at runtime to patch the necessary methods
/// in the PowerRelay component.
/// Patched methods:
/// - <c>PowerRelay.Start</c>: Prevents PowerRelay's default Start logic if it is part of AvsVehicle.
/// - <c>PowerRelay.GetPower</c>: Overrides default power retrieval logic using the energy system of AvsVehicle.
/// - <c>PowerRelay.GetMaxPower</c>: Calculates the maximum power based on the energy sources of AvsVehicle.
/// </example>
[HarmonyPatch(typeof(PowerRelay))]
public static class PowerRelayPatcher
{
    private static LogWriter LogOf(AvsVehicle? vehicle)
    {
        return vehicle.SafeGet(x => x.Log, LogWriter.Default).Tag(nameof(PowerRelayPatcher));
    }


    // We do not want the PowerRelay to do all the things it normally does.
    // In Start, make coroutines are invoked in repeating.
    // So we skip it.

    /// <summary>
    /// Prevents the default behavior of the PowerRelay's Start method from executing,
    /// allowing custom functionality for vehicles implementing the AvsVehicle component.
    /// </summary>
    /// <param name="__instance">The instance of the PowerRelay on which the method is invoked.</param>
    /// <returns>
    /// Returns false if the associated PowerRelay belongs to an AvsVehicle, overriding the default Start behavior.
    /// Returns true to allow the default Start method behavior for other cases.
    /// </returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(PowerRelay.Start))]
    public static bool StartPrefix(PowerRelay __instance)
    {
        var av = __instance.gameObject.SafeGetComponent<AvsVehicle>();
        if (av.IsNotNull())
        {
            LogOf(av).Debug("PowerRelay.Start");
            __instance.InvokeRepeating("UpdatePowerState", UnityEngine.Random.value, 0.5f);
            return false;
        }
        else
        {
            LogOf(null).Debug("Vehicle not recognized: " + __instance.gameObject.NiceName());
        }

        return true;
    }

    /// <summary>
    /// Intercepts the default behavior of the PowerRelay's GetPower method, allowing for custom power handling
    /// when the PowerRelay belongs to an AvsVehicle.
    /// </summary>
    /// <param name="__instance">The instance of the PowerRelay on which the method is invoked.</param>
    /// <param name="__result">The power value to be returned by the GetPower method, modified if the PowerRelay is associated with an AvsVehicle.</param>
    /// <returns>
    /// Returns false to override the default GetPower behavior when the PowerRelay is associated with an AvsVehicle,
    /// substituting custom logic for power handling. Returns true to allow the default behavior in other cases.
    /// </returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(PowerRelay.GetPower))]
    public static bool GetPowerPrefix(PowerRelay __instance, ref float __result)
    {
        var av = __instance
            .SafeGetGameObject()
            .SafeGetComponent<AvsVehicle>();
        if (av.IsNotNull())
        {
            if (av.energyInterface.IsNotNull())
            {
                __result = av.energyInterface.TotalCanProvide(out _);
                //LogOf(av).Debug("EnergyInterface.TotalCanProvide: " + __result);
            }
            else
            {
                LogOf(av).Error("EnergyInterface is null");
                __result = 0;
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// Overrides the default behavior of the PowerRelay's GetMaxPower method to calculate the total energy capacity
    /// for vehicles implementing the AvsVehicle component.
    /// </summary>
    /// <param name="__instance">The instance of the PowerRelay on which the method is invoked.</param>
    /// <param name="__result">The resulting value for the maximum power, calculated based on energy sources when applicable.</param>
    /// <returns>
    /// Returns false if the associated PowerRelay belongs to an AvsVehicle and successfully calculates the total capacity.
    /// Returns true to allow the default GetMaxPower behavior for other cases.
    /// </returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(PowerRelay.GetMaxPower))]
    public static bool GetMaxPowerPrefix(PowerRelay __instance, ref float __result)
    {
        if (__instance.IsNull() || __instance.gameObject.IsNull())
            return true;
        var av = __instance.gameObject.GetComponent<AvsVehicle>();
        if (av.IsNull()) return true;
        if (av.energyInterface.IsNull() || av.energyInterface.sources.IsNull())
        {
            LogOf(av).Error("EnergyInterface is null");
            __result = 0;
            return false;
        }

        __result = av.energyInterface.sources.Where(x => x.IsNotNull()).Select(x => x.capacity).Sum();
        //LogOf(av).Debug("EnergyInterface.sources.Sum: " + __result);
        return false;
    }
}

/// <summary>
/// The PowerSystemPatcher class provides Harmony patches to modify the behavior of the power system,
/// ensuring compatibility with the energy interface of vehicles within the AVS game context.
/// </summary>
/// <remarks>
/// This class primarily addresses issues with energy consumption and power relay systems, such as
/// allowing chargers to function correctly within AVS vehicles that have unique power setups. It
/// ensures that the power consumption mechanisms work seamlessly by interacting with the
/// energy interface in vehicles. Additionally, it includes a transpiler for updating the behavior
/// of the Charger component to handle energy consumption in an expected manner.
/// </remarks>
[HarmonyPatch(typeof(Charger))]
public static class PowerSystemPatcher
{
    /// <summary>
    /// Consumes energy only if the vehicle attached to the PowerRelay GameObject is an AvsVehicle.
    /// Otherwise, does nothing.
    /// </summary>
    /// <param name="mvpr">The PowerRelay instance invoking the method.</param>
    /// <param name="amount">The amount of energy requested for consumption.</param>
    /// <param name="amountConsumed">A reference parameter that tracks the total amount of energy consumed, updated based on vehicle-specific logic.</param>
    private static void PatchedEnergyProcess(PowerRelay mvpr, float amount, ref float amountConsumed)
    {
        // LogWriter.Default.Tag(nameof(PowerSystemPatcher))
        //     .Debug($"{nameof(PatchedEnergyProcess)}({mvpr.NiceName()}, {amount}, {amountConsumed})");
        var v = mvpr.gameObject.GetComponent<AvsVehicle>();
        if (v) amountConsumed += v.energyInterface.ConsumeEnergy(amount);
        //v.Log.Debug("AvsVehicle.energyInterface.ConsumeEnergy: " + amount + " -> " + amountConsumed);
    }

    /* This transpiler simply replaces one method call with another.
     * It calls the method above, which is generic over the replaced method.
     * It allows special handling in the case of a AvsVehicle (which does not have a good PowerRelay, see top of file)
     */
    /// <summary>
    /// Finds any call with "ConsumeEnergy" in the name, which can either the native call or the replacement
    /// emitted by VehicleFramework.
    /// Then inserts a new block just behind it that re-pushes the parameters onto the stack,
    /// then calls <see cref="PatchedEnergyProcess"/> with a ref amountConsumed instead of an out value.
    /// Unlike the VF consumption method, <see cref="PatchedEnergyProcess"/> does nothing if it does not
    /// detect an AVS vehicle. If another AVS instance comes along, both will append in arbitrary order but should
    /// produce the desired net result.
    /// </summary>
    /// <param name="instructions">The original sequence of IL instructions from the Charger.Update method.</param>
    /// <returns>
    /// Returns a modified IEnumerable of CodeInstruction objects, where specific method calls are replaced,
    /// allowing customized processing in scenarios involving the AvsVehicle energy interface.
    /// </returns>
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Charger.Update))]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var log = LogWriter.Default.Tag(nameof(PowerSystemPatcher));
        var newCodes = new List<CodeInstruction>(codes.Count);
        //
        // foreach (var code in codes)
        //     log.Debug(code.ToStr());

        var found = false;
        for (var i = 0; i < codes.Count; i++)
        {
            var code = codes[i];
            if (code.opcode == OpCodes.Call && code.operand.ToString().Contains("ConsumeEnergy") && i > 4)
            {
                newCodes.Add(code);
                newCodes.Add(codes[i - 3]);
                newCodes.Add(codes[i - 2]);
                newCodes.Add(codes[i - 1]);
                newCodes.Add(CodeInstruction.Call(typeof(PowerSystemPatcher), nameof(PatchedEnergyProcess)));
                found = true;
            }
            else
            {
                newCodes.Add(code);
            }
        }

        if (!found)
        {
            log.Error("Charger.Update: Could not find ConsumeEnergy");
            foreach (var code in codes)
                log.Warn(code.ToStr());
        }
        else
        {
            log.Write("Charger.Update: Added instruction");
        }

        //
        // foreach (var code in newCodes)
        //     LogWriter.Default.Tag(nameof(PowerSystemPatcher)).Tag($"Emitting").Debug(code.ToStr());
        return newCodes.AsEnumerable();
    }
}