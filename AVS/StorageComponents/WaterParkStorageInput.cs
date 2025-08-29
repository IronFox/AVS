using AVS.Log;
using AVS.Util;

namespace AVS.StorageComponents;

internal class WaterParkStorageInput : StorageInput
{
    public override void OpenFromExternal()
    {
        if (av.IsNull())
        {
            LogWriter.Default.Error("AvsVehicle is null in WaterParkStorageInput.OpenFromExternal");
            return;
        }
        using var log = av.NewAvsLog();

        if (slotID >= av.Com.WaterParks.Count)
        {
            log.Error($"Invalid slotID {slotID} in WaterParkStorageInput.OpenFromExternal");
            return;
        }

        var mwp = av.Com.WaterParks[slotID].ContentContainer.GetComponent<MobileWaterPark>();
        if (mwp.IsNull())
        {
            log.Error("MobileWaterPark is null in WaterParkStorageInput.OpenFromExternal");
            return;
        }

        var storageInSlot = mwp.Container;
        if (!storageInSlot.IsNull())
        {
            var pda = Player.main.GetPDA();
            Inventory.main.SetUsedStorage(storageInSlot, false);
            pda.Open(PDATab.Inventory, null, null);
        }
    }

    protected override void OpenPDA()
    {
        if (av.IsNull())
        {
            LogWriter.Default.Error("AvsVehicle is null in WaterParkStorageInput.OpenPDA");
            return;
        }
        using var log = av.NewAvsLog();

        if (slotID >= av.Com.WaterParks.Count)
        {
            log.Error($"Invalid slotID {slotID} in WaterParkStorageInput.OpenFromExternal");
            return;
        }

        var mwp = av.Com.WaterParks[slotID].ContentContainer.GetComponent<MobileWaterPark>();
        if (mwp.IsNull())
        {
            log.Error("MobileWaterPark is null in WaterParkStorageInput.OpenFromExternal");
            return;
        }

        var storageInSlot = mwp.Container;
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