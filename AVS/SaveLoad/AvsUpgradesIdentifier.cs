using AVS.BaseVehicle;
using AVS.Log;
using AVS.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            Dictionary<string, TechType> result = new Dictionary<string, TechType>();
            upgradeList.ForEach(x => result.Add(x.Key, x.Value?.techType ?? TechType.None));
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
            if (!mv.PrefabID.ReadReflected<Dictionary<string, TechType>>(
                NewSaveFileName,
                out var theseUpgrades,
                LogWriter.Default))
            {
                isFinished = true;
                yield break;
            }
            foreach (var upgrade in theseUpgrades.Where(x => x.Value != TechType.None))
            {
                TaskResult<GameObject> result = new TaskResult<GameObject>();
                yield return CraftData.InstantiateFromPrefabAsync(upgrade.Value, result, false);
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
                    Logger.LogException($"Failed to load upgrade {upgrade.Value} in slot {upgrade.Key} for {mv.name} : {mv.subName.hullName.text}", e);
                    continue;
                }
            }
            isFinished = true;
        }
    }
}
