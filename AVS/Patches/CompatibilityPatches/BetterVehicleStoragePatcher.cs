using AVS.BaseVehicle;
using HarmonyLib;
using System;
using AVS.Util;

namespace AVS.Patches.CompatibilityPatches;

/// <summary>
/// PURPOSE: add compatibility for better vehicle storage upgrades
/// VALUE: High. It's a very cool mod.
/// </summary>
public static class BetterVehicleStoragePatcher
{
    /// <summary>
    /// This patch is specifically for the Better Vehicle Storage Mod.
    /// It allows the storage containers to be added to AvsVehicles.
    /// </summary>
    [HarmonyPrefix]
    public static bool Prefix(object __instance, Equipment equipment, ref bool __result)
    {
        if (equipment.owner.GetComponent<AvsVehicle>().IsNotNull())
        {
            __result = true;
            return false;
        }

        return true;
    }

    internal static void TryUseBetterVehicleStorage(AvsVehicle mv, int slotID, TechType techType)
    {
        var type3 = Type.GetType("BetterVehicleStorage.Managers.StorageModuleMgr, BetterVehicleStorage", false, false);
        if (type3.IsNotNull())
        {
            var IsStorageModule = AccessTools.Method(type3, "IsStorageModule");
            var isStorageModuleParams = new object[] { techType };
            var isBetterVehicleStorageModule = (bool)IsStorageModule.Invoke(null, isStorageModuleParams);

            if (isBetterVehicleStorageModule)
            {
                var slotItem = mv.GetSlotItem(slotID);
                if (slotItem.IsNull())
                    return; // No item in the slot, nothing to do.

                var GetItemsContainerFromIventoryItem = AccessTools.Method(type3, "GetItemsContainerFromIventoryItem");
                var GetItemsParams = new object[] { slotItem, techType };
                var itemContainer = (ItemsContainer)GetItemsContainerFromIventoryItem.Invoke(null, GetItemsParams);

                var pda = Player.main.GetPDA();
                Inventory.main.SetUsedStorage(itemContainer, false);
                pda.Open(PDATab.Inventory, null, null);
            }
        }
    }
}