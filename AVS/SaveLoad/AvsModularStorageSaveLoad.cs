using AVS.BaseVehicle;
using AVS.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.SaveLoad
{
    internal static class AvsModularStorageSaveLoad
    {
        const string SaveFileNamePrefix = "ModSto";
        internal static string GetSaveFileName(int idx)
        {
            return $"{SaveFileNamePrefix}{idx}";
        }
        internal static void SerializeAllModularStorage(AvsVehicle mv)
        {
            for (int i = 0; i < mv.slotIDs.Length; i++)
            {
                string slotID = mv.slotIDs[i];
                if (mv.modules.equipment.TryGetValue(slotID, out InventoryItem result))
                {
                    var container = result?.item.SafeGetComponent<SeamothStorageContainer>();
                    if (container != null && container.container != null)
                    {
                        SaveThisModularStorage(mv, container.container, i);
                    }
                }
            }
        }
        private static void SaveThisModularStorage(AvsVehicle mv, ItemsContainer container, int slotID)
        {
            List<Tuple<TechType, float, TechType>> result = new List<Tuple<TechType, float, TechType>>();
            foreach (var item in container.ToList())
            {
                TechType thisItemType = item.item.GetTechType();
                float batteryChargeIfApplicable = -1;
                var bat = item.item.GetComponentInChildren<Battery>(true);
                TechType innerBatteryTT = TechType.None;
                if (bat != null)
                {
                    batteryChargeIfApplicable = bat.charge;
                    innerBatteryTT = bat.gameObject.GetComponent<TechTag>().type;
                }
                result.Add(new Tuple<TechType, float, TechType>(thisItemType, batteryChargeIfApplicable, innerBatteryTT));
            }
            mv.PrefabID.WriteReflected(
                GetSaveFileName(slotID),
                result,
                mv.Log);
        }
        internal static IEnumerator DeserializeAllModularStorage(AvsVehicle mv)
        {
            yield return new WaitUntil(() => Admin.GameStateWatcher.IsWorldLoaded);
            yield return new WaitUntil(() => mv.upgradesInput.equipment != null);
            foreach (var upgradesLoader in mv.GetComponentsInChildren<AvsUpgradesIdentifier>())
            {
                yield return new WaitUntil(() => upgradesLoader.isFinished);
            }
            for (int i = 0; i < mv.slotIDs.Length; i++)
            {
                string slotID = mv.slotIDs[i];
                if (mv.modules.equipment.TryGetValue(slotID, out InventoryItem result))
                {
                    var container = result?.item?.GetComponent<SeamothStorageContainer>();
                    if (container != null && container.container != null)
                    {
                        UWE.CoroutineHost.StartCoroutine(LoadThisModularStorage(mv, container.container, i));
                    }
                }
            }
            yield break;
        }
        private static IEnumerator LoadThisModularStorage(AvsVehicle mv, ItemsContainer container, int slotID)
        {
            List<Tuple<TechType, float, TechType>>? thisStorage = null;
            if (mv.PrefabID.ReadReflected(
                GetSaveFileName(slotID),
                out thisStorage,
                mv.Log))
            {
                TaskResult<GameObject> result = new TaskResult<GameObject>();
                foreach (var item in thisStorage)
                {
                    yield return CraftData.InstantiateFromPrefabAsync(item.Item1, result, false);
                    GameObject thisItem = result.Get();

                    thisItem.transform.SetParent(mv.Com.StorageRootObject.transform);
                    try
                    {
                        container.AddItem(thisItem.EnsureComponent<Pickupable>());
                    }
                    catch (Exception e)
                    {
                        Logger.LogException($"Failed to add storage item {thisItem.name} to modular storage in slot {slotID} for {mv.name} : {mv.subName.hullName.text}", e);
                    }
                    thisItem.SetActive(false);
                    if (item.Item2 >= 0)
                    {
                        // then we have a battery xor we are a battery
                        try
                        {
                            UWE.CoroutineHost.StartCoroutine(SaveLoadUtils.ReloadBatteryPower(thisItem, item.Item2, item.Item3));
                        }
                        catch (Exception e)
                        {
                            Logger.LogException($"Failed to load reload battery power for modular storage item {thisItem.name} to modular storage in slot {slotID} for {mv.name} : {mv.subName.hullName.text}", e);
                        }
                    }
                }
            }
        }
    }
}
