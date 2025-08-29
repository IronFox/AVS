using AVS.BaseVehicle;
using AVS.Log;
using AVS.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.SaveLoad;

internal class AvsInnateStorageIdentifier : MonoBehaviour, IProtoTreeEventListener
{
    internal AvsVehicle Av => GetComponentInParent<AvsVehicle>().OrThrow(() => new InvalidOperationException($"Cannot find AvsInnateStorageIdentifier owner"));
    private const string saveFileNameSuffix = "innatestorage";
    private string SaveFileName => SaveLoadUtils.GetSaveFileName(Av.transform, transform, saveFileNameSuffix);

    void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
    {
        var container = GetComponent<InnateStorageContainer>();
        var result = new List<Tuple<TechType, float, TechType>>();
        foreach (var item in container.Container.ToList())
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

            result.Add(new Tuple<TechType, float, TechType>(thisItemType, batteryChargeIfApplicable, innerBatteryTT));
        }

        Av.SaveInnateStorage(SaveFileName, result);
    }

    void IProtoTreeEventListener.OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
    {
        Av.Owner.StartAvsCoroutine(
            nameof(AvsInnateStorageIdentifier) + '.' + nameof(LoadInnateStorage),
            LoadInnateStorage);
    }

    private IEnumerator LoadInnateStorage(SmartLog log)
    {
        yield return new WaitUntil(() => Av.IsNotNull());

        var thisStorage = Av.ReadInnateStorage(SaveFileName);
        if (thisStorage.IsNull())
            if (!Av.PrefabID.ReadReflected(SaveFileName, out thisStorage, Av.Owner))
                yield break;

        var result = new InstanceContainer();
        foreach (var item in thisStorage)
        {
            yield return AvsCraftData.InstantiateFromPrefabAsync(
                log,
                item.Item1, result);
            var thisItem = result.Instance;
            if (thisItem.IsNull())
            {
                log.Error($"AvsCraftData.InstantiateFromPrefabAsync returned null for {item.Item1}");
                continue;
            }

            thisItem.transform.SetParent(Av.Com.StorageRootObject.transform);
            try
            {
                var ic = GetComponent<InnateStorageContainer>();
                if (ic.IsNull())
                {
                    log.Error(
                        $"InnateStorageContainer not found on {gameObject.name} for {Av.name} : {Av.subName.hullName.text}");
                    continue;
                }

                ic.Container.AddItem(thisItem.EnsureComponent<Pickupable>());
            }
            catch (Exception e)
            {
                log.Error(
                    $"Failed to add storage item {thisItem.name} to innate storage on GameObject {gameObject.name} for {Av.name} : {Av.subName.hullName.text}",
                    e);
            }

            thisItem.SetActive(false);
            if (item.Item2 >= 0)
                // then we have a battery xor we are a battery
                try
                {
                    Av.Owner.StartAvsCoroutine(
                        nameof(SaveLoadUtils) + '.' + nameof(SaveLoadUtils.ReloadBatteryPower),
                        log2 => SaveLoadUtils.ReloadBatteryPower(log2, thisItem, item.Item2, item.Item3));
                }
                catch (Exception e)
                {
                    log.Error(
                        $"Failed to reload battery power for innate storage item {thisItem.name} in innate storage on GameObject {gameObject.name} for {Av.name} : {Av.subName.hullName.text}",
                        e);
                }
        }
    }
}