using AVS.BaseVehicle;
using AVS.Util;
using AVS.VehicleTypes;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// PURPOSE: ensure the Player behaves as expected when AvsVehicle are involved
// VALUE: Very high.

namespace AVS.Patches;

/// <summary>
/// The PlayerPatcher class contains a set of Harmony patches that modify the behavior of the Player class
/// in specific scenarios, particularly when interacting with an AVS vehicle or other specialized game states.
/// This class introduces custom behavior for underwater activities, pilot seat exits, and other gameplay elements.
/// It leverages the Harmony library to override or extend the default methods of the Player class.
/// </summary>
[HarmonyPatch(typeof(Player))]
public static class PlayerPatcher
{
    /*
     * This collection of patches covers many topics.
     * Generally, it regards behavior in a AvsVehicle while underwater and exiting the pilot seat.
     * TODO: there is likely some redundancy here with PlayerControllerPatcher
     */
    /// <summary>
    /// Executes additional setup logic after the player object's Awake method is called. This includes initializing the game's console commands,
    /// marking the player as awakened, loading scanner data entries, deciding on HUD elements, and setting up build bot paths.
    /// </summary>
    /// <param name="__instance">The instance of the Player class being initialized.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Player.Awake))]
    public static void AwakePostfix(Player __instance)
    {
        new GameObject().AddComponent<Admin.ConsoleCommands>();
        Admin.GameStateWatcher.IsPlayerAwakened = true;
        Assets.FragmentManager.AddScannerDataEntries();
        HUDBuilder.DecideBuildHUD();

        // Setup build bot paths.
        // We have to do this at game-start time,
        // because the new objects we create are wiped on scene-change.
        RootModController.AnyInstance.StartAvsCoroutine(
            nameof(BuildBotManager) + '.' + nameof(BuildBotManager.SetupBuildBotPathsForAllMVs),
            _ => BuildBotManager.SetupBuildBotPathsForAllMVs());
        return;
    }

    /// <summary>
    /// Performs additional logic after the player's Start method has been executed. This includes updating the game state
    /// to indicate that the player has started.
    /// </summary>
    /// <param name="__instance">The instance of the Player class for which the Start method is being patched.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Player.Start))]
    public static void StartPostfix(Player __instance)
    {
        Admin.GameStateWatcher.IsPlayerStarted = true;
        return;
    }

    /// <summary>
    /// Adjusts the behavior of the Player.TryEject method to ensure compatibility with the AvsVehicle system
    /// and integration with the DeathRun functionality. Prevents the default ejection process if the player is using an AvsVehicle.
    /// </summary>
    /// <param name="__instance">The Player instance on which the TryEject method is invoked.</param>
    /// <returns>Returns true if no AvsVehicle is associated with the player, allowing the default ejection logic to execute; otherwise, returns false.</returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Player.TryEject))]
    public static bool PlayerTryEjectPrefix(Player __instance) =>
        // Player.TryEject does not serve AvsVehicle.
        // The only reason it gets called at all, for a AvsVehicle,
        // is for compatibility with DeathRun remade,
        // which spends energy on Player.TryEject.
        // So we'll gut it and call it at the appropriate time,
        // so that the DeathRun functionality can exist.
        __instance.GetAvsVehicle().IsNull();

    /// <summary>
    /// Overrides the depth classification logic for the player. This determines the current depth class,
    /// which affects crush depth values and other related gameplay mechanics, based on whether the player
    /// is controlling a vehicle or otherwise.
    /// </summary>
    /// <param name="__instance">The instance of the Player class for which the depth class is being evaluated.</param>
    /// <param name="__result">The resulting Ocean.DepthClass value that represents the current depth classification for the player.</param>
    /// <returns>
    /// Returns true to proceed with the original method logic; false if the depth class is overridden
    /// and processed within this method.
    /// </returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Player.GetDepthClass))]
    public static bool GetDepthClass(Player __instance, ref Ocean.DepthClass __result)
    {
        /*
         * TODO
         * I believe this function relates to the PDA voice communicating depth information to you.
         * "Passing 400 meters," that sort of thing.
         * I'm not sure this patch is strictly necessary.
         */
        var av = __instance.GetVehicle() as Submarine;
        if (av.IsNotNull() && !av.IsPlayerControlling())
        {
            //var crushDamage = __instance.gameObject.GetComponentInParent<CrushDamage>();
            //__result = crushDamage.GetDepthClass();
            //__instance.crushDepth = crushDamage.crushDepth;
            __result = Ocean.DepthClass.Safe;
            __instance.crushDepth = 200f;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Adjusts the player's animator states based on their interaction with a submarine. Ensures the correct movement states
    /// are set when the player is inside a submarine but not controlling it, preventing animations such as swimming or diving.
    /// </summary>
    /// <param name="__instance">The instance of the Player class being updated.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Player.Update))]
    public static void UpdatePostfix(Player __instance)
    {
        var av = __instance.GetVehicle() as Submarine;
        if (av.IsNull())
            return;
        if (av.IsPlayerInside() && !av.IsPlayerControlling())
        {
            // animator stuff to ensure we don't act like we're swimming at any point
            __instance.playerAnimator.SetBool("is_underwater", false);
            __instance.playerAnimator.SetBool("on_surface", false);
            __instance.playerAnimator.SetBool("diving", false);
            __instance.playerAnimator.SetBool("diving_land", false);
        }
    }


    /// <summary>
    /// Intercepts the Player.UpdateIsUnderwater method to determine if the player is underwater.
    /// If the player is inside a submarine, they are considered not underwater, ensuring appropriate state updates.
    /// </summary>
    /// <param name="__instance">The Player instance being checked for underwater status.</param>
    /// <returns>
    /// Returns `false` to bypass the original UpdateIsUnderwater method logic
    /// when the player is inside a submarine, or `true` to allow normal execution.
    /// </returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Player.UpdateIsUnderwater))]
    public static bool UpdateIsUnderwaterPrefix(Player __instance)
    {
        var av = __instance.GetVehicle() as Submarine;
        if (av.IsNotNull())
        {
            // declare we aren't underwater,
            // since we're wholly within an air bubble
            __instance.isUnderwater.Update(false);
            __instance.isUnderwaterForSwimming.Update(false);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Adjusts the motor mode of the player before the UpdateMotorMode method is executed. This ensures that the player is in walking mode
    /// if they are in a vehicle derived from the Submarine class but are not actively piloting it.
    /// </summary>
    /// <param name="__instance">The instance of the Player class undergoing motor mode updates.</param>
    /// <returns>
    /// A boolean value indicating whether the original method should execute. Returns false to prevent execution if the motor mode
    /// is explicitly set to walking during the prefix.
    /// </returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Player.UpdateMotorMode))]
    public static bool UpdateMotorModePrefix(Player __instance)
    {
        var av = __instance.GetVehicle() as Submarine;
        if (av.IsNotNull() && !av.IsPlayerControlling())
        {
            // ensure: if we're in a AvsVehicle and we're not piloting, then we're walking.
            __instance.SetMotorMode(Player.MotorMode.Walk);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Adjusts the player's ability to breathe based on whether they are currently mounted
    /// inside a powered and boarded AVS submarine.
    /// </summary>
    /// <param name="__instance">The instance of the Player class being evaluated for breathing capability.</param>
    /// <param name="__result">A reference to the original result indicating whether the player can breathe.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Player.CanBreathe))]
    public static void CanBreathePostfix(Player __instance, ref bool __result)
    {
        if (__instance.currentMountedVehicle.IsNotNull())
        {
            var av = __instance.currentMountedVehicle as AvsVehicle;
            switch (av)
            {
                case Submarine _:
                    __result = av.IsPowered() && av.IsBoarded;
                    return;
                default:
                    return;
            }
        }
    }

    /// <summary>
    /// Intercepts and evaluates the player's position update logic before it executes. This ensures
    /// consistency when the player is piloting a vehicle, handles anomalies in vehicle parenting,
    /// and skips updates during freecam or ghost mode.
    /// </summary>
    /// <param name="__instance">The instance of the Player class updating its position.</param>
    /// <returns>
    /// Returns true to allow the original UpdatePosition logic to execute, or false to skip execution
    /// if abnormalities are detected.
    /// </returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Player.UpdatePosition))]
    public static bool UpdatePositionPrefix(Player __instance)
    {
        // don't do this if there is a parent and mounted vehicle mismatch
        // This is a weird thing. How do we handle it?

        var fcc = MainCameraControl.main.GetComponent<FreecamController>();
        if (fcc.mode || fcc.ghostMode)
            return true;
        var checkedAncestry = new List<Transform>();
        if (__instance.currentMountedVehicle is AvsVehicle av
            && __instance.mode == Player.Mode.LockedPiloting
            && !Admin.Utils.FindVehicleInParents(Player.main.transform, out var v, checkedAncestry))
        {
            using var log = av.NewAvsLog();
            log.Error(
                $"Player does not reside in a vehicle or the wrong one ({v.NiceName()}). Checked ancestry: {string.Join("->", checkedAncestry.Select(x => x.NiceName()))}");
        }
        // Don't skip. This is a weird problem and it needs resolved, so let it die strangely.
        //return false;
        return true;
    }

    /// <summary>
    /// Intercepts the logic for the Player's ExitLockedMode method to determine if the default behavior
    /// should be executed or replaced with customized behavior for exiting vehicles in the AVS system.
    /// </summary>
    /// <param name="__instance">The instance of the Player class attempting to exit locked mode.</param>
    /// <returns>
    /// Returns false if the player is in an AVS vehicle and the customized behavior for exiting a vehicle
    /// is applied. Returns true to allow the default ExitLockedMode logic to proceed.
    /// </returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Player.ExitLockedMode))]
    public static bool PlayerExitLockedModePrefix(Player __instance)
    {
        // if we're in an MV, do our special way of exiting a vehicle instead
        var av = __instance.GetAvsVehicle();
        if (av.IsNull())
            return true;
        av.DeselectSlots();
        return false;
    }

    /// <summary>
    /// Executes logic following the player's death. If the player is inside an AVS vehicle,
    /// performs specific operations to ensure the player is safely exited from the vehicle.
    /// </summary>
    /// <param name="__instance">The instance of the Player class that has been killed.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Player.OnKill))]
    public static void PlayerOnKillPostfix(Player __instance)
    {
        // if we're in an MV, do our special way of exiting a vehicle instead
        var av = __instance.GetAvsVehicle();
        if (av.IsNull())
            return;
        using var log = av.NewAvsLog();
        log.Write("PlayerOnKillPostfix: Player has died, exiting vehicle.");
        av.ExitHelmControl();
        av.ClosestPlayerExit(false);
    }
}