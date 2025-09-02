using AVS.Log;
using AVS.Util;
using System.Collections;
using UnityEngine;

namespace AVS.SaveLoad;

/// <summary>
/// Provides utility methods for saving and loading operations within the AVS.SaveLoad namespace.
/// </summary>
internal static class SaveLoadUtils
{
    internal static bool IsNameUniqueAmongSiblings(Transform tran)
    {
        foreach (Transform tr in tran.parent)
        {
            if (tr == tran) continue;
            if (tr.name == tran.name) return false;
        }

        return true;
    }

    internal static bool IsViableNameCandidate(Transform parent, string nameCandidate)
    {
        foreach (Transform tr in parent)
            if (tr.name == nameCandidate)
                return false;

        return true;
    }

    internal static void EnsureUniqueNameAmongSiblings(Transform tran)
    {
        if (IsNameUniqueAmongSiblings(tran))
            return;

        var counter = 0;
        while (!IsViableNameCandidate(tran.parent, tran.name + counter)) counter++;
        var targetName = tran.name + counter;
        Logger.Warn(
            $"SaveLoadUtils Warning: The name of {tran.NiceName()} is being changed from '{tran.name}' in {tran.parent.NiceName()} to '{targetName}' in order to ensure its name is unique among its siblings. This is important for saving and loading its data correctly.");
        tran.name = targetName;
    }

    internal static string GetTransformPath(Transform root, Transform target)
    {
        if (target == root)
            return "root";
        var result = target.name;
        var index = target.parent;
        while (index && index != root)
        {
            result = $"{index.name}-{result}";
            index = index.parent;
        }

        return result;
    }

    internal static string GetSaveFileName(Transform root, Transform target, string fileSuffix) =>
        $"{GetTransformPath(root, target)}-{fileSuffix}";

    internal static IEnumerator ReloadBatteryPower(SmartLog log, GameObject thisItem, float thisCharge, TechType innerBatteryTT)
    {
        var existing = thisItem.GetComponentInChildren<Battery>();
        // check whether we *are* a battery xor we *have* a battery
        if (existing.IsNotNull())
        {
            // we are a battery
            existing.charge = thisCharge;
        }
        else
        {
            // we have a battery (we are a tool)
            // Thankfully we have this naming convention
            var batSlot = thisItem.transform.Find("BatterySlot");
            if (batSlot.IsNull())
            {
                log.Error($"Failed to find battery slot for tool in innate storage: {thisItem.name}");
                yield break;
            }

            var result = new InstanceContainer();
            yield return AvsCraftData.InstantiateFromPrefabAsync(
                log,
                innerBatteryTT, result);
            var newBat = result.Instance;
            var bat = newBat.SafeGetComponent<Battery>();
            if (bat.IsNotNull())
            {
                bat.charge = thisCharge;
                newBat!.transform.SetParent(batSlot);
                newBat.SetActive(false);
            }
            else
            {
                log.Error($"Failed to find battery component for tool in innate storage: {thisItem.name}");
            }
        }
    }
}