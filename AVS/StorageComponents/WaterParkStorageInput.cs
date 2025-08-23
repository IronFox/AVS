using AVS.Log;
using AVS.Util;

namespace AVS.StorageComponents;

internal class WaterParkStorageInput : StorageInput
{
    public override void OpenFromExternal()
    {
        if (mv.IsNull())
        {
            LogWriter.Default.Error("AvsVehicle is null in WaterParkStorageInput.OpenFromExternal");
            return;
        }

        if (slotID >= mv.Com.WaterParks.Count)
        {
            LogWriter.Default.Error($"Invalid slotID {slotID} in WaterParkStorageInput.OpenFromExternal");
            return;
        }

        var mwp = mv.Com.WaterParks[slotID].ContentContainer.GetComponent<MobileWaterPark>();
        if (mwp.IsNull())
        {
            mv.Log.Error("MobileWaterPark is null in WaterParkStorageInput.OpenFromExternal");
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
        if (mv.IsNull())
        {
            LogWriter.Default.Error("AvsVehicle is null in WaterParkStorageInput.OpenPDA");
            return;
        }

        if (slotID >= mv.Com.WaterParks.Count)
        {
            LogWriter.Default.Error($"Invalid slotID {slotID} in WaterParkStorageInput.OpenFromExternal");
            return;
        }

        var mwp = mv.Com.WaterParks[slotID].ContentContainer.GetComponent<MobileWaterPark>();
        if (mwp.IsNull())
        {
            mv.Log.Error("MobileWaterPark is null in WaterParkStorageInput.OpenFromExternal");
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