using AVS.BaseVehicle;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

// PURPOSE: ensure bleeders deal damage in an intuitive way
// VALUE: high


namespace AVS.Patches.CreaturePatches
{
    /// <summary>
    /// The BleederPatcher class provides functionality to modify the behavior of the "AttachAndSuck" component
    /// ensuring that damage caused by bleeder creatures reflects an intuitive approach. Specifically, this ensures
    /// bleeder creatures deal damage to the appropriate entity instead of inadvertently damaging vehicles.
    /// </summary>
    /// <remarks>
    /// This patch incorporates Harmony transpilers to overwrite specific behaviors of the "AttachAndSuck" class.
    /// It checks whether the player is piloting an AvsVehicle and ensures bleeders interact correctly without
    /// causing unintended collisions between the player and the vehicle's mechanics.
    /// </remarks>
    /// <example>
    /// The BleederPatcher modifies the OnCollisionEnter method within the "AttachAndSuck" component.
    /// </example>
    [HarmonyPatch(typeof(AttachAndSuck))]
    class BleederPatcher
    {
        /// <summary>
        /// Checks if the player is currently inside an AvsVehicle.
        /// </summary>
        /// <returns>True if the player is inside an AvsVehicle, otherwise false.</returns>
        public static bool IsPlayerInsideAvsVehicle()
        {
            return (Player.main.GetVehicle() is AvsVehicle);
        }

        /// <summary>
        /// Modifies the IL code of the OnCollisionEnter method in the AttachAndSuck class to adjust behavior when interacting with vehicles.
        /// </summary>
        /// <param name="instructions">The original set of IL instructions to be modified.</param>
        /// <returns>An enumerable collection of IL instructions with the applied modifications.</returns>
        [HarmonyPatch(nameof(AttachAndSuck.OnCollisionEnter))]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            List<CodeInstruction> newCodes = new List<CodeInstruction>(codes.Count + 2);
            CodeInstruction myNOP = new CodeInstruction(OpCodes.Nop);
            for (int i = 0; i < codes.Count; i++)
            {
                newCodes.Add(myNOP);
            }
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt)
                {
                    if (codes[i].operand.ToString().Contains("IsInSub"))
                    {
                        newCodes[i] = codes[i];
                        newCodes[i + 1] = codes[i + 1];
                        newCodes[i + 2] = CodeInstruction.Call(typeof(BleederPatcher), nameof(IsPlayerInsideAvsVehicle));
                        newCodes[i + 3] = new CodeInstruction(codes[i + 1]);
                        i += 4;
                        continue;
                    }
                }
                newCodes[i] = codes[i];
            }
            return newCodes.AsEnumerable();
        }
    }
}
