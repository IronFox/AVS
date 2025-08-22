using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using AVS.Log;
using AVS.Util;
using HarmonyLib;

// PURPOSE: Allow AvsVehicles to use (and have displayed) custom ping sprites.
// VALUE: Very high.

namespace AVS.Patches;

/// <summary>
/// A class designed to patch the behavior of the uGUI_Pings class through Harmony transpilers.
/// It enables the customization and display of custom ping sprites for AvsVehicles within the game.
/// </summary>
[HarmonyPatch(typeof(uGUI_Pings))]
internal class uGUI_PingsPatcher
{
    /*
     * This transpiler ensure our ping sprites are used properly by the base-game systems,
     * so that we may display our custom ping sprites on the HUD
     */
    /// <summary>
    /// Transpiler method for patching the uGUI_Pings.OnAdd method to ensure custom ping sprites
    /// are used properly by the base-game systems, allowing for display of custom ping sprites on the HUD.
    /// </summary>
    /// <remarks>
    /// Looks for the SetIcon() method invocation in nGUI_Pings.OnAdd.
    /// Then adds a new invocation just after this call, that conditionally calls SetIcon() a second time if
    /// the PingType is of a registered AVS vehicle, otherwise does nothing. The architecture is designed as a
    /// daisy-chain approach where every other lib or AVS incarnation can do exactly the same, regardless of
    /// load-order and the final behavior will always produce the desired outcome. 
    /// </remarks>
    /// <param name="input">A collection of code instructions representing the original IL code of the method being patched.</param>
    /// <returns>
    /// A modified collection of code instructions with the required IL changes to implement the custom patch logic.
    /// </returns>
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(uGUI_Pings.OnAdd))]
    private static IEnumerable<CodeInstruction> uGUI_PingsOnAddTranspiler(IEnumerable<CodeInstruction> input)
    {
        var log = LogWriter.Default.Tag(nameof(uGUI_PingsPatcher));
        var instructions = input.ToList();
        //foreach (var instruction in instructions)
        //    log.Debug($"Instruction: '{instruction}' op '{instruction.operand}' code '{instruction.opcode}'");
        var loadGUI_Ping2 = instructions.FirstOrDefault(i => i.opcode == OpCodes.Ldloc_0);
        var loadInstance = instructions.FirstOrDefault(i => i.opcode == OpCodes.Ldarg_1);
        var loadPingType =
            instructions.FirstOrDefault(i => i.opcode == OpCodes.Ldfld && i.operand.ToString() == "PingType pingType");
        if (loadInstance.IsNull() || loadPingType.IsNull())
        {
            log.Error("Failed to find required instructions for patching.");
            return instructions;
        }

        log.Debug($"loadInstance: {loadInstance.ToStr()}");
        log.Debug($"loadPingType: {loadPingType.ToStr()}");

        var instList = instructions.ToList();

        var index = instList.FindIndexOf(i =>
            i.opcode == OpCodes.Callvirt && i.operand.ToString().Contains("Void SetIcon(Sprite)"));
        if (index < 0)
            index = instList.FindIndexOf(i =>
                i.opcode == OpCodes.Callvirt && i.operand.ToString().Contains("Void SetIcon(UnityEngine.Sprite)"));
        if (index < 0)
        {
            log.Error("Failed to find SetIcon method in uGUI_Pings.OnAdd.");
            return instructions;
        }

        instList.Insert(index + 1, loadGUI_Ping2);
        instList.Insert(index + 2, loadInstance);
        instList.Insert(index + 3, loadPingType);
        instList.Insert(index + 4, Transpilers.EmitDelegate<Action<uGUI_Ping, PingType>>(AvsVehicleBuilder.SetIcon));

        //foreach (var instruction in instList)
        //    LogWriter.Default.Debug($"[uGUI_PingsPatcher] Emitting: '{instruction}' op '{instruction.operand}' code '{instruction.opcode}'");
        log.Debug($"uGUI_Pings.OnAdd successfully patched");

        return instList;
    }
}