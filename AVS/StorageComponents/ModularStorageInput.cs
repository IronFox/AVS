using AVS.BaseVehicle;
using System.Collections.Generic;
using System.Linq;
using AVS.StorageComponents;
using AVS.Util;

namespace AVS;

internal class ModularStorageInput : StorageInput
{
    public override void OpenFromExternal()
    {
        if (mv.IsNull())
            return;
        var storageInSlot = mv.ModGetStorageInSlot(slotID, TechType.VehicleStorageModule);
        if (storageInSlot.IsNotNull())
        {
            var pda = Player.main.GetPDA();
            Inventory.main.SetUsedStorage(storageInSlot, false);
            pda.Open(PDATab.Inventory, null, null);
        }
    }

    protected override void OpenPDA()
    {
        if (mv.IsNull())
            return;
        var storageInSlot = mv.ModGetStorageInSlot(slotID, TechType.VehicleStorageModule);
        if (storageInSlot.IsNull())
            storageInSlot = gameObject.GetComponent<SeamothStorageContainer>().container;

        if (storageInSlot.IsNotNull())
        {
            var pda = Player.main.GetPDA();
            Inventory.main.SetUsedStorage(storageInSlot, false);
            if (!pda.Open(PDATab.Inventory, tr, new PDA.OnClose(OnClosePDA)))
            {
                OnClosePDA(pda);
                return;
            }
        }
        else
        {
            OnClosePDA(null);
        }
    }

    public static List<ItemsContainer> GetAllModularStorageContainers(AvsVehicle mv)
    {
        var result = new List<ItemsContainer>();
        if (mv.IsNull())
            return result;
        var containerList = mv.Com.ModulesRootObject.GetComponentsInChildren<SeamothStorageContainer>(true);
        if (!containerList.Any())
            return result;
        return containerList.Select(x => x.container).ToList();
    }
}