using AVS.Log;
using AVS.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

// PURPOSE: Allow AvsVehicles to use (and have displayed) custom ping sprites.
// VALUE: Very high.

namespace AVS
{
    [HarmonyPatch(typeof(uGUI_Pings))]
    class uGUI_PingsPatcher
    {
        /*
         * This transpiler ensure our ping sprites are used properly by the base-game systems,
         * so that we may display our custom ping sprites on the HUD
         */
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(uGUI_Pings.OnAdd))]
        static IEnumerable<CodeInstruction> uGUI_PingsOnAddTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var log = LogWriter.Default.Tag(nameof(uGUI_PingsPatcher));

            //foreach (var instruction in instructions)
            //    log.Debug($"Instruction: '{instruction}' op '{instruction.operand}' code '{instruction.opcode}'");
            var loaduGUI_Ping2 = instructions.FirstOrDefault(i => i.opcode == OpCodes.Ldloc_0);
            var loadInstance = instructions.FirstOrDefault(i => i.opcode == OpCodes.Ldarg_1);
            var loadPingType = instructions.FirstOrDefault(i => i.opcode == OpCodes.Ldfld && i.operand.ToString() == "PingType pingType");
            if (loadInstance == null || loadPingType == null)
            {
                log.Error("Failed to find required instructions for patching.");
                return instructions;
            }
            log.Debug($"loadInstance: '{loadInstance}' op '{loadInstance.operand}' code '{loadInstance.opcode}'");
            log.Debug($"loadPingType: '{loadPingType}' op '{loadPingType.operand}' code '{loadPingType.opcode}'");

            var instList = instructions.ToList();

            int index = instList.FindIndexOf(i => i.opcode == OpCodes.Callvirt && i.operand.ToString().Contains("Void SetIcon(Sprite)"));
            if (index < 0)
                index = instList.FindIndexOf(i => i.opcode == OpCodes.Callvirt && i.operand.ToString().Contains("Void SetIcon(UnityEngine.Sprite)"));
            if (index < 0)
            {
                log.Error("Failed to find SetIcon method in uGUI_Pings.OnAdd.");
                return instructions;
            }
            instList.Insert(index + 1, loaduGUI_Ping2);
            instList.Insert(index + 2, loadInstance);
            instList.Insert(index + 3, loadPingType);
            instList.Insert(index + 4, Transpilers.EmitDelegate<Action<uGUI_Ping, PingType>>(AvsVehicleBuilder.SetIcon));

            //foreach (var instruction in instList)
            //    LogWriter.Default.Debug($"[uGUI_PingsPatcher] Emitting: '{instruction}' op '{instruction.operand}' code '{instruction.opcode}'");
            log.Debug($"uGUI_Pings.OnAdd successfully patched");

            return instList;
        }
    }
}
