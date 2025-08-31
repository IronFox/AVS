using AVS.BaseVehicle;
using AVS.Crafting;
using AVS.Log;
using AVS.Util;
using AVS.VehicleBuilding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.SaveLoad;

internal class AvsUpgradesIdentifier : MonoBehaviour, IProtoTreeEventListener
{
    internal bool isFinished = false;

    [SerializeField]
    internal AvsVehicle? av;
    internal AvsVehicle AV => av.OrThrow(() => new InvalidOperationException($"Unable to determine AvsUpgradesIdentifier owner"));
    private const string NewSaveFileName = "Upgrades";

    void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
    {
        var upgradeList = AV.modules?.equipment;
        if (upgradeList is null) return;
        var result = new Dictionary<string, string>();
        foreach (var installed in upgradeList)
        {
            if (installed.Value.IsNull()) continue; // Skip null slots
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
        AV.PrefabID?.WriteReflected(
            NewSaveFileName,
            result,
            AV.Owner);
    }

    void IProtoTreeEventListener.OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
    {
        AV.Owner.StartAvsCoroutine(
            nameof(AvsUpgradesIdentifier) + '.' + nameof(LoadUpgrades),
            LoadUpgrades);
    }

    private IEnumerator LoadUpgrades(SmartLog log)
    {
        yield return new WaitUntil(() => AV.IsNotNull());
        yield return new WaitUntil(() => AV.upgradesInput.equipment.IsNotNull());
        AV.UnlockDefaultModuleSlots();
        if (!AV.PrefabID.ReadReflected<Dictionary<string, string>>(
                NewSaveFileName,
                out var theseUpgrades,
                AV.Owner))
        {
            isFinished = true;
            yield break;
        }

        var result = new InstanceContainer();
        foreach (var upgrade in theseUpgrades)
        {
            TechType techType;

            if (upgrade.Value.StartsWith($"Tech:"))
            {
                techType = upgrade.Value.Substring(5).DecodeKey();
                if (techType == TechType.None)
                {
                    log.Error(
                        $"Failed to parse TechType from '{upgrade.Value}' in slot {upgrade.Key} for {AV.NiceName()} : {AV.VehicleName}");
                    continue;
                }

                log.Write(
                    $"Loading upgrade '{upgrade.Value}' in slot {upgrade.Key} for {AV.NiceName()} : {techType.AsString()} / {techType.EncodeKey()}");
            }
            else if (upgrade.Value.StartsWith($"TS:"))
            {
                if (!TechTypeExtensions.FromString(upgrade.Value.Substring(3), out techType, true))
                {
                    log.Error(
                        $"Failed to parse TechType from '{upgrade.Value}' in slot {upgrade.Key} for {AV.NiceName()} : {AV.VehicleName}");
                    continue;
                }

                log.Write(
                    $"Loading upgrade '{upgrade.Value}' in slot {upgrade.Key} for {AV.NiceName()} : {techType.AsString()} / {techType.EncodeKey()}");
            }
            else if (upgrade.Value.StartsWith($"Class:"))
            {
                if (!UpgradeRegistrar.UpgradeClassIdMap.TryGetValue(upgrade.Value.Substring(6), out var upgradePrefab))
                {
                    log.Error(
                        $"Failed to find upgrade prefab for '{upgrade.Value}' in slot {upgrade.Key} for {AV.NiceName()} : {AV.VehicleName}");
                    continue;
                }

                log.Write(
                    $"Loading upgrade '{upgrade.Value}' in slot {upgrade.Key} for {AV.NiceName()} : {upgradePrefab.DisplayName} / {upgradePrefab.TechTypes.ForAvsVehicle}");
                techType = upgradePrefab.TechTypes.ForAvsVehicle;
            }
            else
            {
                if (UpgradeRegistrar.UpgradeClassIdMap.TryGetValue(upgrade.Value, out var upgradePrefab))
                {
                    log.Write(
                        $"Loading upgrade '{upgrade.Value}' in slot {upgrade.Key} for {AV.NiceName()} : {upgradePrefab.DisplayName} / {upgradePrefab.TechTypes.ForAvsVehicle}");
                    techType = upgradePrefab.TechTypes.ForAvsVehicle;
                }
                else
                {
                    log.Error(
                        $"Invalid upgrade format '{upgrade.Value}' in slot {upgrade.Key} for {AV.NiceName()} : {AV.VehicleName}");
                    continue;
                }
            }

            var slotName = upgrade.Key;

            if (!ModuleBuilder.IsModuleName(AV.Owner, slotName))
            {
                var moduleAt = slotName.IndexOf("Module", StringComparison.Ordinal);
                if (moduleAt >= 0 && int.TryParse(slotName.Substring(moduleAt + 6), out var moduleIndex))
                {
                    slotName = AV.slotIDs[moduleIndex];
                    log.Warn($"Slot name '{upgrade.Key}' is invalid, remapped to slot name '{slotName}'");
                }
                else
                {
                    log.Error($"Slot name '{upgrade.Key}' is invalid, unable to remap. Skipping upgrade");
                    continue;
                }
            }


            log.Write(
                $"Loading upgrade {techType} in slot '{slotName}' for {AV.NiceName()} : {techType.AsString()} / {techType.EncodeKey()}");
            yield return AvsCraftData.InstantiateFromPrefabAsync(log, techType,
                result);
            try
            {
                var thisUpgrade = result.Instance;
                if (thisUpgrade.IsNull())
                {
                    log.Error(
                        $"Failed to load upgrade {techType} in slot '{slotName}' for {AV.NiceName()} : {AV.VehicleName}");
                    continue;
                }

                thisUpgrade.transform.SetParent(AV.modulesRoot.transform);
                thisUpgrade.SetActive(false);
                var thisItem = new InventoryItem(thisUpgrade.GetComponent<Pickupable>());
                AV.modules.AddItem(slotName, thisItem, true);
            }
            catch (Exception e)
            {
                log.Error(
                    $"Failed to load upgrade {upgrade.Value} in slot '{upgrade.Key}' for {AV.NiceName()} : {AV.VehicleName}",
                    e);
            }
        }

        isFinished = true;
    }
}