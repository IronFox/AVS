﻿using AVS.Crafting;
using AVS.UpgradeModules;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.Patches
{
    /// <summary>
    /// Helper for enabling AVS upgrades on a Cyclops.
    /// </summary>
    internal class AvsUpgradesListener : MonoBehaviour
    {
        private UpgradeConsole UpgradeConsole => GetComponent<UpgradeConsole>();
        private SubRoot Subroot => GetComponentInParent<SubRoot>();
        private int GetSlotNumber(string slot)
        {
            for (int i = 0; i < UpgradeConsole.modules.equipment.Count; i++)
            {
                try
                {
                    if (slot == SubRoot.slotNames[i])
                    {
                        return i;
                    }
                }
                catch
                {
                    Logger.Warn("Cyclops Upgrades Error: Didn't know about Cyclops Upgrade Slot Name for Slot #" + i.ToString());
                }
            }
            return -1;
        }
        public void OnSlotEquipped(string slot, InventoryItem item)
        {
            IEnumerator BroadcastMessageSoon()
            {
                yield return new WaitUntil(() => Subroot != null);
                Subroot.BroadcastMessage("UpdateAbilities", null, SendMessageOptions.DontRequireReceiver);
            }
            if (item.techType != TechType.None)
            {
                var addedParams = AddActionParams.CreateForCyclops
                (
                    cyclops: Subroot,
                    slotID: GetSlotNumber(slot),
                    techType: item.techType,
                    added: true
                );
                UpgradeRegistrar.OnAddActions.ForEach(x => x(addedParams));
                MainPatcher.Instance.StartCoroutine(BroadcastMessageSoon());
            }
        }
        public void OnSlotUnequipped(string slot, InventoryItem item)
        {
            IEnumerator BroadcastMessageSoon()
            {
                yield return new WaitUntil(() => Subroot != null);
                Subroot.BroadcastMessage("UpdateAbilities", null, SendMessageOptions.DontRequireReceiver);
            }
            if (item.techType != TechType.None)
            {
                var addedParams = AddActionParams.CreateForCyclops
                (
                    cyclops: Subroot,
                    slotID: GetSlotNumber(slot),
                    techType: item.techType,
                    added: false
                );
                UpgradeRegistrar.OnAddActions.ForEach(x => x(addedParams));
                MainPatcher.Instance.StartCoroutine(BroadcastMessageSoon());
            }
        }
        internal void BumpUpgrade(KeyValuePair<string, InventoryItem> upgrade)
        {
            if (upgrade.Value != null)
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
        [HarmonyPostfix]
        [HarmonyPatch(nameof(UpgradeConsole.Awake))]
        public static void UpgradeConsoleAwakeHarmonyPostfix(UpgradeConsole __instance)
        {
            UpdateSignals(__instance);
        }
        [HarmonyPostfix]
        [HarmonyPatch(nameof(UpgradeConsole.InitializeModules))]
        public static void UpgradeConsoleInitializeModulesHarmonyPostfix(UpgradeConsole __instance)
        {
            UpdateSignals(__instance);
        }
        [HarmonyPrefix]
        [HarmonyPatch(nameof(UpgradeConsole.OnProtoDeserialize))]
        public static void UpgradeConsoleOnProtoDeserializePrefix(UpgradeConsole __instance)
        {
            UpdateSignals(__instance);
        }
        [HarmonyPrefix]
        [HarmonyPatch(nameof(UpgradeConsole.OnProtoDeserializeObjectTree))]
        public static void UpgradeConsoleOnProtoDeserializeObjectTreePrefix(UpgradeConsole __instance)
        {
            UpdateSignals(__instance);
        }
        private static void UpdateSignals(UpgradeConsole console)
        {
            SubRoot thisSubRoot = console.GetComponentInParent<SubRoot>();
            if (thisSubRoot == null) return;

            if (thisSubRoot.isCyclops && console.modules != null && console.modules.equipment != null)
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
}
