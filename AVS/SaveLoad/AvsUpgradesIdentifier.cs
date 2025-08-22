using AVS.BaseVehicle;
using AVS.Crafting;
using AVS.Log;
using AVS.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using AVS.VehicleBuilding;
using UnityEngine;

namespace AVS.SaveLoad;

internal class AvsUpgradesIdentifier : MonoBehaviour, IProtoTreeEventListener
{
    internal bool isFinished = false;
    internal AvsVehicle mv => GetComponentInParent<AvsVehicle>();
    private const string NewSaveFileName = "Upgrades";

    void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
    {
        var upgradeList = mv.modules?.equipment;
        if (upgradeList is null) return;
        var result = new Dictionary<string, string>();
        foreach (var installed in upgradeList)
        {
            if (installed.Value == null) continue; // Skip null slots
            if (installed.Value.techType == TechType.None) continue; // Skip empty slots

            //if (UpgradeRegistrar.UpgradeTechTypeMap.TryGetValue(installed.Value.techType, out var upgrade))
            //{
            //    result.Add(installed.Key, $"Class:" + upgrade.ClassId);
            //}
            //else
            //result.Add(installed.Key, $"Tech:" + installed.Value.techType.EncodeKey());
            result.Add(installed.Key, $"TS:" + installed.Value.techType.AsString());
        }

        //upgradeList.ForEach(x => result.Add(x.Key, x.Value?.techType ?? TechType.None));
        mv.PrefabID?.WriteReflected(
            NewSaveFileName,
            result,
            LogWriter.Default);
    }

    void IProtoTreeEventListener.OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
    {
        MainPatcher.Instance.StartCoroutine(LoadUpgrades());
    }

    private IEnumerator LoadUpgrades()
    {
        yield return new WaitUntil(() => mv != null);
        yield return new WaitUntil(() => mv.upgradesInput.equipment != null);
        mv.UnlockDefaultModuleSlots();
        if (!mv.PrefabID.ReadReflected<Dictionary<string, string>>(
                NewSaveFileName,
                out var theseUpgrades,
                LogWriter.Default))
        {
            isFinished = true;
            yield break;
        }

        var log = mv.Log.Tag(nameof(LoadUpgrades));
        foreach (var upgrade in theseUpgrades)
        {
            var result = new TaskResult<GameObject>();
            TechType techType;

            if (upgrade.Value.StartsWith($"Tech:"))
            {
                techType = TechTypeExtensions.DecodeKey(upgrade.Value.Substring(5));
                if (techType == TechType.None)
                {
                    log.Error(
                        $"Failed to parse TechType from '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {mv.VehicleName}");
                    continue;
                }

                log.Write(
                    $"Loading upgrade '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {techType.AsString()} / {techType.EncodeKey()}");
            }
            else if (upgrade.Value.StartsWith($"TS:"))
            {
                if (!TechTypeExtensions.FromString(upgrade.Value.Substring(3), out techType, true))
                {
                    log.Error(
                        $"Failed to parse TechType from '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {mv.VehicleName}");
                    continue;
                }

                log.Write(
                    $"Loading upgrade '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {techType.AsString()} / {techType.EncodeKey()}");
            }
            else if (upgrade.Value.StartsWith($"Class:"))
            {
                if (!UpgradeRegistrar.UpgradeClassIdMap.TryGetValue(upgrade.Value.Substring(6), out var upgradePrefab))
                {
                    log.Error(
                        $"Failed to find upgrade prefab for '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {mv.VehicleName}");
                    continue;
                }

                log.Write(
                    $"Loading upgrade '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {upgradePrefab.DisplayName} / {upgradePrefab.TechTypes.ForAvsVehicle}");
                techType = upgradePrefab.TechTypes.ForAvsVehicle;
            }
            else
            {
                if (UpgradeRegistrar.UpgradeClassIdMap.TryGetValue(upgrade.Value, out var upgradePrefab))
                {
                    log.Write(
                        $"Loading upgrade '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {upgradePrefab.DisplayName} / {upgradePrefab.TechTypes.ForAvsVehicle}");
                    techType = upgradePrefab.TechTypes.ForAvsVehicle;
                }
                else
                {
                    log.Error(
                        $"Invalid upgrade format '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {mv.VehicleName}");
                    continue;
                }
            }

            var slotName = upgrade.Key;

            if (!ModuleBuilder.IsModuleName(slotName))
            {
                var moduleAt = slotName.IndexOf("Module", StringComparison.Ordinal);
                if (moduleAt >= 0 && int.TryParse(slotName.Substring(moduleAt + 6), out var moduleIndex))
                {
                    slotName = mv.slotIDs[moduleIndex];
                    log.Warn($"Slot name '{upgrade.Key}' is invalid, remapped to slot name '{slotName}'");
                }
                else
                {
                    log.Error($"Slot name '{upgrade.Key}' is invalid, unable to remap. Skipping upgrade");
                    ;
                    continue;
                }
            }


            log.Write(
                $"Loading upgrade {techType} in slot '{slotName}' for {mv.NiceName()} : {techType.AsString()} / {techType.EncodeKey()}");
            yield return AvsCraftData.InstantiateFromPrefabAsync(mv.Log.Tag(nameof(AvsUpgradesIdentifier)), techType,
                result);
            try
            {
                var thisUpgrade = result.Get();
                if (!thisUpgrade)
                {
                    log.Error(
                        $"Failed to load upgrade {techType} in slot '{slotName}' for {mv.NiceName()} : {mv.VehicleName}");
                    continue;
                }

                thisUpgrade.transform.SetParent(mv.modulesRoot.transform);
                thisUpgrade.SetActive(false);
                var thisItem = new InventoryItem(thisUpgrade.GetComponent<Pickupable>());
                mv.modules.AddItem(slotName, thisItem, true);
            }
            catch (Exception e)
            {
                log.Error(
                    $"Failed to load upgrade {upgrade.Value} in slot '{upgrade.Key}' for {mv.NiceName()} : {mv.VehicleName}",
                    e);
            }
        }

        isFinished = true;
    }
}