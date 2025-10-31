using AVS.BaseVehicle;
using AVS.Log;
using AVS.StorageComponents;
using AVS.Util;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;

// PURPOSE: allow the Cyclops dock terminal to display AvsVehicle data. 
// VALUE: High.

namespace AVS.Patches;

/// <summary>
/// This class provides a patch for the CyclopsVehicleStorageTerminalManager to enhance its functionality.
/// Specifically, it modifies the behavior of the storage interface when interacting with docked vehicles.
/// </summary>
/// <remarks>
/// The patch allows the Cyclops Vehicle Storage Terminal to interact with AvsVehicle data,
/// enabling seamless integration and interaction with expanded vehicle types and their storage systems.
/// </remarks>
/// <example>
/// This patch executes a postfix on the StorageButtonClick method of the
/// CyclopsVehicleStorageTerminalManager, enabling custom logic for storage access.
/// </example>
[HarmonyPatch(typeof(CyclopsVehicleStorageTerminalManager))]
public static class CyclopsPatcher
{
    /// <summary>
    /// Transforms the IL code within the target method of the CyclopsVehicleStorageTerminalManager class to modify behavior.
    /// This method is designed to enhance compatibility with additional vehicle types by replacing specific calls in instructions.
    /// </summary>
    /// <param name="instructions">A collection of IL code instructions to be modified by this transpiler.</param>
    /// <returns>A transformed IEnumerable of CodeInstruction objects with modifications applied for enhanced functionality.</returns>
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(CyclopsVehicleStorageTerminalManager.VehicleDocked))]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        using var log = SmartLog.LazyForAVS(RootModController.AnyInstance);

        var codes = new List<CodeInstruction>(instructions);

        int insertAt = codes.FindIndex(c =>
            c.opcode == OpCodes.Callvirt && c.operand.ToString().ToLower().Contains("energymixin") && c.operand.ToString().ToLower().Contains("getcomponent"));
        if (insertAt == -1)
        {
            log.Warn("Could not find target instruction to insert after; attempting fallback search.");
            insertAt = codes.FindIndex(c =>
                c.operand.ToString().ToLower().Contains("energymixin"));
        }
        if (insertAt >= 0)
        {
            // (re)load vehicle (this) to end of stack. 
            var c0 = new CodeInstruction(OpCodes.Ldloc_0);
            codes.Insert(insertAt + 1, c0);
            // Stack now expected: energyMixin, vehicle
            var c1 = CodeInstruction.Call(typeof(AvsVehicle), nameof(AvsVehicle.GetEnergyMixinFromVehicle));
            codes.Insert(insertAt + 2, c1);
            // Stack now expected: energyMixin
        }
        else
            log.Warn("Could not find target instruction to insert after; no changes made.");

        return codes;
    }

    /// <summary>
    /// Modifies the behavior of the VehicleDocked method in the CyclopsVehicleStorageTerminalManager class
    /// to support specific functionalities for custom vehicle types derived from AvsVehicle.
    /// </summary>
    /// <param name="__instance">The instance of CyclopsVehicleStorageTerminalManager where the method is being executed.</param>
    /// <param name="vehicle">The vehicle object that is being docked.</param>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(CyclopsVehicleStorageTerminalManager.VehicleDocked))]
    private static void VehicleDockedPrefix(CyclopsVehicleStorageTerminalManager __instance, Vehicle vehicle)
    {
        if (vehicle is AvsVehicle av)
        {
            // TODO: make custom DockedVehicleType and associated HUD
            __instance.dockedVehicleType = CyclopsVehicleStorageTerminalManager.DockedVehicleType.Seamoth;
            __instance.usingModulesUIHolder = __instance.seamothModulesUIHolder;
            __instance.currentScreen = __instance.seamothVehicleScreen;

            IEnumerator EnsureSubRootSet()
            {
                for (var i = 0; i < 100; i++)
                {
                    yield return null;
                    Player.main.SetCurrentSub(__instance.dockingBay.GetSubRoot(), false);
                }
            }

            av.Owner.StartAvsCoroutine(
                nameof(CyclopsPatcher) + '.' + nameof(EnsureSubRootSet),
                _ => EnsureSubRootSet());
        }
    }

    /// <summary>
    /// Executes after a vehicle is docked in the Cyclops's vehicle storage terminal and modifies its behavior for compatibility with custom vehicle types.
    /// Adjusts the instance to accommodate the docked vehicle, ensuring proper utility and interface functionality.
    /// </summary>
    /// <param name="__instance">The instance of the CyclopsVehicleStorageTerminalManager handling the vehicle docking process.</param>
    /// <param name="vehicle">The vehicle being docked in the Cyclops that may require customized behavior adjustments.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CyclopsVehicleStorageTerminalManager.VehicleDocked))]
    private static void VehicleDockedPostfix(CyclopsVehicleStorageTerminalManager __instance, Vehicle vehicle)
    {
        if (vehicle is AvsVehicle av)
        {
            __instance.dockedVehicleType = CyclopsVehicleStorageTerminalManager.DockedVehicleType.Seamoth;
            __instance.usingModulesUIHolder = __instance.seamothModulesUIHolder;
            __instance.currentScreen = __instance.seamothVehicleScreen;
            __instance.vehicleUpgradeConsole = av.upgradesInput;
            if (__instance.vehicleUpgradeConsole && __instance.vehicleUpgradeConsole.equipment.IsNotNull())
            {
                //__instance.vehicleUpgradeConsole.equipment.onEquip += __instance.OnEquip;
                //__instance.vehicleUpgradeConsole.equipment.onUnequip += __instance.OnUneqip;
            }
        }
    }

    /// <summary>
    /// Modifies the behavior of the Cyclops Vehicle Storage Terminal's StorageButtonClick method to support specific functionality for docked vehicles.
    /// This postfix ensures proper handling of storage interactions for Seamoth vehicles docked in the Cyclops.
    /// </summary>
    /// <param name="__instance">The instance of the CyclopsVehicleStorageTerminalManager currently being patched.</param>
    /// <param name="type">The type of vehicle storage being accessed.</param>
    /// <param name="slotID">The slot ID of the storage container being interacted with.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CyclopsVehicleStorageTerminalManager.StorageButtonClick))]
    public static void StorageButtonClickPostfix(CyclopsVehicleStorageTerminalManager __instance,
        CyclopsVehicleStorageTerminalManager.VehicleStorageType type, int slotID)
    {
        if (__instance.dockedVehicleType == CyclopsVehicleStorageTerminalManager.DockedVehicleType.Seamoth)
        {
            foreach (var seamothStorageInput in __instance.currentVehicle.GetAllComponentsInChildren<StorageInput>())
                if (seamothStorageInput.slotID == slotID)
                {
                    seamothStorageInput.OpenFromExternal();
                    return;
                }

            return;
        }
    }
}