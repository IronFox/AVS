// PURPOSE: Allow AvsVehicles to be built basically anywhere (don't "need deeper water" unnecessarily)
// VALUE: High.
// doesn't work atm.

using AVS.Log;
using AVS.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace AVS.Patches
{
    /// <summary>
    /// The <c>ConstructorInputPatcher</c> class provides a Harmony patch for modifying
    /// the crafting behavior in the <c>ConstructorInput</c> class, enabling more flexible
    /// and customized crafting mechanics within the application.
    /// </summary>
    /// <remarks>
    /// This class modifies the behavior of the <c>ConstructorInput.Craft</c> method using
    /// a Harmony transpiler to allow for crafting in additional locations by overriding
    /// the default crafting position validation logic.
    /// </remarks>
    [HarmonyPatch(typeof(ConstructorInput))]
    public class ConstructorInputPatcher
    {
        /// <summary>
        /// A Harmony transpiler method that modifies instructions within the
        /// <c>ConstructorInput.Craft</c> method to customize crafting position validation logic.
        /// </summary>
        /// <param name="instructions">The original sequence of IL code instructions from the <c>ConstructorInput.Craft</c> method.</param>
        /// <returns>The modified sequence of IL code instructions with custom crafting position validation logic.</returns>
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(ConstructorInput.Craft))]
        public static IEnumerable<CodeInstruction> ConstructorInputCraftranspiler(IEnumerable<CodeInstruction> instructions)
        {
            using var log = SmartLog.ForAVS(RootModController.AnyInstance);
            var list = instructions.ToList();
            // foreach (var i in list)
            //     log.Write($"Instruction: opCode='{i.opcode}', operand={i.operand.ObjectToStr()}, labels:{string.Join(", ",i.labels.Select(x => x.ToStr()))}");
            //

            var consumeResourceInstruction = list.FirstOrDefault(i => i.opcode == OpCodes.Call && i.operand.ToString().Contains("ConsumeResource"));
            var consumeResourceInstructionIndex = list.IndexOf(consumeResourceInstruction);
            var labelAt = list.FindLastIndex(consumeResourceInstructionIndex, i => i.labels.Count > 0);
            if (labelAt == -1)
            {
                log.Error("Could not find label for ConsumeResource");
                return list;
            }
            var label = list[labelAt].labels[0];
            log.Write($"Using {label.ToStr()}");

            CodeInstruction[] insertInstructions =
            [
                //new (OpCodes.Ldarg_0),  //this
                //new (OpCodes.Ldloc_0),  //pollPosition
                new (OpCodes.Ldarg_1),  //techType
                Transpilers.EmitDelegate<Func<TechType, bool>>(IsAvsVehicle),
                new (OpCodes.Brtrue, label),
            ];

            var insertAt = list.FindIndex(x => x.opcode == OpCodes.Stloc_1) + 1;
            if (insertAt < 0 || insertAt >= labelAt)
            {
                log.Error("Could not find insert point for custom crafting position validation logic.");
                return list;
            }
            log.Write($"Inserting custom crafting position validation logic at {insertAt}");
            list.InsertRange(insertAt, insertInstructions);

            // foreach (var i in list)
            //     log.Write($"Emitting: opCode='{i.opcode}', operand={i.operand.ObjectToStr()}, labels:{string.Join(", ",i.labels.Select(x => x.ToStr()))}");


            return list;
            //
            // CodeMatch ReturnValidCraftingPositionMatch = new CodeMatch(i => i.opcode == OpCodes.Call && i.operand.ToString().Contains("ReturnValidCraftingPosition"));
            //
            // var newInstructions = new CodeMatcher(instructions)
            //     .MatchStartForward(ReturnValidCraftingPositionMatch)
            //     .RemoveInstruction()
            //     .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_1))
            //     .Insert(Transpilers.EmitDelegate<Func<ConstructorInput, Vector3, TechType, bool>>(ReturnValidCraftingPositionSpecial));
            //
            // return newInstructions.InstructionEnumeration();
        }

        private static bool IsAvsVehicle(TechType craftTechType)
        {
            if (AvsVehicleManager.VehicleTypes.Any(x => x.TechType == craftTechType))
                return true;
            return false;   //move along
        }
    }
}
