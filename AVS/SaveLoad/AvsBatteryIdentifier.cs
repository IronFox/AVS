using AVS.BaseVehicle;
using AVS.Log;
using AVS.Util;
using System;
using System.Collections;
using UnityEngine;

namespace AVS.SaveLoad;

internal class AvsBatteryIdentifier : MonoBehaviour, IProtoTreeEventListener
{
    [SerializeField]
    internal AvsVehicle? av;

    private AvsVehicle AV => av.OrThrow(() => new InvalidOperationException(
            $"AvsBatteryIdentifier on GameObject {gameObject.name} has null av reference"));
    private const string saveFileNameSuffix = "battery";
    private string SaveFileName => SaveLoadUtils.GetSaveFileName(AV.transform, transform, saveFileNameSuffix);

    void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
    {
        var thisEM = GetComponent<EnergyMixin>();
        if (thisEM.batterySlot.storedItem.IsNull())
        {
            var emptyBattery = new Tuple<TechType, float>(0, 0);
            AV.SaveBatteryData(SaveFileName, emptyBattery);
        }
        else
        {
            var thisTT = thisEM.batterySlot.storedItem.item.GetTechType();
            var thisEnergy = thisEM.battery.charge;
            var thisBattery = new Tuple<TechType, float>(thisTT, thisEnergy);
            AV.SaveBatteryData(SaveFileName, thisBattery);
        }
    }

    void IProtoTreeEventListener.OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
    {
        AV.Owner.StartAvsCoroutine(
            nameof(AvsBatteryIdentifier) + '.' + nameof(LoadBattery),
            LoadBattery);
    }

    private IEnumerator LoadBattery(SmartLog log)
    {
        var av = AV;
        var thisBattery = av.ReadBatteryData(SaveFileName);
        if (thisBattery == default)
            SaveFiles.Current.ReadPrefabReflected(av.PrefabID, SaveFileName, out thisBattery, av.Owner);
        if (thisBattery == default || thisBattery.Item1 == TechType.None) yield break;
        var result = new InstanceContainer();
        yield return
            AvsCraftData.InstantiateFromPrefabAsync(log, thisBattery.Item1, result);
        var thisItem = result.Instance;
        if (thisItem.IsNull())
        {
            log.Error($"AvsCraftData.InstantiateFromPrefabAsync returned null for {thisBattery.Item1}");
            yield break;
        }

        try
        {
            thisItem.GetComponent<Battery>().charge = thisBattery.Item2;
            thisItem.transform.SetParent(av.Com.StorageRootObject.transform);
            GetComponent<EnergyMixin>().battery = thisItem.GetComponent<Battery>();
            GetComponent<EnergyMixin>().batterySlot.AddItem(thisItem.GetComponent<Pickupable>());
            thisItem.SetActive(false);
        }
        catch (Exception e)
        {
            log.Error(
                $"Failed to load battery : {thisBattery.Item1} for {av.name} on GameObject {gameObject.name} : {av.subName.hullName.text}",
                e);
        }
    }
}