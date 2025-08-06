using AVS.Log;
using AVS.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

// PURPOSE: Allow ModVehicles to use (and have displayed) custom ping sprites.
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

            foreach (var instruction in instructions)
                LogWriter.Default.Debug($"[uGUI_PingsPatcher] Instruction: '{instruction}' op '{instruction.operand}' code '{instruction.opcode}'");
            var loaduGUI_Ping2 = instructions.FirstOrDefault(i => i.opcode == OpCodes.Ldloc_0);
            var loadInstance = instructions.FirstOrDefault(i => i.opcode == OpCodes.Ldarg_1);
            var loadPingType = instructions.FirstOrDefault(i => i.opcode == OpCodes.Ldfld && i.operand.ToString() == "PingType pingType");
            if (loadInstance == null || loadPingType == null)
            {
                LogWriter.Default.Error("[uGUI_PingsPatcher] Failed to find required instructions for patching.");
                return instructions;
            }
            LogWriter.Default.Debug($"[uGUI_PingsPatcher] loadInstance: '{loadInstance}' op '{loadInstance.operand}' code '{loadInstance.opcode}'");
            LogWriter.Default.Debug($"[uGUI_PingsPatcher] loadPingType: '{loadPingType}' op '{loadPingType.operand}' code '{loadPingType.opcode}'");

            var instList = instructions.ToList();

            int index = instList.FindIndexOf(i => i.opcode == OpCodes.Callvirt && i.operand.ToString().Contains("Void SetIcon(Sprite)"));

            if (index < 0)
            {
                LogWriter.Default.Error("[uGUI_PingsPatcher] Failed to find SetIcon method in uGUI_Pings.OnAdd.");
                return instructions;
            }
            instList.Insert(index + 1, loaduGUI_Ping2);
            instList.Insert(index + 2, loadInstance);
            instList.Insert(index + 3, loadPingType);
            instList.Insert(index + 4, Transpilers.EmitDelegate<Action<uGUI_Ping, PingType>>(AvsVehicleBuilder.SetIcon));

            //CodeMatch SetIconMatch = new CodeMatch(i => i.opcode == OpCodes.Callvirt && i.operand.ToString().Contains("Void SetIcon(Sprite)"));

            //var match = new CodeMatcher(instructions)
            //    .MatchStartForward(SetIconMatch);

            //var newInstructions = new CodeMatcher(instructions)
            //    .MatchStartForward(SetIconMatch)
            //    .Repeat(x =>
            //        x.Advance(1) //skip over
            //        .InsertAndAdvance(loaduGUI_Ping2)
            //        .InsertAndAdvance(loadInstance)
            //        .InsertAndAdvance(loadPingType)
            //        .InsertAndAdvance(Transpilers
            //            .EmitDelegate<Action<uGUI_Ping, PingType>>
            //            (AvsVehicleBuilder.SetIcon))
            //    );


            //var loadThis = newInstructions.EnumerateMatches()
            //    .FirstOrDefault(m => m.IsMatch && m.MatchCount == 1 && m.InstructionList.Count > 0);

            //CodeMatch VFGetPingTypeMatch = new CodeMatch(i => i.opcode == OpCodes.Call && i.operand.ToString().Contains("System.String GetPingTypeString(CachedEnumString`1[PingType], PingType)"));
            //var newInstructions2 = new CodeMatcher(newInstructions.InstructionEnumeration())
            //    .MatchStartForward(VFGetPingTypeMatch)
            //    .Repeat(x =>
            //        x.RemoveInstruction()
            //        .InsertAndAdvance(Transpilers.EmitDelegate<Func<CachedEnumString<PingType>, PingType, string>>(AvsVehicleBuilder.GetPingTypeString))
            //        .RemoveInstruction()
            //        .Insert(Transpilers.EmitDelegate<Func<SpriteManager.Group, string, Atlas.Sprite?>>(AvsVehicleBuilder.GetPingTypeSprite))
            //        );
            foreach (var instruction in instList)
                LogWriter.Default.Debug($"[uGUI_PingsPatcher] Emitting: '{instruction}' op '{instruction.operand}' code '{instruction.opcode}'");


            return instList;
        }
    }
}
