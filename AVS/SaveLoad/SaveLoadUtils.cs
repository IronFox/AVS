using AVS.Util;
using System.Collections;
using UnityEngine;

namespace AVS.SaveLoad
{
    public static class SaveLoadUtils
    {
        internal static bool IsNameUniqueAmongSiblings(Transform tran)
        {
            foreach (Transform tr in tran.parent)
            {
                if (tr == tran)
                {
                    continue;
                }
                if (tr.name == tran.name)
                {
                    return false;
                }
            }
            return true;
        }
        internal static bool IsViableNameCandidate(Transform parent, string nameCandidate)
        {
            foreach (Transform tr in parent)
            {
                if (tr.name == nameCandidate)
                {
                    return false;
                }
            }
            return true;
        }
        internal static void EnsureUniqueNameAmongSiblings(Transform tran)
        {
            if (IsNameUniqueAmongSiblings(tran))
                return;

            int counter = 0;
            while (!IsViableNameCandidate(tran.parent, tran.name + counter))
            {
                counter++;
            }
            string targetName = tran.name + counter;
            Logger.Warn($"SaveLoadUtils Warning: The name of {tran.NiceName()} is being changed from '{tran.name}' in {tran.parent.NiceName()} to '{targetName}' in order to ensure its name is unique among its siblings. This is important for saving and loading its data correctly.");
            tran.name = targetName;
        }
        internal static string GetTransformPath(Transform root, Transform target)
        {
            if (target == root)
            {
                return "root";
            }
            string result = target.name;
            Transform index = target.parent;
            while (index != root)
            {
                result = $"{index.name}-{result}";
                index = index.parent;
            }
            return result;
        }
        internal static string GetSaveFileName(Transform root, Transform target, string fileSuffix)
        {
            return $"{GetTransformPath(root, target)}-{fileSuffix}";
        }
        public static IEnumerator ReloadBatteryPower(GameObject thisItem, float thisCharge, TechType innerBatteryTT)
        {
            // check whether we *are* a battery xor we *have* a battery
            if (thisItem.GetComponent<Battery>() != null)
            {
                // we are a battery
                thisItem.GetComponentInChildren<Battery>().charge = thisCharge;
            }
            else
            {
                // we have a battery (we are a tool)
                // Thankfully we have this naming convention
                Transform batSlot = thisItem.transform.Find("BatterySlot");
                if (batSlot == null)
                {
                    Logger.Error($"Failed to find battery slot for tool in innate storage: {thisItem.name}");
                    yield break;
                }
                TaskResult<GameObject> result = new TaskResult<GameObject>();
                yield return CraftData.InstantiateFromPrefabAsync(innerBatteryTT, result, false);
                GameObject newBat = result.Get();
                if (newBat.GetComponent<Battery>() != null)
                {
                    newBat.GetComponent<Battery>().charge = thisCharge;
                }
                else
                {
                    Logger.Error($"Failed to find battery component for tool in innate storage: {thisItem.name}");
                    yield break;
                }
                newBat.transform.SetParent(batSlot);
                newBat.SetActive(false);
            }
        }
    }
}
