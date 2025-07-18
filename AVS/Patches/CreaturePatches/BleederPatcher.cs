using AVS.BaseVehicle;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

// PURPOSE: ensure bleeders deal damage in an intuitive way
// VALUE: high

namespace AVS.Patches.CreaturePatches
{
    [HarmonyPatch(typeof(AttachAndSuck))]
    class BleederPatcher
    {
        /* This patch is intended to ensure bleeder's take the player's life and not the vehicle's life.
         * It works by adding a check for whether the player is in an AvsVehicle before it attaches
         * Without this, the bleeder will collide with the AvsVehicle, find that it has a player, but that the player isn't piloting or in a cyclops (so he's vulnerable)
         * Then it will attach to the player but deal damage to the AvsVehicle
         */
        public static bool IsPlayerInsideAvsVehicle()
        {
            return (Player.main.GetVehicle() is AvsVehicle);
        }
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
