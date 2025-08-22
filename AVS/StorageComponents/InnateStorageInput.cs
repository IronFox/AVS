using AVS.StorageComponents;
using AVS.Util;
using UnityEngine;

namespace AVS;

internal class InnateStorageInput : StorageInput
{
    public override void OpenFromExternal()
    {
        if (mv.IsNull())
        {
            Debug.LogError("AvsVehicle is null in InnateStorageInput.OpenFromExternal");
            return;
        }

        var storageInSlot = mv.ModGetStorageInSlot(slotID, AvsVehicleBuilder.InnateStorage);
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
        {
            Debug.LogError("AvsVehicle is null in InnateStorageInput.OpenPDA");
            return;
        }

        var storageInSlot = mv.ModGetStorageInSlot(slotID, AvsVehicleBuilder.InnateStorage);
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
}