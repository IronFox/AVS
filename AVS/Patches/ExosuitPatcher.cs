using AVS.Crafting;
using AVS.UpgradeModules.Variations;
using AVS.Util;
using HarmonyLib;

// PURPOSE: allow VF upgrades for the Prawn to be used as expected
// VALUE: High.

namespace AVS.Patches;

/* This set of patches is meant to only effect Exosuits.
 * For whatever reason, Exosuit does not implement
 * OnUpgradeModuleUse or
 * OnUpgradeModuleToggle
 * So we patch those in Vehicle here.
 *
 * The purpose of these patches is to let our
 * AvsVehicleUpgrades be usable.
 */
/// <summary>
/// This class contains Harmony patches designed to extend and modify the functionality
/// of vehicles, specifically Exosuits, in order to enable proper handling of custom
/// vehicle upgrades.
/// </summary>
/// <remarks>
/// The Exosuit does not originally implement methods such as `OnUpgradeModuleUse`
/// or `OnUpgradeModuleToggle`. These patches address this limitation by intercepting
/// and adding the required functionality to those methods through the vehicle class.
/// The purpose of these modifications is to make custom upgrade modules usable
/// within Exosuits.
/// </remarks>
[HarmonyPatch(typeof(Vehicle))]
public class VehicleExosuitPatcher
{
    /// <summary>
    /// Postfix method for the <c>Vehicle.OnUpgradeModuleToggle</c> Harmony patch. This method is invoked
    /// after the <c>OnUpgradeModuleToggle</c> method is called for a vehicle. It ensures that custom
    /// functionality is executed when a toggleable upgrade module is activated or deactivated in an Exosuit.
    /// </summary>
    /// <param name="__instance">The instance of the <c>Vehicle</c> that the upgrade module belongs to.</param>
    /// <param name="slotID">The slot ID of the upgrade module being toggled.</param>
    /// <param name="active">
    /// A boolean indicating whether the upgrade module in the specified slot is being activated (<c>true</c>)
    /// or deactivated (<c>false</c>).
    /// </param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Vehicle.OnUpgradeModuleToggle))]
    public static void VehicleOnUpgradeModuleTogglePostfix(Vehicle __instance, int slotID, bool active)
    {
        var exo = __instance as Exosuit;
        if (exo.IsNotNull())
        {
            var techType = exo.modules.GetTechTypeInSlot(exo.slotIDs[slotID]);
            if (UpgradeRegistrar.OnToggleActions.TryGetValue(techType, out var tracker))
                tracker.OnToggle(exo, slotID, active);
        }
    }

    /// <summary>
    /// Postfix method for the <c>Vehicle.OnUpgradeModuleUse</c> Harmony patch. This method is invoked
    /// after the <c>OnUpgradeModuleUse</c> method is called for a vehicle. It ensures that custom
    /// functionality is executed when an upgrade module is used in an Exosuit, enabling additional
    /// behaviors or handling for specific modules.
    /// </summary>
    /// <param name="__instance">The instance of the <c>Vehicle</c> being patched, which is expected to be an Exosuit.</param>
    /// <param name="techType">The technology type of the upgrade module being used.</param>
    /// <param name="slotID">The slot ID of the upgrade module being used.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Vehicle.OnUpgradeModuleUse))]
    public static void VehicleOnUpgradeModuleUsePostfix(Vehicle __instance, TechType techType, int slotID)
    {
        var exo = __instance as Exosuit;
        if (exo.IsNotNull())
        {
            var param = new SelectableModule.Params
            (
                __instance,
                slotID,
                techType
            );
            UpgradeRegistrar.OnSelectActions.ForEach(x => x(param));

            var charge = param.Vehicle.quickSlotCharge[param.SlotID];
            var chargeFraction = param.Vehicle.GetSlotCharge(param.SlotID);

            var param2 = new SelectableChargeableModule.Params
            (
                __instance,
                slotID,
                techType,
                charge,
                chargeFraction
            );
            UpgradeRegistrar.OnSelectChargeActions.ForEach(x => x(param2));

            var param3 = new ChargeableModule.Params
            (
                __instance,
                slotID,
                techType,
                charge,
                chargeFraction
            );
            UpgradeRegistrar.OnChargeActions.ForEach(x => x(param3));
        }
    }
}

/// <summary>
/// This class applies Harmony patches to the `Exosuit` class to introduce
/// functionality for handling interactions with upgrade modules.
/// The patches adjust the behavior of input-related methods to extend support
/// for custom vehicle upgrades.
/// </summary>
/// <remarks>
/// The `Exosuit` class inherently lacks support for certain methods such as
/// `OnUpgradeModuleUse` and `OnUpgradeModuleToggle`, which prevents its proper
/// integration with custom upgrade modules. This class overrides and extends
/// input processing methods (e.g., key presses and releases) to enable
/// support for features like module activation, toggling, and charging during
/// gameplay.
/// </remarks>
[HarmonyPatch(typeof(Exosuit))]
public class ExosuitPatcher
{
    private static void DoSlotDown(Exosuit exo, int slotID, bool isJank)
    {
        if (!exo.playerFullyEntered || exo.ignoreInput)
            return;
        if (slotID < 0)
            // Usually this means slotID was -1,
            // which means there was no active slot.
            return;
        if (exo.GetQuickSlotCooldown(slotID) != 1f)
            // cooldown isn't finished!
            return;
        var slotIDToTest = isJank ? slotID - 2 : slotID;
        //if (!Player.main.GetQuickSlotKeyDown(slotIDToTest) && !GameInput.GetButtonHeld(GameInput.Button.LeftHand))
        //{
        //    // we didn't actually hit the slot button!
        //    // (or hit the left mouse button!)
        //    // this prevents activation on SlotNext and SlotPrev
        //    return;
        //}
        var quickSlotType = exo.GetQuickSlotType(slotID, out var techType);
        if (quickSlotType == QuickSlotType.Selectable)
        {
            if (exo.ConsumeEnergy(techType))
                exo.OnUpgradeModuleUse(techType, slotID);
        }
        else if (quickSlotType == QuickSlotType.SelectableChargeable)
        {
            exo.quickSlotCharge[slotID] = 0f;
        }
    }

    private static void DoSlotHeld(Exosuit exo, int slotID)
    {
        if (!exo.playerFullyEntered || exo.ignoreInput)
            return;
        var quickSlotType = exo.GetQuickSlotType(slotID, out var techType);
        if (quickSlotType == QuickSlotType.SelectableChargeable)
            exo.ChargeModule(techType, slotID);
    }

    private static void DoSlotUp(Exosuit exo, int slotID)
    {
        if (!exo.playerFullyEntered || exo.ignoreInput)
            return;
        var quickSlotType = exo.GetQuickSlotType(slotID, out var techType);
        if (quickSlotType == QuickSlotType.SelectableChargeable)
        {
            if (exo.ConsumeEnergy(techType))
                exo.OnUpgradeModuleUse(techType, slotID);
            exo.quickSlotCharge[slotID] = 0f;
        }
    }

    /// <summary>
    /// Postfix method for the <c>Exosuit.SlotKeyDown</c> Harmony patch. This method is executed
    /// after the <c>SlotKeyDown</c> method of the <c>Exosuit</c> class to trigger custom functionality
    /// when a slot key is pressed in the Exosuit's upgrade module system.
    /// </summary>
    /// <param name="__instance">The instance of the <c>Exosuit</c> that the action is being performed on.</param>
    /// <param name="slotID">The identifier for the specific upgrade module slot being interacted with.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Exosuit.SlotKeyDown))]
    public static void ExosuitSlotKeyDownPostfix(Exosuit __instance, int slotID)
    {
        DoSlotDown(__instance, slotID, true);
    }

    /// <summary>
    /// Postfix method for the <c>Exosuit.SlotKeyHeld</c> Harmony patch. This method is invoked
    /// when a slot key is held down for an Exosuit. It ensures appropriate functionality related
    /// to the held slot is executed, such as handling custom upgrade module behaviors.
    /// </summary>
    /// <param name="__instance">The instance of the <c>Exosuit</c> where the slot key is being held.</param>
    /// <param name="slotID">The ID of the slot associated with the key being held.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Exosuit.SlotKeyHeld))]
    public static void ExosuitSlotKeyHeldPostfix(Exosuit __instance, int slotID)
    {
        DoSlotHeld(__instance, slotID);
    }

    /// <summary>
    /// Postfix method for the <c>Exosuit.SlotKeyUp</c> Harmony patch. This method is invoked
    /// after the <c>SlotKeyUp</c> method is executed in an <c>Exosuit</c>. It handles the
    /// release action of a specific upgrade slot and ensures proper processing of custom
    /// upgrade functionalities.
    /// </summary>
    /// <param name="__instance">The instance of the <c>Exosuit</c> for which the slot key action occurred.</param>
    /// <param name="slotID">The slot ID associated with the released key in the <c>Exosuit</c>.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Exosuit.SlotKeyUp))]
    public static void ExosuitSlotKeyUpPostfix(Exosuit __instance, int slotID)
    {
        DoSlotUp(__instance, slotID);
    }

    /// <summary>
    /// Postfix method for the <c>Exosuit.SlotLeftDown</c> Harmony patch. This method is invoked
    /// after the <c>SlotLeftDown</c> method is called for an Exosuit. It processes interactions
    /// with the currently active upgrade module when the left-hand slot is pressed down.
    /// </summary>
    /// <param name="__instance">The instance of the <c>Exosuit</c> for which the left-hand slot is activated.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Exosuit.SlotLeftDown))]
    public static void ExosuitSlotLeftDownPostfix(Exosuit __instance)
    {
        if (!AvatarInputHandler.main.IsEnabled())
            return;
        var slotID = __instance.activeSlot;
        DoSlotDown(__instance, slotID, false);
    }

    /// <summary>
    /// Postfix method for the <c>Exosuit.SlotLeftHeld</c> Harmony patch. This method is invoked
    /// after the <c>SlotLeftHeld</c> method is called to ensure functionality related to
    /// continuous interaction with upgrade modules is processed while the left interaction button is held.
    /// </summary>
    /// <param name="__instance">The instance of the <c>Exosuit</c> being interacted with.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Exosuit.SlotLeftHeld))]
    public static void ExosuitSlotLeftHeldPostfix(Exosuit __instance)
    {
        if (!AvatarInputHandler.main.IsEnabled())
            return;
        var slotID = __instance.activeSlot;
        DoSlotHeld(__instance, slotID);
    }

    /// <summary>
    /// Postfix method for the <c>Exosuit.SlotLeftUp</c> Harmony patch. This method is executed
    /// after the <c>SlotLeftUp</c> method is called on an <c>Exosuit</c>, enabling custom actions
    /// when the left slot key is released for the currently active slot.
    /// </summary>
    /// <param name="__instance">The instance of the <c>Exosuit</c> the method is invoked on, representing the player's current vehicle.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Exosuit.SlotLeftUp))]
    public static void ExosuitSlotLeftUpPostfix(Exosuit __instance)
    {
        if (!AvatarInputHandler.main.IsEnabled())
            return;
        var slotID = __instance.activeSlot;
        DoSlotUp(__instance, slotID);
    }
}