﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

// PURPOSE: allow intuitive use of drone stations while seated
// VALUE: Moderate. Some value for immersion. See GUIHandPatcher too.

namespace AVS.Patches
{
    // The transpilers here replace all calls to Bench.ExitSittingMode with Bench.MaybeExitSittingMode,
    // which always calls Bench.ExitSittingMode if a drone isn't being piloted,
    // and still calls it if the player died or just became underwater.
    [HarmonyPatch(typeof(Bench))]
    public class BenchPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Bench.EnterSittingMode))]
        public static void BenchEnterSittingModePostfix()
        {
            var mv = Player.main.GetAvsVehicle();
            if (mv != null)
            {
                mv.controlSheme = Vehicle.ControlSheme.Mech;
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Bench.ExitSittingMode))]
        public static void BenchExitSittingModePostfix(Bench __instance, Player player, bool skipCinematics)
        {
            var mv = Player.main.GetAvsVehicle();
            if (mv != null)
            {
                mv.controlSheme = (Vehicle.ControlSheme)12;
            }
        }

        public static IEnumerable<CodeInstruction> ReplaceExitSittingMode(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatch ExitSittingModeMatch = new CodeMatch(i => i.opcode == OpCodes.Call && i.operand.ToString().Contains("ExitSittingMode"));

            var newInstructions = new CodeMatcher(instructions)
                .MatchStartForward(ExitSittingModeMatch)
                .RemoveInstruction()
                .Insert(Transpilers.EmitDelegate<Action<Bench, Player, bool>>(MaybeExitSittingMode));

            return newInstructions.InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Bench.OnUpdate))]
        public static IEnumerable<CodeInstruction> OnUpdateTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceExitSittingMode(instructions);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Bench.OnPlayerDeath))]
        public static IEnumerable<CodeInstruction> OnPlayerDeathTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceExitSittingMode(instructions);
        }

        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Bench.CheckIfUnderwater))]
        public static IEnumerable<CodeInstruction> CheckIfUnderwaterTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceExitSittingMode(instructions);
        }

        public static void MaybeExitSittingMode(Bench thisBench, Player player, bool skipCinematics)
        {
            // If the player is piloting a drone,
            // consume the "exit" input to leave the drone,
            // but do not, at that time, exit the bench.
            // If instead the player died or is underwater,
            // leave the drone AND exit the bench.
            if (skipCinematics || player.isUnderwater.value) // these are true for OnDeath and OnUnderwater, respectively
            {
                // Exit the bench normally
                thisBench.ExitSittingMode(player, skipCinematics);
            }
        }

    }
}
