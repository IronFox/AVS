using AVS.Crafting;
using AVS.UpgradeModules;
using AVS.Util;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.Patches;

/// <summary>
/// Helper for enabling AVS upgrades on a Cyclops.
/// </summary>
internal class AvsUpgradesListener : MonoBehaviour
{
    private UpgradeConsole UpgradeConsole => GetComponent<UpgradeConsole>();
    private SubRoot Subroot => GetComponentInParent<SubRoot>();

    private int GetSlotNumber(string slot)
    {
        for (var i = 0; i < UpgradeConsole.modules.equipment.Count; i++)
            try
            {
                if (slot == SubRoot.slotNames[i])
                    return i;
            }
            catch
            {
                Logger.Warn("Cyclops Upgrades Error: Didn't know about Cyclops Upgrade Slot Name for Slot #" +
                            i.ToString());
            }

        return -1;
    }

    public void OnSlotEquipped(string slot, InventoryItem item)
    {
        IEnumerator BroadcastMessageSoon()
        {
            yield return new WaitUntil(() => Subroot.IsNotNull());
            Subroot.BroadcastMessage("UpdateAbilities", null, SendMessageOptions.DontRequireReceiver);
        }

        if (item.techType != TechType.None)
        {
            var addedParams = AddActionParams.CreateForCyclops
            (
                Subroot,
                GetSlotNumber(slot),
                item.techType,
                true
            );
            UpgradeRegistrar.OnAddActions.ForEach(x => x(addedParams));
            MainPatcher.AnyInstance.StartCoroutine(BroadcastMessageSoon());
        }
    }

    public void OnSlotUnequipped(string slot, InventoryItem item)
    {
        IEnumerator BroadcastMessageSoon()
        {
            yield return new WaitUntil(() => Subroot.IsNotNull());
            Subroot.BroadcastMessage("UpdateAbilities", null, SendMessageOptions.DontRequireReceiver);
        }

        if (item.techType != TechType.None)
        {
            var addedParams = AddActionParams.CreateForCyclops
            (
                Subroot,
                GetSlotNumber(slot),
                item.techType,
                false
            );
            UpgradeRegistrar.OnAddActions.ForEach(x => x(addedParams));
            MainPatcher.AnyInstance.StartCoroutine(BroadcastMessageSoon());
        }
    }

    internal void BumpUpgrade(KeyValuePair<string, InventoryItem> upgrade)
    {
        if (upgrade.Value.IsNotNull())
        {
            OnSlotUnequipped(upgrade.Key, upgrade.Value);
            OnSlotEquipped(upgrade.Key, upgrade.Value);
        }
    }
}

/// <summary>
/// Patcher for Cyclops Upgrade Console to ensure AVS upgrades work correctly.
/// </summary>
[HarmonyPatch(typeof(UpgradeConsole))]
public class UpgradeConsolePatcher
{
    /// <summary>
    /// Postfix method invoked after the `Awake` method of the UpgradeConsole has been executed.
    /// Ensures that the UpgradeConsole is properly configured to support AVS upgrades.
    /// </summary>
    /// <param name="__instance">The instance of the UpgradeConsole that has been initialized.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(UpgradeConsole.Awake))]
    public static void UpgradeConsoleAwakeHarmonyPostfix(UpgradeConsole __instance)
    {
        UpdateSignals(__instance);
    }

    /// <summary>
    /// Postfix method invoked after the `InitializeModules` method of the UpgradeConsole has been executed.
    /// Ensures that the UpgradeConsole is properly configured to process modules and support AVS upgrades.
    /// </summary>
    /// <param name="__instance">The instance of the UpgradeConsole that has been initialized.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(UpgradeConsole.InitializeModules))]
    public static void UpgradeConsoleInitializeModulesHarmonyPostfix(UpgradeConsole __instance)
    {
        UpdateSignals(__instance);
    }

    /// <summary>
    /// Prefix method invoked before the `OnProtoDeserialize` method of the UpgradeConsole is executed.
    /// Ensures that the UpgradeConsole's state is correctly updated to support AVS upgrades during deserialization.
    /// </summary>
    /// <param name="__instance">The instance of the UpgradeConsole being deserialized.</param>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(UpgradeConsole.OnProtoDeserialize))]
    public static void UpgradeConsoleOnProtoDeserializePrefix(UpgradeConsole __instance)
    {
        UpdateSignals(__instance);
    }

    /// <summary>
    /// Prefix method invoked before the `OnProtoDeserializeObjectTree` method of the UpgradeConsole is executed.
    /// Ensures that the UpgradeConsole is correctly initialized to handle AVS upgrades by updating its signals.
    /// </summary>
    /// <param name="__instance">The instance of the UpgradeConsole that is being deserialized.</param>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(UpgradeConsole.OnProtoDeserializeObjectTree))]
    public static void UpgradeConsoleOnProtoDeserializeObjectTreePrefix(UpgradeConsole __instance)
    {
        UpdateSignals(__instance);
    }

    /// <summary>
    /// Updates signals for the UpgradeConsole to ensure compatibility with AVS upgrades.
    /// Configures the equipment module event listeners and processes all existing upgrades.
    /// </summary>
    /// <param name="console">The instance of the UpgradeConsole to update signals for.</param>
    private static void UpdateSignals(UpgradeConsole console)
    {
        var thisSubRoot = console.GetComponentInParent<SubRoot>();
        if (thisSubRoot.IsNull()) return;

        if (thisSubRoot.isCyclops && console.modules.IsNotNull() && console.modules.equipment.IsNotNull())
        {
            var listener = console.gameObject.EnsureComponent<AvsUpgradesListener>();
            console.modules.onEquip -= listener.OnSlotEquipped;
            console.modules.onUnequip -= listener.OnSlotUnequipped;
            console.modules.onEquip += listener.OnSlotEquipped;
            console.modules.onUnequip += listener.OnSlotUnequipped;
            console.modules.equipment.ForEach(listener.BumpUpgrade);
        }
    }
}