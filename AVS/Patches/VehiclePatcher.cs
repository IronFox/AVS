using AVS.BaseVehicle;
using AVS.Localization;
using AVS.Util;
using AVS.Util.Math;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

// PURPOSE: generally ensures AvsVehicles behave like normal Vehicles
// VALUE: Very high.

namespace AVS.Patches;

/// <summary>
/// A class designed to patch and modify the behavior of the Vehicle class.
/// The modifications primarily ensure that AvsVehicle instances behave appropriately
/// within the context of the base Vehicle class functionality.
/// </summary>
/// <remarks>
/// This class uses Harmony patches to inject custom behavior at runtime. Each patch ensures
/// compatibility and extended functionality for AvsVehicle instances. Specific logic is implemented for
/// lifecycle methods, energy management, storage handling, and other core vehicle operations.
/// </remarks>
/// <example>
/// The VehiclePatcher class intercepts and adjusts specific methods of the Vehicle class to
/// support specialized behavior for AvsVehicle instances, such as overriding initialization, storage
/// interactions, and powering behavior.
/// </example>
[HarmonyPatch(typeof(Vehicle))]
public class VehiclePatcher
{
    /*
     * This collection of patches generally ensures our AvsVehicles behave like normal Vehicles.
     * Each will be commented if necessary
     */

    /// <summary>
    /// A Harmony prefix patch for the OnHandHover method in the Vehicle class.
    /// This prefix is intended to modify the behavior of AvsVehicle instances while maintaining the default logic for normal Vehicle instances.
    /// </summary>
    /// <param name="__instance">The Vehicle instance on which the method is being called. This can be cast to AvsVehicle if applicable.</param>
    /// <returns>
    /// A boolean indicating whether the original OnHandHover method in the Vehicle class should be executed.
    /// Return true to allow the original method to execute; return false to skip the original method logic.
    /// </returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Vehicle.OnHandHover))]
    public static bool OnHandHoverPrefix(Vehicle __instance)
    {
        var av = __instance as AvsVehicle;
        if (av.IsNull())
        {
            //if (VehicleTypes.Drone.mountedDrone.IsNotNull())
            //{
            //    return false;
            //}
        }
        else
        {
            if (av.isScuttled)
            {
                var now = av.GetComponent<Sealed>().openedAmount;
                var max = av.GetComponent<Sealed>().maxOpenedAmount;
                var percent = now.Percentage(max);
                HandReticle.main.SetText(HandReticle.TextType.Hand,
                    Translator.GetFormatted(TranslationKey.HandHover_Vehicle_DeconstructionPercent, percent), true,
                    GameInput.Button.None);
            }
            else if (av.IsBoarded)
            {
                HandReticle.main.SetText(HandReticle.TextType.Hand, "", true, GameInput.Button.None);
            }
            else
            {
                HandReticle.main.SetText(HandReticle.TextType.Hand, av.GetName(), true, GameInput.Button.None);
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// A Harmony prefix patch for the ApplyPhysicsMove method in the Vehicle class.
    /// This prefix modifies the physics behavior for instances of the AvsVehicle class
    /// while retaining the default behavior for other Vehicle instances.
    /// </summary>
    /// <param name="__instance">The Vehicle instance on which the method is being called. This can be cast to AvsVehicle if applicable.</param>
    /// <param name="___wasAboveWater">A reference to a boolean indicating whether the vehicle was above water in the last frame.</param>
    /// <param name="___accelerationModifiers">A reference to an array of VehicleAccelerationModifier instances that influence the vehicle's acceleration behavior.</param>
    /// <returns>
    /// A boolean value indicating whether the original ApplyPhysicsMove method in the Vehicle class should be executed.
    /// Return true to allow the original method to execute; return false to skip the original method logic.
    /// </returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Vehicle.ApplyPhysicsMove))]
    private static bool ApplyPhysicsMovePrefix(Vehicle __instance, ref bool ___wasAboveWater,
        ref VehicleAccelerationModifier[] ___accelerationModifiers)
    {
        var av = __instance as AvsVehicle;
        if (av.IsNotNull())
            return false;
        return true;
    }

    /// <summary>
    /// A Harmony prefix patch for the LazyInitialize method in the Vehicle class.
    /// This prefix is used to initialize the EnergyInterface for AvsVehicle instances while preserving the behavior for standard Vehicle instances.
    /// </summary>
    /// <param name="__instance">The Vehicle instance being initialized. This can be cast to AvsVehicle if applicable.</param>
    /// <param name="___energyInterface">A reference to the EnergyInterface field of the Vehicle instance to initialize or update.</param>
    /// <returns>
    /// A boolean indicating whether the original LazyInitialize method in the Vehicle class should execute.
    /// Return true to allow the original method to proceed; return false to skip the original method logic.
    /// </returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Vehicle.LazyInitialize))]
    public static bool LazyInitializePrefix(Vehicle __instance, ref EnergyInterface ___energyInterface)
    {
        var av = __instance as AvsVehicle;
        if (av.IsNull())
            return true;

        ___energyInterface = __instance.gameObject.GetComponent<EnergyInterface>();
        return true;
    }

    /// <summary>
    /// A Harmony postfix patch for the GetAllStorages method in the Vehicle class.
    /// This method adds additional storage containers specific to AvsVehicle instances
    /// to the list of containers collected by the original Vehicle logic.
    /// </summary>
    /// <param name="__instance">The instance of the Vehicle class for which the GetAllStorages method is called. Can be cast to AvsVehicle if applicable.</param>
    /// <param name="containers">A reference to the list of IItemsContainer objects that the original method collects. Additional containers are appended to this list.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Vehicle.GetAllStorages))]
    public static void GetAllStoragesPostfix(Vehicle __instance, ref List<IItemsContainer> containers)
    {
        var av = __instance as AvsVehicle;
        if (av.IsNull())
            return;

        foreach (var tmp in ((AvsVehicle)__instance).Com.InnateStorages)
        {
            var ic = tmp.Container.GetComponent<InnateStorageContainer>();
            if (ic.IsNotNull())
                containers.Add(ic.Container);
        }
    }

    /// <summary>
    /// A Harmony postfix patch for the IsPowered method in the Vehicle class.
    /// This postfix modifies the result of the IsPowered method for instances of AvsVehicle
    /// by taking into account the IsPoweredOn state of the AvsVehicle instance.
    /// </summary>
    /// <param name="__instance">The Vehicle instance on which the method is being called. This can be cast to AvsVehicle if applicable.</param>
    /// <param name="___energyInterface">The energy interface associated with the Vehicle, which reflects its energy behavior.</param>
    /// <param name="__result">A reference to the original method result. This value can be modified by the postfix.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Vehicle.IsPowered))]
    public static void IsPoweredPostfix(Vehicle __instance, ref EnergyInterface ___energyInterface,
        ref bool __result)
    {
        var av = __instance as AvsVehicle;
        if (av.IsNull())
            return;
        if (!av.IsPoweredOn)
            __result = false;
    }

    /// <summary>
    /// A Harmony transpiler patch for the Update method in the Vehicle class.
    /// This transpiler ensures compatibility and introduces custom rotation control for AvsVehicle instances,
    /// maintaining integration with existing Vehicle logic.
    /// </summary>
    /// <param name="instructions">The original sequence of IL code instructions from the Update method.</param>
    /// <returns>
    /// A modified sequence of IL code instructions, with additional logic injected to handle AvsVehicle-specific rotation control.
    /// </returns>
    [HarmonyPatch(nameof(Vehicle.Update))]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        /* This is basically a prefix for Vehicle.Update,
         * but we choose to transpile instead,
         * so that our code may be considered "core."
         * That is, it will be skipped if any other Prefix returns false.
         * This is desirable to be as "alike" normal Vehicles as possible;
         * in particular, this ensures compatibility with FreeLook
         * We must control our AvsVehicle rotation within the core Vehicle.Update code.
         */
        var codes = new List<CodeInstruction>(instructions);
        var newCodes = new List<CodeInstruction>(codes.Count + 2);
        var myNOP = new CodeInstruction(OpCodes.Nop);
        for (var i = 0; i < codes.Count + 2; i++)
            newCodes.Add(myNOP);
        // push reference to vehicle
        // Call a static function which takes a vehicle and ControlsRotation if it's a AvsVehicle
        newCodes[0] = new CodeInstruction(OpCodes.Ldarg_0);
        newCodes[1] = CodeInstruction.Call(typeof(AvsVehicle), nameof(AvsVehicle.MaybeControlRotation));
        for (var i = 0; i < codes.Count; i++)
            newCodes[i + 2] = codes[i];
        return newCodes.AsEnumerable();
    }


    /// <summary>
    /// A Harmony prefix patch for the ReAttach method in the Vehicle class.
    /// This prefix ensures that the docking bay is notified properly when a Vehicle instance is re-attached.
    /// </summary>
    /// <param name="__instance">The Vehicle instance on which the ReAttach method is being invoked.</param>
    /// <returns>
    /// A boolean indicating whether the original ReAttach method in the Vehicle class should be executed.
    /// Return true to allow the original method to execute; return false to skip the original method logic.
    /// </returns>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Vehicle.ReAttach))]
    public static bool VehicleReAttachPrefix(Vehicle __instance)
    {
        IEnumerator NotifyDockingBay(Transform baseCell)
        {
            if (baseCell.IsNull())
                yield break;
            yield return new WaitUntil(() => baseCell.Find("BaseMoonpool(Clone)").IsNotNull());
            var thisBasesBays = baseCell.SafeGetComponentsInChildren<VehicleDockingBay>(true);
            if (thisBasesBays.IsNotNull())
            {
                const float expectedMaxDistance = 5f;
                foreach (var bay in thisBasesBays)
                    if (bay.IsNotNull() && Vector3.Distance(__instance.transform.position, bay.transform.position) <
                        expectedMaxDistance)
                        bay.DockVehicle(__instance, false);
            }
        }

        RootModController.AnyInstance.StartAvsCoroutine(
            nameof(VehiclePatcher) + '.' + nameof(NotifyDockingBay),
            _ => NotifyDockingBay(__instance.transform.parent.Find("BaseCell(Clone)")));
        return true;
    }

    /// <summary>
    /// A Harmony postfix patch for the Awake method in the Vehicle class.
    /// This patch registers the Vehicle instance with the GameObjectManager, enabling further management and tracking of the instance.
    /// </summary>
    /// <param name="__instance">The Vehicle instance that has completed its Awake initialization.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Vehicle.Awake))]
    public static void VehicleAwakeHarmonyPostfix(Vehicle __instance)
    {
        Admin.GameObjectManager<Vehicle>.Register(__instance);
    }
}

/// <summary>
/// A class that extends and customizes the behavior of the Vehicle class through Harmony patches.
/// It primarily focuses on modifying energy management functionality to accommodate specific behaviors
/// for AvsVehicle instances and ensure seamless integration with the base Vehicle class.
/// </summary>
/// <remarks>
/// VehiclePatcher2 applies a transpiler to the Vehicle class's energy recharge logic, allowing for
/// dynamic modifications that enhance compatibility with AvsVehicles. Additionally, it provides
/// custom logic for retrieving appropriate PowerRelay objects associated with vehicles, depending
/// on their specific type and hierarchy within the game world.
/// </remarks>
[HarmonyPatch(typeof(Vehicle))]
public class VehiclePatcher2
{
    /* This transpiler makes one part of UpdateEnergyRecharge more generic
     * Optionally change GetComponentInParent to GetComponentInParentButNotInMe
     * Simple as.
     * The purpose is to ensure AvsVehicles are recharged while docked.
     */
    /// <summary>
    /// Transpiler method for the Vehicle.UpdateEnergyRecharge method using Harmony.
    /// This method modifies the IL code of the original UpdateEnergyRecharge method to make
    /// the energy recharge logic more generic and optionally change component retrieval behavior,
    /// ensuring compatibility with AvsVehicles while docked.
    /// </summary>
    /// <param name="instructions">A collection of CodeInstruction objects representing the IL code of the Vehicle.UpdateEnergyRecharge method.</param>
    /// <returns>
    /// An IEnumerable of CodeInstruction objects representing the modified IL code to be executed in place of the original method.
    /// </returns>
    [HarmonyPatch(nameof(Vehicle.UpdateEnergyRecharge))]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var newCodes = new List<CodeInstruction>(codes.Count);
        var myNOP = new CodeInstruction(OpCodes.Nop);
        for (var i = 0; i < codes.Count; i++)
            newCodes.Add(myNOP);
        for (var i = 0; i < codes.Count; i++)
            if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString().ToLower().Contains("powerrelay"))
                newCodes[i] = CodeInstruction.Call(typeof(VehiclePatcher2), nameof(GetPowerRelayAboveVehicle));
            else
                newCodes[i] = codes[i];

        return newCodes.AsEnumerable();
    }

    private static PowerRelay GetPowerRelayAboveVehicle(Vehicle veh)
    {
        if ((veh as AvsVehicle).IsNull())
            return veh.GetComponentInParent<PowerRelay>();
        else
            return veh.transform.parent.gameObject.GetComponentInParent<PowerRelay>();
    }
}