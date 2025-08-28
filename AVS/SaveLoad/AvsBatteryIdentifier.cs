using AVS.BaseVehicle;
using AVS.Util;
using System;
using System.Collections;
using UnityEngine;

namespace AVS.SaveLoad;

internal class AvsBatteryIdentifier : MonoBehaviour, IProtoTreeEventListener
{
    internal AvsVehicle av => GetComponentInParent<AvsVehicle>();
    private const string saveFileNameSuffix = "battery";
    private string SaveFileName => SaveLoadUtils.GetSaveFileName(av.transform, transform, saveFileNameSuffix);

    void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
    {
        var thisEM = GetComponent<EnergyMixin>();
        if (thisEM.batterySlot.storedItem.IsNull())
        {
            var emptyBattery = new Tuple<TechType, float>(0, 0);
            av.SaveBatteryData(SaveFileName, emptyBattery);
        }
        else
        {
            var thisTT = thisEM.batterySlot.storedItem.item.GetTechType();
            var thisEnergy = thisEM.battery.charge;
            var thisBattery = new Tuple<TechType, float>(thisTT, thisEnergy);
            av.SaveBatteryData(SaveFileName, thisBattery);
        }
    }

    void IProtoTreeEventListener.OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
    {
        av.Owner.StartCoroutine(LoadBattery());
    }

    private IEnumerator LoadBattery()
    {
        yield return new WaitUntil(() => av.IsNotNull());
        var log = av.Log.Tag(nameof(LoadBattery));
        var thisBattery = av.ReadBatteryData(SaveFileName);
        if (thisBattery == default)
            SaveFiles.Current.ReadPrefabReflected(av.PrefabID, SaveFileName, out thisBattery, av.Log);
        if (thisBattery == default || thisBattery.Item1 == TechType.None) yield break;
        var result = new InstanceContainer();
        yield return
            AvsCraftData.InstantiateFromPrefabAsync(av.Log.Tag(nameof(LoadBattery)), thisBattery.Item1, result);
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