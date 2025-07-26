using AVS.BaseVehicle;
using System;
using System.Collections;
using UnityEngine;

namespace AVS.SaveLoad
{
    internal class AvsBatteryIdentifier : MonoBehaviour, IProtoTreeEventListener
    {
        internal AvsVehicle mv => GetComponentInParent<AvsVehicle>();
        const string saveFileNameSuffix = "battery";
        private string SaveFileName => SaveLoadUtils.GetSaveFileName(mv.transform, transform, saveFileNameSuffix);

        void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
        {
            EnergyMixin thisEM = GetComponent<EnergyMixin>();
            if (thisEM.batterySlot.storedItem == null)
            {
                var emptyBattery = new Tuple<TechType, float>(0, 0);
                mv.SaveBatteryData(SaveFileName, emptyBattery);
            }
            else
            {
                TechType thisTT = thisEM.batterySlot.storedItem.item.GetTechType();
                float thisEnergy = thisEM.battery.charge;
                var thisBattery = new Tuple<TechType, float>(thisTT, thisEnergy);
                mv.SaveBatteryData(SaveFileName, thisBattery);
            }
        }
        void IProtoTreeEventListener.OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
        {
            UWE.CoroutineHost.StartCoroutine(LoadBattery());
        }
        private IEnumerator LoadBattery()
        {
            yield return new WaitUntil(() => mv != null);
            var thisBattery = mv.ReadBatteryData(SaveFileName);
            if (thisBattery == default)
            {
                SaveFiles.Current.ReadPrefabReflected(mv.PrefabID, SaveFileName, out thisBattery, mv.Log);
            }
            if (thisBattery == default || thisBattery.Item1 == TechType.None)
            {
                yield break;
            }
            TaskResult<GameObject> result = new TaskResult<GameObject>();
            yield return CraftData.InstantiateFromPrefabAsync(thisBattery.Item1, result, false);
            GameObject thisItem = result.Get();
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
                Logger.LogException($"Failed to load battery : {thisBattery.Item1} for {mv.name} on GameObject {gameObject.name} : {mv.subName.hullName.text}", e);
            }
        }
    }
}
