﻿using AVS.Util;
using HarmonyLib;
using UnityEngine;

// PURPOSE: protect player's in Submarines against Warper teleport balls.
// VALUE: High.

namespace AVS.Patches.CreaturePatches
{
    [HarmonyPatch(typeof(Creature))]
    class WarperPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Creature.ChooseBestAction))]
        public static void ChooseBestActionPostfix(Creature __instance, ref CreatureAction __result)
        {
            if (__result == null || __result.GetType() == null)
            {
                return;
            }
            if (__instance.name.Contains("Warper") && __result.GetType().ToString().Contains("RangedAttackLastTarget"))
            {
                if (Player.main != null)
                {
                    var sub = Player.main.GetVehicle() as VehicleTypes.Submarine;
                    if (sub != null)
                    {
                        __result = new SwimRandom();
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(WarpBall))]
    class WarperPatcher2
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(WarpBall.Warp))]
        public static bool WarpBallWarpPrefix(WarpBall __instance, GameObject target, ref Vector3 position)
        {
            // Warp balls shouldn't effect players in Submarines

            Player myPlayer = target.GetComponent<Player>();
            var mySub = target.GetComponent<VehicleTypes.Submarine>()
                .Or(() => myPlayer.SafeGetVehicle<VehicleTypes.Submarine>());

            if (mySub == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
