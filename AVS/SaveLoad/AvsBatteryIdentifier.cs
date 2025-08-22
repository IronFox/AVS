using AVS.BaseVehicle;
using System;
using System.Collections;
using AVS.Util;
using UnityEngine;

namespace AVS.SaveLoad;

internal class AvsBatteryIdentifier : MonoBehaviour, IProtoTreeEventListener
{
    internal AvsVehicle mv => GetComponentInParent<AvsVehicle>();
    private const string saveFileNameSuffix = "battery";
    private string SaveFileName => SaveLoadUtils.GetSaveFileName(mv.transform, transform, saveFileNameSuffix);

    void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
    {
        var thisEM = GetComponent<EnergyMixin>();
        if (thisEM.batterySlot.storedItem == null)
        {
            var emptyBattery = new Tuple<TechType, float>(0, 0);
            mv.SaveBatteryData(SaveFileName, emptyBattery);
        }
        else
        {
            var thisTT = thisEM.batterySlot.storedItem.item.GetTechType();
            var thisEnergy = thisEM.battery.charge;
            var thisBattery = new Tuple<TechType, float>(thisTT, thisEnergy);
            mv.SaveBatteryData(SaveFileName, thisBattery);
        }
    }

    void IProtoTreeEventListener.OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
    {
        MainPatcher.Instance.StartCoroutine(LoadBattery());
    }

    private IEnumerator LoadBattery()
    {
        yield return new WaitUntil(() => mv != null);
        var log = mv.Log.Tag(nameof(LoadBattery));
        var thisBattery = mv.ReadBatteryData(SaveFileName);
        if (thisBattery == default)
            SaveFiles.Current.ReadPrefabReflected(mv.PrefabID, SaveFileName, out thisBattery, mv.Log);
        if (thisBattery == default || thisBattery.Item1 == TechType.None) yield break;
        var result = new TaskResult<GameObject>();
        yield return
            AvsCraftData.InstantiateFromPrefabAsync(mv.Log.Tag(nameof(LoadBattery)), thisBattery.Item1, result, false);
        var thisItem = result.Get();
        if (thisItem == null)
        {
            log.Error($"AvsCraftData.InstantiateFromPrefabAsync returned null for {thisBattery.Item1}");
            yield break;
        }

        try
        {
            thisItem.GetComponent<Battery>().charge = thisBattery.Item2;
            thisItem.transform.SetParent(mv.Com.StorageRootObject.transform);
            GetComponent<EnergyMixin>().battery = thisItem.GetComponent<Battery>();
            GetComponent<EnergyMixin>().batterySlot.AddItem(thisItem.GetComponent<Pickupable>());
            thisItem.SetActive(false);
        }
        catch (Exception e)
        {
            log.Error(
                $"Failed to load battery : {thisBattery.Item1} for {mv.name} on GameObject {gameObject.name} : {mv.subName.hullName.text}",
                e);
        }
    }
}