using AVS.BaseVehicle;
using AVS.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

// PURPOSE: allow AvsVehicles to use in-game docking bays
// VALUE: High.

namespace AVS.Patches;
// This set of patches modulates the moonpool docking bay behavior
// It governs:
// - accepting a AvsVehicle for docking
// - animating the docking bay arms
// - alerting the vehicle it has been docked
// See DockedVehicleHandTargetPatcher for undocking actions

/// <summary>
/// Provides Harmony patches for the <c>VehicleDockingBay</c> class, modifying its behavior
/// related to docking and undocking vehicles. These patches enhance or customize the functionality
/// of docking bays for smaller vehicles while addressing compatibility and interaction concerns.
/// The patches perform the following tasks:
/// - Determine if a vehicle is dockable based on its type and size.
/// - Animate the docking bay arms during docking operations.
/// - Manage the status and positioning of docked vehicles.
/// - Trigger alerts to notify vehicles when they have been docked.
/// - Handle trigger interactions for detecting nearby or departing vehicles.
/// - Update relevant docking bay states and perform cleanup upon destruction.
/// Note: This implementation complements <c>DockedVehicleHandTargetPatcher</c>, which handles undocking processes.
/// </summary>
[HarmonyPatch(typeof(VehicleDockingBay))]
internal class VehicleDockingBayPatch
{
    private static bool HandleMoonpool(AvsVehicle av)
    {
        //Vector3 boundingDimensions = av.CyclopsDockRotation * av.GetBoundingDimensions();
        var boundingDimensions = av.GetDockingBoundsSize();
        if (boundingDimensions == Vector3.zero)
            return false;
        var mpx = 8.6f;
        var mpy = 8.6f;
        var mpz = 12.0f;
        if (boundingDimensions.x > mpx)
            return false;
        else if (boundingDimensions.y > mpy)
            return false;
        else if (boundingDimensions.z > mpz)
            return false;
        return true;
    }

    private static bool HandleCyclops(AvsVehicle av)
    {
        //Vector3 boundingDimensions = av.CyclopsDockRotation * av.GetBoundingDimensions();
        var boundingDimensions = av.GetDockingBoundsSize();
        if (boundingDimensions == Vector3.zero)
            return false;
        var mpx = 4.5f;
        var mpy = 6.0f;
        var mpz = 4.5f;
        if (boundingDimensions.x > mpx)
            return false;
        else if (boundingDimensions.y > mpy)
            return false;
        else if (boundingDimensions.z > mpz)
            return false;
        return true;
    }

    private static bool IsThisDockable(VehicleDockingBay bay, GameObject nearby) =>
        !IsThisASubmarineWithStandingPilot(nearby)
        && IsThisVehicleSmallEnough(bay, nearby);

    private static bool IsThisASubmarineWithStandingPilot(GameObject nearby)
    {
        var av = UWE.Utils.GetComponentInHierarchy<VehicleTypes.Submarine>(nearby.gameObject);
        if (av.IsNull())
            return false;
        if (av.IsBoarded && !av.IsPlayerPiloting())
            return true;
        return false;
    }

    private static bool IsThisVehicleSmallEnough(VehicleDockingBay bay, GameObject nearby)
    {
        var av = UWE.Utils.GetComponentInHierarchy<AvsVehicle>(nearby.gameObject);
        if (av.IsNull())
            return true;
        if (!av.Config.CanMoonpoolDock || av.docked)
            return false;
        var subRootName = bay.subRoot.name.ToLower();
        if (subRootName.Contains("base"))
        {
            return HandleMoonpool(av);
        }
        else if (subRootName.Contains("cyclops"))
        {
            return HandleCyclops(av);
        }
        else
        {
            Logger.Warn("Trying to dock in something that is neither a moonpool nor a cyclops. What is this?");
            return false;
        }
    }

    private static void HandleMVDocked(Vehicle vehicle, VehicleDockingBay dock)
    {
        var av = vehicle as AvsVehicle;
        if (av.IsNotNull())
        {
            var moonpool = dock.GetComponentInParent<Moonpool>();
            var cmm = dock.GetComponentInParent<CyclopsMotorMode>();
            if (av.IsBoarded)
            {
                Player.main.SetCurrentSub(dock.GetSubRoot(), true);
                Player.main.ToNormalMode(false);
            }

            if (moonpool.IsNotNull() || cmm.IsNotNull())
            {
                var playerSpawn = dock.transform.Find("playerSpawn");
                av.DockVehicle(playerSpawn.position);
            }
            else
            {
                av.Log.Warn(av.Owner.ModName +
                            " AVS is not aware of this dock. The player is probably in a weird position now.");
                av.DockVehicle();
            }
        }
    }

    /// <summary>
    /// Modifies the behavior of the VehicleDockingBay's LateUpdate method to add animation functionality
    /// when an AvsVehicle is docked. If the currently docked vehicle is an instance of AvsVehicle,
    /// it triggers the moonpool arm animation logic within the docked vehicle.
    /// </summary>
    /// <param name="__instance">The instance of the VehicleDockingBay currently being updated.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(VehicleDockingBay.LateUpdate))]
    // This patch animates the docking bay arms as if a seamoth is docked
    public static void LateUpdatePostfix(VehicleDockingBay __instance)
    {
        var av = __instance.dockedVehicle as AvsVehicle;
        if (av.IsNotNull())
            av.AnimateMoonPoolArms(__instance);
    }

    /// <summary>
    /// Extends the behavior of the VehicleDockingBay's DockVehicle method to handle additional logic
    /// specific to the docking of an AvsVehicle. This includes notifying the docked vehicle and
    /// managing docking animations or other related functionality.
    /// </summary>
    /// <param name="__instance">The instance of the VehicleDockingBay where the vehicle is docked.</param>
    /// <param name="vehicle">The vehicle being docked into the docking bay.</param>
    /// <param name="rebuildBase">A flag indicating whether the base should be rebuilt during the docking process.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(VehicleDockingBay.DockVehicle))]
    // This patch ensures a modvehicle docks correctly
    public static void DockVehiclePostfix(VehicleDockingBay __instance, Vehicle vehicle, bool rebuildBase)
    {
        HandleMVDocked(vehicle, __instance);
    }

    /// <summary>
    /// Enhances the operation of the VehicleDockingBay's SetVehicleDocked method to handle the docking
    /// of AvsVehicle instances by invoking necessary custom logic. This ensures that when a vehicle is
    /// docked, additional actions specific to AvsVehicle instances are triggered.
    /// </summary>
    /// <param name="__instance">The instance of the VehicleDockingBay where the vehicle is being docked.</param>
    /// <param name="vehicle">The vehicle being docked into the VehicleDockingBay.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(VehicleDockingBay.SetVehicleDocked))]
    // This patch ensures a modvehicle docks correctly
    public static void SetVehicleDockedPostfix(VehicleDockingBay __instance, Vehicle vehicle)
    {
        HandleMVDocked(vehicle, __instance);
    }

    /// <summary>
    /// Determines if a vehicle entering the trigger collider of the docking bay is dockable.
    /// Verifies the compatibility of the nearby game object with the docking bay mechanism
    /// and initiates docking procedures if applicable.
    /// </summary>
    /// <param name="__instance">The instance of the VehicleDockingBay being interacted with.</param>
    /// <param name="other">The collider of the object entering the docking bay's trigger area.</param>
    /// <returns>Returns true if the collider represents a dockable vehicle, otherwise false.</returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(VehicleDockingBay.OnTriggerEnter))]
    // This patch controls whether to dock a AvsVehicle. Only small AvsVehicles are accepted.
    public static bool OnTriggerEnterPrefix(VehicleDockingBay __instance, Collider other) =>
        IsThisDockable(__instance, other.gameObject);

    /// <summary>
    /// Adjusts the position and rotation of a docked AvsVehicle during the docking process.
    /// This prefix changes the default behavior of the UpdateDockedPosition method
    /// to handle specific cases for moonpool and cyclops docking, ensuring proper alignment
    /// and animation synchronization.
    /// </summary>
    /// <param name="__instance">The instance of the VehicleDockingBay being updated.</param>
    /// <param name="vehicle">The vehicle currently being docked, expected to be an AvsVehicle.</param>
    /// <param name="interpfraction">The interpolation fraction indicating the progress of the docking process.</param>
    /// <returns>Returns false to replace the original method's execution when the vehicle is an AvsVehicle,
    /// and true otherwise.</returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(VehicleDockingBay.UpdateDockedPosition))]
    public static bool UpdateDockedPositionPrefix(VehicleDockingBay __instance, Vehicle vehicle,
        float interpfraction)
    {
        var av = vehicle as AvsVehicle;
        if (av.IsNull())
            return true;
        Transform endingTransform;
        var subRootName = __instance.subRoot.name.ToLower();
        if (subRootName.Contains("base"))
        {
            endingTransform = __instance.dockingEndPos.parent.parent;
        }
        else if (subRootName.Contains("cyclops"))
        {
            endingTransform = __instance.dockingEndPos;
        }
        else
        {
            Logger.Warn("Trying to dock in something that is neither a moonpool nor a cyclops. What is this?");
            return true;
        }

        if (!av.IsUndockingAnimating)
        {
            vehicle.transform.position =
                Vector3.Lerp(__instance.startPosition, endingTransform.position, interpfraction) -
                av.GetDockingDifferenceFromCenter();
            //vehicle.transform.rotation = Quaternion.Lerp(__instance.startRotation, av.CyclopsDockRotation * endingTransform.rotation, interpfraction);
            vehicle.transform.rotation =
                Quaternion.Lerp(__instance.startRotation, endingTransform.rotation, interpfraction);
        }

        return false;
    }

    /// <summary>
    /// Determines whether the object entering the launch bay area is dockable based on custom
    /// criteria. If the object is deemed dockable, it allows subsequent logic to proceed.
    /// </summary>
    /// <param name="__instance">The instance of the VehicleDockingBay encountering the entering object.</param>
    /// <param name="nearby">The GameObject that has entered the launch bay area trigger.</param>
    /// <returns>Returns true if the object is dockable, otherwise false to prevent further processing.</returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(VehicleDockingBay.LaunchbayAreaEnter))]
    public static bool LaunchbayAreaEnterPrefix(VehicleDockingBay __instance, GameObject nearby) =>
        IsThisDockable(__instance, nearby);

    /// <summary>
    /// Determines if a nearby object is eligible to exit the launch bay area and prevents
    /// interaction with objects that do not meet the docking criteria.
    /// </summary>
    /// <param name="__instance">The instance of the VehicleDockingBay being evaluated.</param>
    /// <param name="nearby">The GameObject that is attempting to exit the launch bay area.</param>
    /// <returns>True if the nearby object is dockable, otherwise false to block the exit interaction.</returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(VehicleDockingBay.LaunchbayAreaExit))]
    public static bool LaunchbayAreaExitPrefix(VehicleDockingBay __instance, GameObject nearby) =>
        IsThisDockable(__instance, nearby);

    /// <summary>
    /// Adds the instance of the VehicleDockingBay to the list of docking bays when the VehicleDockingBay starts.
    /// This enables tracking and management of all active docking bays for further operations or interactions.
    /// </summary>
    /// <param name="__instance">The VehicleDockingBay instance that is being initialized.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(VehicleDockingBay.Start))]
    public static void VehicleDockingBayStartPostfix(VehicleDockingBay __instance)
    {
        DockingBayList.Add(__instance);
    }

    /// <summary>
    /// Modifies the behavior of the VehicleDockingBay's OnDestroy method to ensure
    /// the docking bay is removed from the internal tracking list when it is destroyed.
    /// This prevents stale references to destroyed docking bay instances, maintaining
    /// the integrity of the docking bay management system.
    /// </summary>
    /// <param name="__instance">The instance of the VehicleDockingBay being destroyed.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(VehicleDockingBay.OnDestroy))]
    public static void VehicleDockingBayOnDestroyPostfix(VehicleDockingBay __instance)
    {
        DockingBayList.Remove(__instance);
    }

    private static List<VehicleDockingBay> DockingBayList { get; } = new();


    /// <summary>
    /// A read-only list of all registered VehicleDockingBay instances.
    /// </summary>
    /// <remarks>
    /// The property provides access to the internal collection of docking bays managed by the patch.
    /// It enables iterating through or performing actions on all loaded VehicleDockingBay instances.
    /// </remarks>
    public static IReadOnlyList<VehicleDockingBay> DockingBays => DockingBayList;
}

/// <summary>
/// Provides Harmony patches for the <c>VehicleDockingBay</c> class, focusing on enhancing docking behavior for vehicles,
/// specifically addressing compatibility with <c>AvsVehicle</c>. These patches allow seamless docking of in-game vehicles
/// with customized logic and ensure proper operation within docking bays. The key functionality includes:
/// - Intercepting the method <c>VehicleDockingBay.LateUpdate</c> to modify docking behavior.
/// - Enabling or restricting cinematic mode initiation during docking, based on the <c>AvsVehicle</c> type.
/// - Maintaining compatibility with vanilla vehicle docking processes.
/// - Safeguarding against improper cinematic triggers for non-standard vehicles.
/// This implementation supplements base docking behavior and integrates with extended vehicle systems.
/// </summary>
[HarmonyPatch(typeof(VehicleDockingBay))]
public static class VehicleDockingBayPatch2
{
    /// <summary>
    /// Transpiler method that modifies the IL code of the VehicleDockingBay's LateUpdate method.
    /// It alters the behavior of the StartCinematicMode call, adding a delegate to invoke custom logic
    /// for handling cinematic mode transitions in specific scenarios.
    /// </summary>
    /// <param name="instructions">The original collection of IL instructions from the LateUpdate method.</param>
    /// <returns>A modified collection of IL instructions that includes custom logic for cinematic mode handling.</returns>
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(VehicleDockingBay.LateUpdate))]
    public static IEnumerable<CodeInstruction> VehicleDockingBayLateUpdateTranspiler(
        IEnumerable<CodeInstruction> instructions)
    {
        var startCinematicMatch = new CodeMatch(i =>
            i.opcode == OpCodes.Callvirt && i.operand.ToString().Contains("StartCinematicMode"));

        var newInstructions = new CodeMatcher(instructions)
            .MatchStartForward(startCinematicMatch)
            .Repeat(x =>
                x.RemoveInstruction()
                    .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_0))
                    .Insert(Transpilers.EmitDelegate<Action<PlayerCinematicController, Player, Vehicle>>(
                        MaybeStartCinematicMode))
            );

        return newInstructions.InstructionEnumeration();
    }


    private static void MaybeStartCinematicMode(PlayerCinematicController cinematic, Player player, Vehicle vehicle)
    {
        if (!(vehicle is AvsVehicle))
            cinematic.StartCinematicMode(player);
    }
}