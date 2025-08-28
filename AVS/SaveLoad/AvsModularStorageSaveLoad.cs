using AVS.Admin;
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

    internal static string GetSaveFileName(int idx) => $"{SaveFileNamePrefix}{idx}";


    internal static void SerializeAllModularStorage(AvsVehicle av)
    {
        for (var i = 0; i < av.slotIDs.Length; i++)
        {
            var slotID = av.slotIDs[i];
            if (av.modules.equipment.TryGetValue(slotID, out var result))
            {
                var container = result?.item.SafeGetComponent<SeamothStorageContainer>();
                if (container.IsNotNull() && container.container.IsNotNull())
                    SaveThisModularStorage(av, container.container, i);
            }
        }
    }

    public static void SaveThisModularStorage(AvsVehicle av, ItemsContainer container, int slotID)
    {
        var log = av.Log.Tag("ModularStorage");
        var result = new List<StorageItem>();
        foreach (var item in container.ToList())
        {
            var thisItemType = item.item.GetTechType();
            float batteryChargeIfApplicable = -1;
            var bat = item.item.GetComponentInChildren<Battery>(true);
            var innerBatteryTT = TechType.None;
            if (bat.IsNotNull())
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

        av.PrefabID.WriteReflected(
            GetSaveFileName(slotID),
            result,
            log);
    }

    internal static IEnumerator DeserializeAllModularStorage(MainPatcher mp, AvsVehicle av)
    {
        yield return new WaitUntil(() => GameStateWatcher.IsWorldLoaded);
        yield return new WaitUntil(() => av.upgradesInput.equipment.IsNotNull());
        foreach (var upgradesLoader in av.GetComponentsInChildren<AvsUpgradesIdentifier>())
            yield return new WaitUntil(() => upgradesLoader.isFinished);
        for (var i = 0; i < av.slotIDs.Length; i++)
        {
            var slotID = av.slotIDs[i];
            if (av.modules.equipment.TryGetValue(slotID, out var result))
            {
                var container = result?.item?.GetComponent<SeamothStorageContainer>();
                if (container.IsNotNull() && container.container.IsNotNull())
                    mp.StartCoroutine(LoadThisModularStorage(mp, av, container.container, i));
            }
        }
    }

    private static IEnumerator LoadThisModularStorage(MainPatcher mp, AvsVehicle av, ItemsContainer container, int slotID)
    {
        var log = av.Log.Tag(nameof(LoadThisModularStorage));
        if (av.PrefabID.ReadReflected(
                GetSaveFileName(slotID),
                out List<StorageItem>? thisStorage,
                log))
        {
            var result = new InstanceContainer();
            foreach (var item in thisStorage)
            {
                if (!TechTypeExtensions.FromString(item.techTypeAsString, out var tt, true))
                {
                    log.Error(
                        $"Failed to parse TechType '{item.techTypeAsString}' for modular storage item in slot {slotID} for {av.NiceName()} : {av.VehicleName}");
                    continue;
                }

                yield return AvsCraftData.InstantiateFromPrefabAsync(log, tt, result);
                var thisItem = result.Instance;
                if (thisItem.IsNull())
                {
                    log.Error($"AvsCraftData.InstantiateFromPrefabAsync returned null for {tt}");
                    continue;
                }

                thisItem.transform.SetParent(av.Com.StorageRootObject.transform);
                try
                {
                    container.AddItem(thisItem.EnsureComponent<Pickupable>());
                }
                catch (Exception e)
                {
                    log.Error(
                        $"Failed to add storage item {thisItem.NiceName()} to modular storage in slot {slotID} for {av.NiceName()} : {av.VehicleName}",
                        e);
                }

                thisItem.SetActive(false);
                if (item.batteryCharge >= 0)
                    // then we have a battery xor we are a battery
                    try
                    {
                        if (TechTypeExtensions.FromString(item.innerBatteryTechTypeAsString, out var btt, true))
                            mp.StartCoroutine(
                                SaveLoadUtils.ReloadBatteryPower(thisItem, item.batteryCharge, btt));
                        else
                            log.Error(
                                $"Failed to parse inner battery TechType '{item.innerBatteryTechTypeAsString}' for item {thisItem.NiceName()} in modular storage slot {slotID} for {av.NiceName()} : {av.VehicleName}");
                    }
                    catch (Exception e)
                    {
                        log.Error(
                            $"Failed to load reload battery power for modular storage item {thisItem.NiceName()} to modular storage in slot {slotID} for {av.NiceName()} : {av.VehicleName}",
                            e);
                    }
            }
        }
    }
}