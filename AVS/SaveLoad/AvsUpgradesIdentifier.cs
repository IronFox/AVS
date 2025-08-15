using AVS.BaseVehicle;
using AVS.Crafting;
using AVS.Log;
using AVS.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.SaveLoad
{
    internal class AvsUpgradesIdentifier : MonoBehaviour, IProtoTreeEventListener
    {
        internal bool isFinished = false;
        internal AvsVehicle mv => GetComponentInParent<AvsVehicle>();
        private const string NewSaveFileName = "Upgrades";
        void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
        {
            var upgradeList = mv.modules?.equipment;
            if (upgradeList is null)
            {
                return;
            }
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (var installed in upgradeList)
            {
                if (installed.Value == null)
                {
                    continue; // Skip null slots
                }
                if (installed.Value.techType == TechType.None)
                {
                    continue; // Skip empty slots
                }

                //if (UpgradeRegistrar.UpgradeTechTypeMap.TryGetValue(installed.Value.techType, out var upgrade))
                //{
                //    result.Add(installed.Key, $"Class:" + upgrade.ClassId);
                //}
                //else
                //result.Add(installed.Key, $"Tech:" + installed.Value.techType.EncodeKey());
                result.Add(installed.Key, $"TS:" + installed.Value.techType.AsString());
            }
            //upgradeList.ForEach(x => result.Add(x.Key, x.Value?.techType ?? TechType.None));
            mv.PrefabID?.WriteReflected(
                NewSaveFileName,
                result,
                LogWriter.Default);
        }
        void IProtoTreeEventListener.OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
        {
            MainPatcher.Instance.StartCoroutine(LoadUpgrades());
        }
        private IEnumerator LoadUpgrades()
        {
            yield return new WaitUntil(() => mv != null);
            yield return new WaitUntil(() => mv.upgradesInput.equipment != null);
            mv.UnlockDefaultModuleSlots();
            if (!mv.PrefabID.ReadReflected<Dictionary<string, string>>(
                NewSaveFileName,
                out var theseUpgrades,
                LogWriter.Default))
            {
                isFinished = true;
                yield break;
            }
            foreach (var upgrade in theseUpgrades)
            {
                TaskResult<GameObject> result = new TaskResult<GameObject>();
                TechType techType;

                if (upgrade.Value.StartsWith($"Tech:"))
                {
                    techType = TechTypeExtensions.DecodeKey(upgrade.Value.Substring(5));
                    if (techType == TechType.None)
                    {
                        mv.Log.Tag("Modules").Error($"Failed to parse TechType from '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {mv.VehicleName}");
                        continue;
                    }
                    mv.Log.Tag("Modules").Write($"Loading upgrade '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {techType.AsString()} / {techType.EncodeKey()}");
                }
                else if (upgrade.Value.StartsWith($"TS:"))
                {
                    if (!TechTypeExtensions.FromString(upgrade.Value.Substring(3), out techType, true))
                    {
                        mv.Log.Tag("Modules").Error($"Failed to parse TechType from '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {mv.VehicleName}");
                        continue;
                    }
                    mv.Log.Tag("Modules").Write($"Loading upgrade '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {techType.AsString()} / {techType.EncodeKey()}");
                }
                else if (upgrade.Value.StartsWith($"Class:"))
                {
                    if (!UpgradeRegistrar.UpgradeClassIdMap.TryGetValue(upgrade.Value.Substring(6), out var upgradePrefab))
                    {
                        mv.Log.Tag("Modules").Error($"Failed to find upgrade prefab for '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {mv.VehicleName}");
                        continue;
                    }
                    mv.Log.Tag("Modules").Write($"Loading upgrade '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {upgradePrefab.DisplayName} / {upgradePrefab.TechTypes.ForAvsVehicle}");
                    techType = upgradePrefab.TechTypes.ForAvsVehicle;
                }
                else
                {
                    if (UpgradeRegistrar.UpgradeClassIdMap.TryGetValue(upgrade.Value, out var upgradePrefab))
                    {
                        mv.Log.Tag("Modules").Write($"Loading upgrade '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {upgradePrefab.DisplayName} / {upgradePrefab.TechTypes.ForAvsVehicle}");
                        techType = upgradePrefab.TechTypes.ForAvsVehicle;
                    }
                    else
                    {
                        mv.Log.Tag("Modules").Error($"Invalid upgrade format '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {mv.VehicleName}");
                        continue;
                    }
                }



                yield return CraftData.InstantiateFromPrefabAsync(techType, result, false);
                try
                {
                    GameObject thisUpgrade = result.Get();
                    thisUpgrade.transform.SetParent(mv.modulesRoot.transform);
                    thisUpgrade.SetActive(false);
                    InventoryItem thisItem = new InventoryItem(thisUpgrade.GetComponent<Pickupable>());
                    mv.modules.AddItem(upgrade.Key, thisItem, true);
                }
                catch (Exception e)
                {
                    mv.Log.Tag("Modules").Error($"Failed to load upgrade '{upgrade.Value}' in slot {upgrade.Key} for {mv.NiceName()} : {mv.VehicleName}", e);
                    continue;
                }
            }
            isFinished = true;
        }
    }
}
