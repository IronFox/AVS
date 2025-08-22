using AVS.Util;
using HarmonyLib;
using UnityEngine;

// PURPOSE: protect player's in Submarines against Warper teleport balls.
// VALUE: High.

namespace AVS.Patches.CreaturePatches;

/// <summary>
/// The WarperPatcher class provides patches to modify the behavior of creatures in the game, specifically addressing interactions with "Warper" creatures and their "Warp Ball" attacks.
/// It is designed to protect players who are located inside submarines from being targeted or affected by Warper attacks.
/// </summary>
/// <remarks>
/// This class utilizes Harmony patches to alter the behavior of "Warper" creatures, redirecting certain attack actions to ensure player safety inside submarines.
/// Additionally, it prevents warp balls from impacting players who are inside submarines.
/// </remarks>
[HarmonyPatch(typeof(Creature))]
internal class WarperPatcher
{
    /// <summary>
    /// Postfix method for the <c>ChooseBestAction</c> method in the <c>Creature</c> class.
    /// Modifies the behavior of the Warper creature to prevent it from selecting ranged attack actions
    /// (specifically "RangedAttackLastTarget") if the player is inside a submarine.
    /// </summary>
    /// <param name="__instance">The instance of the <c>Creature</c> being patched.</param>
    /// <param name="__result">The action chosen by the <c>Creature</c> before modification. Can be modified by the postfix.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Creature.ChooseBestAction))]
    public static void ChooseBestActionPostfix(Creature __instance, ref CreatureAction __result)
    {
        if (__result.IsNull())
            return;
        if (__instance.name.Contains("Warper") && __result.GetType().ToString().Contains("RangedAttackLastTarget"))
            if (Player.main.IsNotNull())
            {
                var sub = Player.main.GetVehicle() as VehicleTypes.Submarine;
                if (sub.IsNotNull())
                    __result = new SwimRandom();
            }
    }
}

/// <summary>
/// The WarperPatcher2 class provides patches to modify the behavior of warp balls in the game, specifically preventing their effects from impacting players who are located inside submarines.
/// </summary>
/// <remarks>
/// This class employs Harmony patches to intercept the behavior of warp balls. By applying modifications, it ensures that warp balls no longer target or impact players who are either inside a submarine or safely within its vicinity. This functionality enhances player safety while using submarines in potentially dangerous situations involving warp balls.
/// </remarks>
[HarmonyPatch(typeof(WarpBall))]
internal class WarperPatcher2
{
    /// <summary>
    /// Prefix method for the <c>Warp</c> method in the <c>WarpBall</c> class.
    /// Modifies the behavior of warp balls to prevent their effects from impacting players if they are inside a submarine.
    /// Ensures warp balls do not target or affect players who are protected by submarines.
    /// </summary>
    /// <param name="__instance">The instance of the <c>WarpBall</c> being patched.</param>
    /// <param name="target">The potential target of the warp ball's effect.</param>
    /// <param name="position">The position in the game world where the warp effect is intended to occur. Can be modified by the prefix.</param>
    /// <returns>
    /// Returns <c>true</c> to allow the original <c>Warp</c> method to execute when players are not in a submarine,
    /// and <c>false</c> to skip the original method when a submarine is detected, effectively preventing the warp effect.
    /// </returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(WarpBall.Warp))]
    public static bool WarpBallWarpPrefix(WarpBall __instance, GameObject target, ref Vector3 position)
    {
        // Warp balls shouldn't effect players in Submarines

        var myPlayer = target.GetComponent<Player>();
        var mySub = target.GetComponent<VehicleTypes.Submarine>()
            .Or(() => myPlayer.SafeGetVehicle<VehicleTypes.Submarine>());

        if (!mySub)
            return true;
        else
            return false;
    }
}