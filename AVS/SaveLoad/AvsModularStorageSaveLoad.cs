using AVS.BaseVehicle;
using AVS.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.SaveLoad;

internal static class AvsModularStorageSaveLoad
{
    private class StorageItem
    {
        public string? techTypeAsString;
        public float batteryCharge;
        public string? innerBatteryTechTypeAsString;
    }


    private const string SaveFileNamePrefix = "ModSto";

    internal static string GetSaveFileName(int idx)
    {
        return $"{SaveFileNamePrefix}{idx}";
    }


    internal static void SerializeAllModularStorage(AvsVehicle mv)
    {
        for (var i = 0; i < mv.slotIDs.Length; i++)
        {
            var slotID = mv.slotIDs[i];
            if (mv.modules.equipment.TryGetValue(slotID, out var result))
            {
                var container = result?.item.SafeGetComponent<SeamothStorageContainer>();
                if (container != null && container.container != null)
                    SaveThisModularStorage(mv, container.container, i);
            }
        }
    }

    public static void SaveThisModularStorage(AvsVehicle mv, ItemsContainer container, int slotID)
    {
        var log = mv.Log.Tag("ModularStorage");
        var result = new List<StorageItem>();
        foreach (var item in container.ToList())
        {
            var thisItemType = item.item.GetTechType();
            float batteryChargeIfApplicable = -1;
            var bat = item.item.GetComponentInChildren<Battery>(true);
            var innerBatteryTT = TechType.None;
            if (bat != null)
            {
                batteryChargeIfApplicable = bat.charge;
                innerBatteryTT = bat.gameObject.GetComponent<TechTag>().type;
            }

            result.Add(new StorageItem
            {
                techTypeAsString = thisItemType.AsString(),
                batteryCharge = batteryChargeIfApplicable,
                innerBatteryTechTypeAsString = innerBatteryTT.AsString()
            });
        }

        mv.PrefabID.WriteReflected(
            GetSaveFileName(slotID),
            result,
            log);
    }

    internal static IEnumerator DeserializeAllModularStorage(AvsVehicle mv)
    {
        yield return new WaitUntil(() => Admin.GameStateWatcher.IsWorldLoaded);
        yield return new WaitUntil(() => mv.upgradesInput.equipment != null);
        foreach (var upgradesLoader in mv.GetComponentsInChildren<AvsUpgradesIdentifier>())
            yield return new WaitUntil(() => upgradesLoader.isFinished);
        for (var i = 0; i < mv.slotIDs.Length; i++)
        {
            var slotID = mv.slotIDs[i];
            if (mv.modules.equipment.TryGetValue(slotID, out var result))
            {
                var container = result?.item?.GetComponent<SeamothStorageContainer>();
                if (container != null && container.container != null)
                    MainPatcher.Instance.StartCoroutine(LoadThisModularStorage(mv, container.container, i));
            }
        }

        yield break;
    }

    private static IEnumerator LoadThisModularStorage(AvsVehicle mv, ItemsContainer container, int slotID)
    {
        var log = mv.Log.Tag(nameof(LoadThisModularStorage));
        List<StorageItem>? thisStorage = null;
        if (mv.PrefabID.ReadReflected(
                GetSaveFileName(slotID),
                out thisStorage,
                log))
        {
            var result = new TaskResult<GameObject>();
            foreach (var item in thisStorage)
            {
                if (!TechTypeExtensions.FromString(item.techTypeAsString, out var tt, true))
                {
                    log.Error(
                        $"Failed to parse TechType '{item.techTypeAsString}' for modular storage item in slot {slotID} for {mv.NiceName()} : {mv.VehicleName}");
                    continue;
                }

                yield return AvsCraftData.InstantiateFromPrefabAsync(log, tt, result);
                var thisItem = result.Get();
                if (thisItem == null)
                {
                    log.Error($"AvsCraftData.InstantiateFromPrefabAsync returned null for {tt}");
                    continue;
                }

                thisItem.transform.SetParent(mv.Com.StorageRootObject.transform);
                try
                {
                    container.AddItem(thisItem.EnsureComponent<Pickupable>());
                }
                catch (Exception e)
                {
                    log.Error(
                        $"Failed to add storage item {thisItem.NiceName()} to modular storage in slot {slotID} for {mv.NiceName()} : {mv.VehicleName}",
                        e);
                }

                thisItem.SetActive(false);
                if (item.batteryCharge >= 0)
                    // then we have a battery xor we are a battery
                    try
                    {
                        if (TechTypeExtensions.FromString(item.innerBatteryTechTypeAsString, out var btt, true))
                            MainPatcher.Instance.StartCoroutine(
                                SaveLoadUtils.ReloadBatteryPower(thisItem, item.batteryCharge, btt));
                        else
                            log.Error(
                                $"Failed to parse inner battery TechType '{item.innerBatteryTechTypeAsString}' for item {thisItem.NiceName()} in modular storage slot {slotID} for {mv.NiceName()} : {mv.VehicleName}");
                    }
                    catch (Exception e)
                    {
                        log.Error(
                            $"Failed to load reload battery power for modular storage item {thisItem.NiceName()} to modular storage in slot {slotID} for {mv.NiceName()} : {mv.VehicleName}",
                            e);
                    }
            }
        }
    }
}