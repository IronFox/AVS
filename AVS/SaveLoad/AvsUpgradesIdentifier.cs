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
    internal AvsVehicle Av => GetComponentInParent<AvsVehicle>().OrThrow(() => new InvalidOperationException($"Unable to determine AvsUpgradesIdentifier owner"));
    private const string NewSaveFileName = "Upgrades";

    void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
    {
        var upgradeList = Av.modules?.equipment;
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
        Av.PrefabID?.WriteReflected(
            NewSaveFileName,
            result,
            LogWriter.Default);
    }

    void IProtoTreeEventListener.OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
    {
        Av.StartCoroutine(LoadUpgrades());
    }

    private IEnumerator LoadUpgrades()
    {
        yield return new WaitUntil(() => Av.IsNotNull());
        yield return new WaitUntil(() => Av.upgradesInput.equipment.IsNotNull());
        Av.UnlockDefaultModuleSlots();
        if (!Av.PrefabID.ReadReflected<Dictionary<string, string>>(
                NewSaveFileName,
                out var theseUpgrades,
                LogWriter.Default))
        {
            isFinished = true;
            yield break;
        }

        var result = new InstanceContainer();
        var log = Av.Log.Tag(nameof(LoadUpgrades));
        foreach (var upgrade in theseUpgrades)
        {
            TechType techType;

            if (upgrade.Value.StartsWith($"Tech:"))
            {
                techType = upgrade.Value.Substring(5).DecodeKey();
                if (techType == TechType.None)
                {
                    log.Error(
                        $"Failed to parse TechType from '{upgrade.Value}' in slot {upgrade.Key} for {Av.NiceName()} : {Av.VehicleName}");
                    continue;
                }

                log.Write(
                    $"Loading upgrade '{upgrade.Value}' in slot {upgrade.Key} for {Av.NiceName()} : {techType.AsString()} / {techType.EncodeKey()}");
            }
            else if (upgrade.Value.StartsWith($"TS:"))
            {
                if (!TechTypeExtensions.FromString(upgrade.Value.Substring(3), out techType, true))
                {
                    log.Error(
                        $"Failed to parse TechType from '{upgrade.Value}' in slot {upgrade.Key} for {Av.NiceName()} : {Av.VehicleName}");
                    continue;
                }

                log.Write(
                    $"Loading upgrade '{upgrade.Value}' in slot {upgrade.Key} for {Av.NiceName()} : {techType.AsString()} / {techType.EncodeKey()}");
            }
            else if (upgrade.Value.StartsWith($"Class:"))
            {
                if (!UpgradeRegistrar.UpgradeClassIdMap.TryGetValue(upgrade.Value.Substring(6), out var upgradePrefab))
                {
                    log.Error(
                        $"Failed to find upgrade prefab for '{upgrade.Value}' in slot {upgrade.Key} for {Av.NiceName()} : {Av.VehicleName}");
                    continue;
                }

                log.Write(
                    $"Loading upgrade '{upgrade.Value}' in slot {upgrade.Key} for {Av.NiceName()} : {upgradePrefab.DisplayName} / {upgradePrefab.TechTypes.ForAvsVehicle}");
                techType = upgradePrefab.TechTypes.ForAvsVehicle;
            }
            else
            {
                if (UpgradeRegistrar.UpgradeClassIdMap.TryGetValue(upgrade.Value, out var upgradePrefab))
                {
                    log.Write(
                        $"Loading upgrade '{upgrade.Value}' in slot {upgrade.Key} for {Av.NiceName()} : {upgradePrefab.DisplayName} / {upgradePrefab.TechTypes.ForAvsVehicle}");
                    techType = upgradePrefab.TechTypes.ForAvsVehicle;
                }
                else
                {
                    log.Error(
                        $"Invalid upgrade format '{upgrade.Value}' in slot {upgrade.Key} for {Av.NiceName()} : {Av.VehicleName}");
                    continue;
                }
            }

            var slotName = upgrade.Key;

            if (!ModuleBuilder.IsModuleName(Av.Owner, slotName))
            {
                var moduleAt = slotName.IndexOf("Module", StringComparison.Ordinal);
                if (moduleAt >= 0 && int.TryParse(slotName.Substring(moduleAt + 6), out var moduleIndex))
                {
                    slotName = Av.slotIDs[moduleIndex];
                    log.Warn($"Slot name '{upgrade.Key}' is invalid, remapped to slot name '{slotName}'");
                }
                else
                {
                    log.Error($"Slot name '{upgrade.Key}' is invalid, unable to remap. Skipping upgrade");
                    continue;
                }
            }


            log.Write(
                $"Loading upgrade {techType} in slot '{slotName}' for {Av.NiceName()} : {techType.AsString()} / {techType.EncodeKey()}");
            yield return AvsCraftData.InstantiateFromPrefabAsync(Av.Log.Tag(nameof(AvsUpgradesIdentifier)), techType,
                result);
            try
            {
                var thisUpgrade = result.Instance;
                if (thisUpgrade.IsNull())
                {
                    log.Error(
                        $"Failed to load upgrade {techType} in slot '{slotName}' for {Av.NiceName()} : {Av.VehicleName}");
                    continue;
                }

                thisUpgrade.transform.SetParent(Av.modulesRoot.transform);
                thisUpgrade.SetActive(false);
                var thisItem = new InventoryItem(thisUpgrade.GetComponent<Pickupable>());
                Av.modules.AddItem(slotName, thisItem, true);
            }
            catch (Exception e)
            {
                log.Error(
                    $"Failed to load upgrade {upgrade.Value} in slot '{upgrade.Key}' for {Av.NiceName()} : {Av.VehicleName}",
                    e);
            }
        }

        isFinished = true;
    }
}