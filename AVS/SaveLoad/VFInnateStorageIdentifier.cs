﻿using AVS.BaseVehicle;
using AVS.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.SaveLoad
{
    internal class VFInnateStorageIdentifier : MonoBehaviour, IProtoTreeEventListener
    {
        internal AvsVehicle mv => GetComponentInParent<AvsVehicle>();
        const string saveFileNameSuffix = "innatestorage";
        private string SaveFileName => SaveLoadUtils.GetSaveFileName(mv.transform, transform, saveFileNameSuffix);

        void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
        {
            InnateStorageContainer container = GetComponent<InnateStorageContainer>();
            List<Tuple<TechType, float, TechType>> result = new List<Tuple<TechType, float, TechType>>();
            foreach (var item in container.Container.ToList())
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
            mv.SaveInnateStorage(SaveFileName, result);
        }
        void IProtoTreeEventListener.OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
        {
            UWE.CoroutineHost.StartCoroutine(LoadInnateStorage());
        }
        private IEnumerator LoadInnateStorage()
        {
            yield return new WaitUntil(() => mv != null);

            var thisStorage = mv.ReadInnateStorage(SaveFileName);
            if (thisStorage == null)
            {
                if (!mv.PrefabID.ReadReflected(SaveFileName, out thisStorage, mv.Log))
                    yield break;
            }

            TaskResult<GameObject> result = new TaskResult<GameObject>();
            foreach (var item in thisStorage)
            {
                yield return CraftData.InstantiateFromPrefabAsync(item.Item1, result, false);
                GameObject thisItem = result.Get();

                thisItem.transform.SetParent(mv.Com.StorageRootObject.transform);
                try
                {
                    var ic = GetComponent<InnateStorageContainer>();
                    if (ic == null)
                    {
                        Logger.Error($"InnateStorageContainer not found on {gameObject.name} for {mv.name} : {mv.subName.hullName.text}");
                        continue;
                    }
                    ic.Container.AddItem(thisItem.EnsureComponent<Pickupable>());
                }
                catch (Exception e)
                {
                    Logger.LogException($"Failed to add storage item {thisItem.name} to innate storage on GameObject {gameObject.name} for {mv.name} : {mv.subName.hullName.text}", e);
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
                        Logger.LogException($"Failed to reload battery power for innate storage item {thisItem.name} in innate storage on GameObject {gameObject.name} for {mv.name} : {mv.subName.hullName.text}", e);
                    }
                }
            }
        }


    }
}
